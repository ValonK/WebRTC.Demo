using RTCDemo.iOS.Controls;
using RTCDemo.iOS.Models;
using RTCDemo.iOS.Services.SignalR;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.ViewControllers;

public class MainViewController : BaseViewController
{
    private IncomingCallControl _incomingCallControl;
    private UICollectionView _clientsCollectionView;
    private readonly List<Client> _connectedClients = new();
    private EmptyView _emptyView;
    private Client _otherClient;

    private UIView _statusIndicator;

    public override async void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.White;

        var logo = new UIImageView
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            Image = UIImage.FromFile("rta_logo.png"), 
            ContentMode = UIViewContentMode.ScaleAspectFit
        };

        var titleLabel = new UILabel
        {
            Text = "RTC Demo",
            TextAlignment = UITextAlignment.Center,
            Font = UIFont.BoldSystemFontOfSize(20),
            BackgroundColor = UIColor.Clear,
            TextColor = UIColor.Black,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        _statusIndicator = new UIView
        {
            BackgroundColor = UIColor.Red,
            Layer =
            {
                CornerRadius = 10,
                MasksToBounds = true,
                BorderColor = UIColor.Black.CGColor,
                BorderWidth = 0.5f,
            },
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        View.AddSubview(logo);
        View.AddSubview(titleLabel);
        View.AddSubview(_statusIndicator);

        NSLayoutConstraint.ActivateConstraints([
            logo.WidthAnchor.ConstraintEqualTo(30),
            logo.HeightAnchor.ConstraintEqualTo(30),
            logo.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, 20),
            logo.CenterYAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 20),

            titleLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
            titleLabel.CenterYAnchor.ConstraintEqualTo(logo.CenterYAnchor),

            _statusIndicator.WidthAnchor.ConstraintEqualTo(20),
            _statusIndicator.HeightAnchor.ConstraintEqualTo(20),
            _statusIndicator.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -20),
            _statusIndicator.CenterYAnchor.ConstraintEqualTo(logo.CenterYAnchor)
        ]);

        var layout = new UICollectionViewFlowLayout
        {
            ItemSize = new CGSize(View.Bounds.Width - 40, 80),
            MinimumLineSpacing = 10,
            SectionInset = new UIEdgeInsets(10, 20, 10, 20)
        };

        _clientsCollectionView = new UICollectionView(CGRect.Empty, layout)
        {
            BackgroundColor = UIColor.White,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        _clientsCollectionView.RegisterClassForCell(typeof(ContactsCollectionViewCell), "ContactsViewCell");

        var clientsSource = new ContactsCollectionViewSource(_connectedClients, OnClientSelected);
        _clientsCollectionView.DataSource = clientsSource;
        _clientsCollectionView.Delegate = clientsSource;

        _emptyView = new EmptyView("ic_empty.png", "No contacts online!");
        View.AddSubview(_clientsCollectionView);
        View.AddSubview(_emptyView);

        NSLayoutConstraint.ActivateConstraints([
            _clientsCollectionView.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 20),
            _clientsCollectionView.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
            _clientsCollectionView.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor),
            _clientsCollectionView.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor),

            _emptyView.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
            _emptyView.CenterYAnchor.ConstraintEqualTo(View.CenterYAnchor)
        ]);

        UpdateEmptyViewVisibility();

        SignalrService.ConnectionStatusChanged += OnConnectionStatusChanged;
        SignalrService.ConnectedClientsUpdated += OnConnectedClientsUpdated;
        SignalrService.ClientDisconnected += ClientDisconnected;
        SignalrService.IncomingCallReceived += IncomingCallReceived;
        SignalrService.Closed += Closed;
        SignalrService.CancelCalls += SignalrServiceOnCancelCalls;

        await SignalrService.InitializeAsync();
    }

    private void OnConnectionStatusChanged(object sender, ConnectionState state)
    {
        InvokeOnMainThread(() =>
        {
            _statusIndicator.BackgroundColor = state switch
            {
                ConnectionState.Connected => UIColor.Green,
                ConnectionState.Connecting => UIColor.Yellow,
                ConnectionState.Failed or ConnectionState.Disconnected => UIColor.Red,
                _ => _statusIndicator.BackgroundColor
            };
        });
    }

    private void UpdateEmptyViewVisibility()
    {
        var isEmpty = _connectedClients.Count == 0;
        _emptyView.SetVisibility(isEmpty);
        _clientsCollectionView.Hidden = isEmpty;
    }

    private void Closed(object sender, EventArgs e)
    {
        InvokeOnMainThread(() =>
        {
            _connectedClients.Clear();
            _clientsCollectionView.ReloadData();
            UpdateEmptyViewVisibility();
        });
    }

    private void OnConnectedClientsUpdated(object sender, List<Client> clients)
    {
        if (clients == null || clients.Count == 0)
        {
            InvokeOnMainThread(() =>
            {
                _connectedClients.Clear();
                UpdateEmptyViewVisibility();
            });
            return;
        }

        var validClients = clients.Where(client => !string.IsNullOrEmpty(client.Name)).ToList();
        if (validClients.Count == 0) return;

        InvokeOnMainThread(() =>
        {
            _connectedClients.Clear();
            _connectedClients.AddRange(validClients);
            _clientsCollectionView.ReloadData();
            UpdateEmptyViewVisibility();
        });
    }

    private void ClientDisconnected(object sender, Client e)
    {
        InvokeOnMainThread(() =>
        {
            _connectedClients.RemoveAll(c => c.Id == e.Id);
            _clientsCollectionView.ReloadData();
            UpdateEmptyViewVisibility();
        });
    }

    private async void OnClientSelected(Client client)
    {
        await SignalrService.RequestCall(client.Id);

        var callingViewController = new CallingViewController(client);
        NavigationController?.PushViewController(callingViewController, animated: true);
    }

    private void IncomingCallReceived(object sender, Client client)
    {
        _otherClient = client;

        InvokeOnMainThread(() =>
        {
            _incomingCallControl = new IncomingCallControl();
            _incomingCallControl.OnAccept += OverlayControl_OnAccept;
            _incomingCallControl.OnDecline += OverlayControl_OnDecline;
            _incomingCallControl.ShowInView(View, client);
        });
    }

    private async void OverlayControl_OnAccept(object sender, Client client)
    {
        try
        {
            AudioService.StopSound();

            await SignalrService.AcceptCall(client.Id);

            var callViewController = new CallViewController(client, false);
            NavigationController?.PushViewController(callViewController, animated: true);

            _incomingCallControl.Close();
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString());
        }
    }

    private async void OverlayControl_OnDecline(object sender, EventArgs e)
    {
        try
        {
            Logger.Log("Call Declined!");
            _incomingCallControl.Close();
            await SignalrService.DeclineCall(_otherClient.Id);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }

    private void SignalrServiceOnCancelCalls(object sender, EventArgs e)
    {
        InvokeOnMainThread(() => { _incomingCallControl?.Close(); });
    }
}