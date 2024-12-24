using System.Diagnostics.CodeAnalysis;
using CoreAnimation;
using RTCDemo.iOS.Models;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.ViewControllers;

[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
public class CallingViewController(Client client) : BaseViewController
{
    private UIView _bottomBar;
    private UIImageView _clientImageView;
    private UILabel _clientNameLabel;
    private UILabel _statusLabel;       
    private UIButton _endCallButton;

    private CAGradientLayer _gradientLayer;

    private NSTimer _statusAnimationTimer;
    private int _dotCount;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        SetupGradientBackground();
        SetupBottomBar();
        StartStatusAnimation();

        AudioService.PlaySound("dialingSound");

        SignalrService.CallAccepted += SignalrServiceOnCallAccepted;
        SignalrService.CallDeclined += CallDeclined;
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        _gradientLayer.Frame = View!.Bounds;

        const int bottomBarHeight = 100;
        _bottomBar.Frame = new CGRect(
            0,
            View.Bounds.Height - bottomBarHeight,
            View.Bounds.Width,
            bottomBarHeight
        );

        const int imageSize = 60;
        nfloat verticalCenter = (bottomBarHeight - imageSize) / 2f;

        _clientImageView.Frame = new CGRect(
            20,
            verticalCenter,
            imageSize,
            imageSize
        );

        _endCallButton.Frame = new CGRect(
            _bottomBar.Bounds.Width - imageSize - 20,
            verticalCenter,
            imageSize,
            imageSize
        );

        const int nameLabelHeight = 20;
        const int statusLabelHeight = 20;
        const int totalLabelStackHeight = nameLabelHeight + 5 + statusLabelHeight;

        var labelsX = _clientImageView.Frame.Right + 10;
        var availableWidth = _bottomBar.Bounds.Width - labelsX - (imageSize + 20);
        nfloat stackTop = (bottomBarHeight - totalLabelStackHeight) / 2f;

        _clientNameLabel.Frame = new CGRect(
            labelsX,
            stackTop,
            availableWidth,
            nameLabelHeight
        );

        _statusLabel.Frame = new CGRect(
            labelsX,
            _clientNameLabel.Frame.Bottom + 5,
            availableWidth,
            statusLabelHeight
        );
    }

    private void SetupGradientBackground()
    {
        _gradientLayer = new CAGradientLayer
        {
            Colors =
            [
                UIColor.FromRGB(83, 100, 115).CGColor,
                UIColor.FromRGB(20, 38, 69).CGColor
            ],
            StartPoint = new CGPoint(0, 0),
            EndPoint = new CGPoint(1, 1)
        };
        View!.Layer.InsertSublayer(_gradientLayer, 0);
    }

    private void SetupBottomBar()
    {
        _bottomBar = new UIView
        {
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.8f)
        };
        View!.AddSubview(_bottomBar);

        _clientImageView = new UIImageView
        {
            ContentMode = UIViewContentMode.ScaleAspectFill,
            ClipsToBounds = true,
            Image = UIImage.FromBundle("ic_user")
        };
        _clientImageView.Layer.CornerRadius = 30f;
        _clientImageView.Layer.BorderWidth = 1f;
        _clientImageView.Layer.BorderColor = UIColor.White.CGColor;

        _clientNameLabel = new UILabel
        {
            Text = client.Name,
            Font = UIFont.BoldSystemFontOfSize(18f),
            TextColor = UIColor.White
        };

        _statusLabel = new UILabel
        {
            Text = "connecting",
            Font = UIFont.SystemFontOfSize(14f),
            TextColor = UIColor.LightGray
        };

        _endCallButton = new UIButton(UIButtonType.Custom)
        {
            BackgroundColor = UIColor.Red,
            Layer = { CornerRadius = 30 },
            ClipsToBounds = true
        };
        var icon = UIImage.FromBundle("ic_close");
        _endCallButton.SetImage(icon, UIControlState.Normal);
        _endCallButton.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
        _endCallButton.ImageEdgeInsets = new UIEdgeInsets(15, 15, 15, 15);
        _endCallButton.TouchUpInside += EndCallButton_TouchUpInside;

        _bottomBar.AddSubviews(
            _clientImageView,
            _clientNameLabel,
            _statusLabel,
            _endCallButton
        );
    }

    private void StartStatusAnimation()
    {
        _dotCount = 0;
        _statusAnimationTimer =
            NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(500), UpdateStatusLabel);
    }

    private void UpdateStatusLabel(NSTimer timer)
    {
        _dotCount = (_dotCount + 1) % 4;
        const string baseText = "connecting";
        var dots = new string('.', _dotCount);
        var spaces = new string(' ', 3 - _dotCount);
        _statusLabel.Text = $"{baseText}{dots}{spaces}";
    }

    private void StopStatusAnimation()
    {
        if (_statusAnimationTimer == null) return;
        _statusAnimationTimer.Invalidate();
        _statusAnimationTimer.Dispose();
        _statusAnimationTimer = null;
    }

    private async void EndCallButton_TouchUpInside(object sender, EventArgs e)
    {
        try
        {
            await SignalrService.CancelCall();
            InvokeOnMainThread(Close);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }

    private void SignalrServiceOnCallAccepted(object sender, Client e)
    {
        InvokeOnMainThread(() =>
        {
            AudioService.StopSound();
            var callViewController = new CallViewController(e, true);
            NavigationController?.PushViewController(callViewController, animated: true);
        });
    }

    private void CallDeclined(object sender, Client e)
    {
        InvokeOnMainThread(Close);
    }

    private void Close()
    {
        StopStatusAnimation();
        AudioService.StopSound();
        NavigationController?.PopToRootViewController(true);
    }
}
