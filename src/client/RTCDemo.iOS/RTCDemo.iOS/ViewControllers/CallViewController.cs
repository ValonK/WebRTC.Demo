using System.Diagnostics.CodeAnalysis;
using RTCDemo.iOS.Models;
using RTCDemo.iOS.Services.Call;
using RTCDemo.iOS.Services.RTC;
using WebRTC.Bindings.iOS;

namespace RTCDemo.iOS.ViewControllers;

[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
public class CallViewController(Client client, bool isCaller) : BaseViewController
{
    private readonly Client _client = client ?? throw new ArgumentNullException(nameof(client));

    private IRtcService _rtcService;
    
    private UIView _buttonSection;
    private UIImageView _clientImageView;
    private UILabel _clientNameLabel;
    private UILabel _sessionTimerLabel;
    private UIButton _endCallButton;

    private UIView _targetPreview; 
    private UIView _selfPreview;   

    private NSTimer _timer;
    private DateTime _startTime;

    private CallManager _callManager;

    private bool _isLocalFullScreen;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        InitializeUi();

        _rtcService = new RtcService();
        
        _callManager = new CallManager(_rtcService, _client, isCaller);
        _callManager.CallEnded += OnCallEnded;
        _callManager.CallCanceled += OnCallCanceled;
        _callManager.StartCall();

        _startTime = DateTime.Now;
        _timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromSeconds(1), SessionTimerTick);
        
        if (_rtcService.LocalVideoView != null) AddVideoView(_selfPreview, _rtcService.LocalVideoView);   
        if (_rtcService.RemoteVideoView != null) AddVideoView(_targetPreview, _rtcService.RemoteVideoView); 
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        const int imageSize = 60;
        const int sectionHeight = 100;

        _buttonSection.Frame = new CGRect(
            x: 0,
            y: View!.Bounds.Height - sectionHeight,
            width: View.Bounds.Width,
            height: sectionHeight
        );

        _targetPreview.Frame = new CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);

        const int selfPreviewWidth = 120;
        const int selfPreviewHeight = 180;
        _selfPreview.Frame = new CGRect(
            x: View.Bounds.Width - selfPreviewWidth - 20,
            y: View.Bounds.Height - sectionHeight - selfPreviewHeight - 20,
            width: selfPreviewWidth,
            height: selfPreviewHeight
        );

        if (_rtcService.RemoteVideoView.Superview.Equals(_targetPreview)) _rtcService.RemoteVideoView.Frame = _targetPreview.Bounds;

        nfloat verticalCenter = (sectionHeight - imageSize) / 2;

        _clientImageView.Frame = new CGRect(
            x: 20,
            y: verticalCenter,
            width: imageSize,
            height: imageSize
        );

        _endCallButton.Frame = new CGRect(
            x: _buttonSection.Bounds.Width - imageSize - 20,
            y: verticalCenter,
            width: imageSize,
            height: imageSize
        );

        const int nameLabelHeight = 20;
        const int timerLabelHeight = 16;
        const int totalLabelStackHeight = nameLabelHeight + 5 + timerLabelHeight;

        var labelsX = _clientImageView.Frame.Right + 10;
        var availableWidthForName = _buttonSection.Bounds.Width - labelsX - (imageSize + 20);
        nfloat stackTop = (sectionHeight - totalLabelStackHeight) / 2f;

        _clientNameLabel.Frame = new CGRect(
            x: labelsX,
            y: stackTop,
            width: availableWidthForName,
            height: nameLabelHeight
        );

        _sessionTimerLabel.Frame = new CGRect(
            x: labelsX,
            y: _clientNameLabel.Frame.Bottom + 5,
            width: availableWidthForName,
            height: timerLabelHeight
        );
    }

    private void SessionTimerTick(NSTimer timer)
    {
        var elapsed = DateTime.Now - _startTime;
        _sessionTimerLabel.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
    }

    private void EndCallButton_TouchUpInside(object sender, EventArgs e)
    {
        _callManager.EndCall();
    }

    private void OnCallEnded() => CloseScreen();
    private void OnCallCanceled() => CloseScreen();

    private void CloseScreen()
    {
        InvokeOnMainThread(() =>
        {
            _timer?.Invalidate();
            _timer = null;

            _callManager?.Dispose();
            _callManager = null;
        
            if (isCaller)
            {
                NavigationController?.PopToRootViewController(true);
            }
            else
            {
                NavigationController?.PopViewController(true);
            }
        });
    }
    
    private void InitializeUi()
    {
        _buttonSection = new UIView
        {
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.8f)
        };

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
            Text = _client.Name,
            Font = UIFont.BoldSystemFontOfSize(18f),
            TextColor = UIColor.White
        };

        _sessionTimerLabel = new UILabel
        {
            Text = "00:00",
            Font = UIFont.SystemFontOfSize(14f),
            TextColor = UIColor.LightGray
        };

        _endCallButton = CreateEndCallButton();

        _selfPreview = CreatePreviewView(UIColor.Clear, isSelfPreview: true);
        _selfPreview.UserInteractionEnabled = true;
        var tapGesture = new UITapGestureRecognizer(SwapLocalAndRemotePreviews);
        _selfPreview.AddGestureRecognizer(tapGesture);

        _targetPreview = CreatePreviewView(UIColor.Black);

        View!.AddSubviews(_targetPreview, _selfPreview, _buttonSection);
        _buttonSection.AddSubviews(
            _clientImageView,
            _clientNameLabel,
            _sessionTimerLabel,
            _endCallButton
        );
    }

    private UIButton CreateEndCallButton()
    {
        var button = new UIButton(UIButtonType.Custom)
        {
            BackgroundColor = UIColor.Red,
            Layer = { CornerRadius = 30 },
            ClipsToBounds = true
        };

        var icon = UIImage.FromBundle("ic_close");
        button.SetImage(icon, UIControlState.Normal);
        button.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
        button.ImageEdgeInsets = new UIEdgeInsets(15, 15, 15, 15);
        button.TouchUpInside += EndCallButton_TouchUpInside;

        return button;
    }
    
    private void SwapLocalAndRemotePreviews()
    {
        UIView.Transition(
            View,
            0.3,
            UIViewAnimationOptions.TransitionCrossDissolve,
            () =>
            {
                _selfPreview.Subviews.ToList().ForEach(v => v.RemoveFromSuperview());
                _targetPreview.Subviews.ToList().ForEach(v => v.RemoveFromSuperview());

                if (_isLocalFullScreen)
                {
                    AddVideoView(_selfPreview, _rtcService.LocalVideoView);
                    AddVideoView(_targetPreview, _rtcService.RemoteVideoView);
                }
                else
                {
                    AddVideoView(_targetPreview, _rtcService.LocalVideoView);
                    AddVideoView(_selfPreview, _rtcService.RemoteVideoView);
                }

                _isLocalFullScreen = !_isLocalFullScreen;
            },
            null
        );
    }

    private static UIView CreatePreviewView(UIColor backgroundColor, bool isSelfPreview = false)
    {
        var view = new UIView
        {
            BackgroundColor = backgroundColor,
            Layer =
            {
                CornerRadius = isSelfPreview ? 10 : 0,
                BorderColor = UIColor.White.CGColor,
                BorderWidth = isSelfPreview ? 1 : 0
            }
        };

        if (!isSelfPreview) return view;
        
        view.Layer.ShadowColor = UIColor.Black.CGColor;
        view.Layer.ShadowOpacity = 0.5f;
        view.Layer.ShadowOffset = new CGSize(0, 2);
        view.Layer.ShadowRadius = 4;

        return view;
    }

    private static void AddVideoView(UIView container, UIView videoView)
    {
        container.AddSubview(videoView);

        if (Math.Abs(container.Frame.Width - 120) < 0.1 && 
            Math.Abs(container.Frame.Height - 180) < 0.1)
        {
            videoView.Frame = new CGRect(0, 0, 120, 180);
            videoView.AutoresizingMask = UIViewAutoresizing.None;
        }
        else
        {
            videoView.Frame = container.Bounds;
            videoView.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }

        videoView.ClipsToBounds = true;
        videoView.Layer.CornerRadius = container.Layer.CornerRadius;
        videoView.Layer.MasksToBounds = true;

        switch (videoView)
        {
            case RTCMTLVideoView metalView:
                metalView.VideoContentMode = UIViewContentMode.ScaleAspectFit;
                break;
            case RTCEAGLVideoView eglView:
                eglView.ContentMode = UIViewContentMode.ScaleAspectFit;
                eglView.ClipsToBounds = true;
                break;
            default:
                videoView.ContentMode = UIViewContentMode.ScaleAspectFit;
                break;
        }
    }
}
