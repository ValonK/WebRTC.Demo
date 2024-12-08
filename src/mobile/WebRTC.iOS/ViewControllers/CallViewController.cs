using WebRTC.iOS.Models;
using WebRTC.iOS.Services.SignalR;

namespace WebRTC.iOS.ViewControllers;

public class CallViewController(Client targetClient, ISignalRService signalRService) : UIViewController
{
    private readonly Client _targetClient = targetClient ?? throw new ArgumentNullException(nameof(targetClient));
    private readonly ISignalRService _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));

    private UILabel _statusLabel;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View.BackgroundColor = UIColor.White;

        var titleLabel = new UILabel
        {
            Text = $"Calling {_targetClient.Name}",
            TextAlignment = UITextAlignment.Center,
            Font = UIFont.BoldSystemFontOfSize(20),
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        _statusLabel = new UILabel
        {
            Text = "Waiting for the other client to accept...",
            TextAlignment = UITextAlignment.Center,
            Font = UIFont.SystemFontOfSize(16),
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        var stackView = new UIStackView
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Center,
            Distribution = UIStackViewDistribution.EqualSpacing,
            Spacing = 20,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        stackView.AddArrangedSubview(titleLabel);
        stackView.AddArrangedSubview(_statusLabel);

        View.AddSubview(stackView);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            stackView.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
            stackView.CenterYAnchor.ConstraintEqualTo(View.CenterYAnchor),
            stackView.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, 20),
            stackView.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -20)
        });

        // Listen for connection response
        // _signalRService.ConnectionRequestReceived += OnConnectionRequestReceived;
    }

    private void OnConnectionRequestReceived(string connectionId)
    {
        // Ensure this is the target client
        if (connectionId == _targetClient.Id)
        {
            InvokeOnMainThread(() =>
            {
                _statusLabel.Text = "Call accepted. Establishing connection...";

                // Proceed with WebRTC connection setup
                // For example, you could navigate to a VideoCallViewController
                // var videoCallViewController = new VideoCallViewController(_targetClient, _signalRService);
                // NavigationController?.PushViewController(videoCallViewController, true);
            });
        }
    }

    public override void ViewWillDisappear(bool animated)
    {
        base.ViewWillDisappear(animated);

        // Unsubscribe to prevent memory leaks
        // _signalRService.ConnectionRequestReceived -= OnConnectionRequestReceived;
    }
}

