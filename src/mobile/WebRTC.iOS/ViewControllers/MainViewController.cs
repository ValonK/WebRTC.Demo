using WebRTC.iOS.Models;
using WebRTC.iOS.Services.SignalR;

namespace WebRTC.iOS.ViewControllers;

public class MainViewController(ISignalRService signalRService) : UIViewController
{
    private UICollectionView _clientsCollectionView;
    private readonly List<Client> _connectedClients = new();

    public override async void ViewDidLoad()
    {
        base.ViewDidLoad();
        View.BackgroundColor = UIColor.White;

        var titleLabel = new UILabel
        {
            Text = "Connected Clients",
            Frame = new CGRect(20, 60, View.Bounds.Width - 40, 40),
            TextAlignment = UITextAlignment.Center,
            Font = UIFont.BoldSystemFontOfSize(20)
        };

        var layout = new UICollectionViewFlowLayout
        {
            ItemSize = new CGSize(View.Bounds.Width - 40, 80),
            MinimumLineSpacing = 10,
            SectionInset = new UIEdgeInsets(10, 20, 10, 20)
        };

        _clientsCollectionView = new UICollectionView(CGRect.Empty, layout)
        {
            Frame = new CGRect(0, 120, View.Bounds.Width, View.Bounds.Height - 200),
            BackgroundColor = UIColor.White,
        };

        _clientsCollectionView.RegisterClassForCell(typeof(ClientCell), "ClientCell");

        _clientsCollectionView.DataSource = new ClientsCollectionViewSource(_connectedClients, OnClientSelected);

        View.AddSubviews(titleLabel, _clientsCollectionView);
        
        signalRService.ClientConnected += OnClientConnected;
        signalRService.ConnectedClientsUpdated += OnConnectedClientsUpdated;
        signalRService.ClientDisconnected += SignalRServiceOnClientDisconnected;
        signalRService.Closed += Closed;
        await signalRService.StartConnectionAsync("http://192.168.0.86:5136/signalhub");
    }

    private void Closed(object sender, EventArgs e)
    {
        InvokeOnMainThread(() =>
        {
            _connectedClients.Clear();
            _clientsCollectionView.ReloadData();
        });
    }

    private void OnConnectedClientsUpdated(object sender, List<Client> clients)
    {
        InvokeOnMainThread(() =>
        {
            _connectedClients.Clear();
            _connectedClients.AddRange(clients);
            _clientsCollectionView.ReloadData();
        });
    }

    private void SignalRServiceOnClientDisconnected(object sender, Client e)
    {
        InvokeOnMainThread(() =>
        {
            _connectedClients.RemoveAll(c => c.Id == e.Id);
            _clientsCollectionView.ReloadData();
        });
    }

    private void OnClientConnected(object sender, Client client)
    {
        InvokeOnMainThread(() =>
        {
            _connectedClients.Add(client);
            _clientsCollectionView.ReloadData();
        });
    }

    private void OnClientSelected(Client client)
    {
        signalRService.RequestConnection(client.Id);
        var alert = UIAlertController.Create("Request Sent", $"Requested connection to {client.Name}",
            UIAlertControllerStyle.Alert);
        alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
        PresentViewController(alert, true, null);
    }
}

public class ClientsCollectionViewSource(List<Client> clients, Action<Client> onClientSelected)
    : UICollectionViewDataSource
{
    private readonly Action<Client> _onClientSelected = onClientSelected;

    public override nint GetItemsCount(UICollectionView collectionView, nint section) => clients.Count;

    public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
    {
        var cell = collectionView.DequeueReusableCell("ClientCell", indexPath) as ClientCell;
        var client = clients[indexPath.Row];
        cell?.Configure(client);
        return cell;
    }
}

public class ClientCell : UICollectionViewCell
{
    private readonly UILabel _nameLabel;
    private readonly UILabel _connectionIdLabel;

    [Export("initWithFrame:")]
    public ClientCell(CGRect frame) : base(frame)
    {
        ContentView.Layer.CornerRadius = 8;
        ContentView.Layer.BorderColor = UIColor.LightGray.CGColor;
        ContentView.Layer.BorderWidth = 1;
        ContentView.BackgroundColor = UIColor.White;

        _nameLabel = new UILabel
        {
            Frame = new CGRect(10, 10, ContentView.Bounds.Width - 20, 20),
            Font = UIFont.BoldSystemFontOfSize(16),
            TextColor = UIColor.Black
        };

        _connectionIdLabel = new UILabel
        {
            Frame = new CGRect(10, 40, ContentView.Bounds.Width - 20, 20),
            Font = UIFont.SystemFontOfSize(14),
            TextColor = UIColor.Gray
        };

        ContentView.AddSubviews(_nameLabel, _connectionIdLabel);
    }

    public void Configure(Client client)
    {
        _nameLabel.Text = $"Name: {client.Name}";
        _connectionIdLabel.Text = $"ID: {client.Id}";
    }
}
