using RTCDemo.iOS.Models;
using WebRTC.Bindings.iOS;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.ViewControllers;

public class CallViewController(Client client, bool isCaller) : UIViewController
{
    private readonly Client _client = client ?? throw new ArgumentNullException(nameof(client));
    private UIView _buttonSection;
    private UIButton _endCallButton;
    private UIView _targetPreview;
    private UIView _selfPreview;
    private UILabel _endCallLabel;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        InitializeUi();
        SetupRtc();

        if (RtcService.LocalVideoView != null)
        {
            AddVideoView(_selfPreview, RtcService.LocalVideoView);
        }

        if (RtcService.RemoteVideoView != null)
        {
            AddVideoView(_targetPreview, RtcService.RemoteVideoView);
        }
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        const int sectionHeight = 200;
        _buttonSection.Frame = new CGRect(0, View!.Bounds.Height - sectionHeight, View.Bounds.Width, sectionHeight);

        _targetPreview.Frame = new CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);

        const int selfPreviewWidth = 110;
        const int selfPreviewHeight = 150;
        _selfPreview.Frame = new CGRect(
            View.Bounds.Width - selfPreviewWidth - 10,
            View.Bounds.Height - sectionHeight - selfPreviewHeight - 10,
            selfPreviewWidth,
            selfPreviewHeight
        );

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SignalrService.SignalingDataReceived -= SignalrServiceOnSignalingDataReceived;
            RtcService.IceCandidateGenerated -= RtcServiceOnIceCandidateGenerated;
        }

        base.Dispose(disposing);
    }
    
    private void InitializeUi()
    {
        _buttonSection = new UIView
        {
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.2f)
        };

        _endCallButton = CreateEndCallButton();
        _selfPreview = CreatePreviewView(UIColor.Clear);
        _targetPreview = CreatePreviewView(UIColor.Black);

        _endCallLabel = new UILabel
        {
            Text = "End",
            Font = UIFont.SystemFontOfSize(16),
            TextColor = UIColor.White,
            TextAlignment = UITextAlignment.Center
        };

        View!.AddSubviews(_targetPreview, _selfPreview, _buttonSection);
        _buttonSection.AddSubviews(_endCallButton, _endCallLabel);
    }

    private UIButton CreateEndCallButton()
    {
        var button = new UIButton(UIButtonType.Custom)
        {
            BackgroundColor = UIColor.Red,
            Layer = { CornerRadius = 35 },
            ClipsToBounds = true
        };

        var icon = UIImage.FromBundle("ic_close");
        button.SetImage(icon, UIControlState.Normal);
        button.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
        button.ImageEdgeInsets = new UIEdgeInsets(20, 20, 20, 20);
        button.TouchUpInside += EndCallButton_TouchUpInside;

        return button;
    }

    private UIView CreatePreviewView(UIColor backgroundColor)
    {
        return new UIView
        {
            BackgroundColor = backgroundColor,
            Layer =
            {
                CornerRadius = 10,
                ShadowColor = UIColor.Black.CGColor,
                ShadowOpacity = 0.5f,
                ShadowOffset = new CGSize(0, 2),
                ShadowRadius = 4
            }
        };
    }

    private void AddVideoView(UIView container, UIView videoView)
    {
        container.AddSubview(videoView);
        videoView.Frame = container.Bounds;
        videoView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
    }
    
    private void SetupRtc()
    {
        try
        {
            SignalrService.SignalingDataReceived += SignalrServiceOnSignalingDataReceived;
            RtcService.IceCandidateGenerated += RtcServiceOnIceCandidateGenerated;
            RtcService.SetupMediaTracks();

            if (isCaller)
            {
                RtcService.Connect(async (sdp, err) =>
                {
                    if (err == null)
                    {
                        await SendSignalingMessage(sdp);
                    }
                    else
                    {
                        Logger.Log(err.LocalizedDescription);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during RTC setup: {ex}");
        }
    }

    private void EndCallButton_TouchUpInside(object sender, EventArgs e)
    {
        RtcService?.Disconnect();
        DismissViewController(true, null);
    }

    private void SignalrServiceOnSignalingDataReceived(object sender, (Client targetClient, SignalingMessage signalingMessage) data)
    {
        if (data.signalingMessage == null) return;

        var messageType = data.signalingMessage.Type;
        if (messageType?.Equals(RTCSdpType.Offer.ToString(), StringComparison.OrdinalIgnoreCase) == true)
        {
            Logger.Log("Offer received");
            RtcService.OfferReceived(new RTCSessionDescription(RTCSdpType.Offer, data.signalingMessage.Sdp), async void (sdp, err) =>
            {
                try
                {
                    if (err == null)
                    {
                        await SendSignalingMessage(sdp);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString());
                }
            });
        }
        else if (messageType?.Equals(RTCSdpType.Answer.ToString(), StringComparison.OrdinalIgnoreCase) == true)
        {
            Logger.Log("Answer received");
            RtcService.AnswerReceived(new RTCSessionDescription(RTCSdpType.Answer, data.signalingMessage.Sdp), (_, err) =>
            {
                Logger.Log(err?.LocalizedDescription);
            });
        }
        else if (data.signalingMessage.Candidate != null)
        {
            var candidate = data.signalingMessage.Candidate;
            RtcService.CandiateReceived(new RTCIceCandidate(candidate.Sdp, candidate.SdpMLineIndex, candidate.SdpMid));
        }
    }

    private async void RtcServiceOnIceCandidateGenerated(object sender, RTCIceCandidate iceCandidate)
    {
        try
        {
            var signalingMessage = new SignalingMessage
            {
                Candidate = new Candidate
                {
                    Sdp = iceCandidate.Sdp,
                    SdpMLineIndex = iceCandidate.SdpMLineIndex,
                    SdpMid = iceCandidate.SdpMid
                }
            };

            await SignalrService.SendSignalingData(_client, signalingMessage);
        }
        catch (Exception e)
        {
            Logger.Log($"Error sending ICE candidate: {e}");
        }
    }

    private async Task SendSignalingMessage(RTCSessionDescription sdp)
    {
        try
        {
            var signalingMessage = new SignalingMessage
            {
                Type = sdp.Type.ToString(),
                Sdp = sdp.Sdp
            };

            await SignalrService.SendSignalingData(_client, signalingMessage);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error sending signaling message: {ex}");
        }
    }
}