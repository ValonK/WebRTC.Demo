using System.Diagnostics.CodeAnalysis;
using CoreAnimation;
using RTCDemo.iOS.Models;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.ViewControllers;

[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
public class CallingViewController(Client client) : UIViewController
{
    private UILabel _clientNameLabel;
    private UILabel _statusLabel;
    private UIButton _endCallButton;
    private UILabel _endCallLabel;
    private UIView _buttonSection;
    private NSTimer _statusAnimationTimer;

    private int _dotCount;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        var gradientLayer = new CAGradientLayer
        {
            Frame = View!.Bounds,
            Colors =
            [
                UIColor.FromRGB(83, 100, 115).CGColor,
                UIColor.FromRGB(20, 38, 69).CGColor
            ],
            StartPoint = new CGPoint(0, 0),
            EndPoint = new CGPoint(1, 1)
        };
        View.Layer.InsertSublayer(gradientLayer, 0);

        _clientNameLabel = new UILabel
        {
            Text = client.Name,
            Font = UIFont.BoldSystemFontOfSize(28),
            TextColor = UIColor.White,
            TextAlignment = UITextAlignment.Center
        };

        _statusLabel = new UILabel
        {
            Text = "connecting",
            Font = UIFont.SystemFontOfSize(20),
            TextColor = UIColor.LightGray,
            TextAlignment = UITextAlignment.Center,
            Lines = 1
        };

        _buttonSection = new UIView
        {
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.2f)
        };

        _endCallButton = new UIButton(UIButtonType.Custom);
        var icon = UIImage.FromBundle("ic_close");
        _endCallButton.SetImage(icon, UIControlState.Normal);
        _endCallButton.BackgroundColor = UIColor.Red;
        _endCallButton.Layer.CornerRadius = 35;
        _endCallButton.ClipsToBounds = true;
        _endCallButton.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
        _endCallButton.ImageEdgeInsets = new UIEdgeInsets(20, 20, 20, 20);
        _endCallButton.TouchUpInside += EndCallButton_TouchUpInside;

        _endCallLabel = new UILabel
        {
            Text = "End",
            Font = UIFont.SystemFontOfSize(16),
            TextColor = UIColor.White,
            TextAlignment = UITextAlignment.Center
        };

        View.AddSubviews(_clientNameLabel, _statusLabel, _buttonSection);
        _buttonSection.AddSubviews(_endCallButton, _endCallLabel);

        StartStatusAnimation();
        AudioService.PlaySound("dialingSound");

        SignalrService.CallAccepted += SignalrServiceOnCallAccepted;
        SignalrService.CallDeclined += CallDeclined;
    }
    
    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        _clientNameLabel.Frame = new CGRect(20, 100, View!.Bounds.Width - 40, 40);

        _statusLabel.Frame = new CGRect(20, _clientNameLabel.Frame.Bottom + 10, View.Bounds.Width - 40, 30);

        const int sectionHeight = 200;
        _buttonSection.Frame = new CGRect(0, View.Bounds.Height - sectionHeight, View.Bounds.Width, sectionHeight);

        const int buttonSize = 70;
        _endCallButton.Frame = new CGRect(
            (_buttonSection.Bounds.Width - buttonSize) / 2,
            (_buttonSection.Bounds.Height - buttonSize) / 2 - 15,
            buttonSize,
            buttonSize
        );

        _endCallLabel.Frame = new CGRect(
            _endCallButton.Frame.Left,
            _endCallButton.Frame.Bottom + 15,
            _endCallButton.Frame.Width,
            20
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
    
    private void SignalrServiceOnCallAccepted(object sender, Client e)
    {
        InvokeOnMainThread(() =>
        {
            AudioService.StopSound();
            var callingViewController = new CallViewController(e, true)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FullScreen
            };
            PresentViewController(callingViewController, animated: true, completionHandler: null);
        });
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

    private void CallDeclined(object sender, Client e)
    {
        InvokeOnMainThread(Close);
    }
    
    private void Close()
    {
        StopStatusAnimation();
        AudioService.StopSound();
        
        DismissViewController(true, null);
    }
}