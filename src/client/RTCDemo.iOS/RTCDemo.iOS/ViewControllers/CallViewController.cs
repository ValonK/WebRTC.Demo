using RTCDemo.iOS.Models;
using WebRTC.Bindings.iOS;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.ViewControllers;

public class CallViewController(Client client, bool isCaller) : UIViewController
{
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
            _selfPreview.AddSubview(RtcService.LocalVideoView);
            RtcService.LocalVideoView.Frame = _selfPreview.Bounds;
            RtcService.LocalVideoView.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }

        if (RtcService.RemoteVideoView != null)
        {
            _targetPreview.AddSubview(RtcService.RemoteVideoView);
            RtcService.RemoteVideoView.Frame = _targetPreview.Bounds;
            RtcService.RemoteVideoView.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }
    }
    
    private void InitializeUi()
    {
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

        _selfPreview = new UIView
        {
            BackgroundColor = UIColor.Clear,
            Layer =
            {
                CornerRadius = 10,
                ShadowColor = UIColor.Black.CGColor,
                ShadowOpacity = 0.5f,
                ShadowOffset = new CGSize(0, 2),
                ShadowRadius = 4
            }
        };

        _targetPreview = new UIView
        {
            BackgroundColor = UIColor.Black
        };

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
    
    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        const int sectionHeight = 200;
        _buttonSection.Frame = new CGRect(0, View!.Bounds.Height - sectionHeight, View.Bounds.Width, sectionHeight);
        _targetPreview.Frame = new CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);

        const int selfPreviewWidth = 110;
        const int selfPreviewHeight = 150;
        _selfPreview.Frame = new CGRect(View.Bounds.Width - selfPreviewWidth - 10,
            View.Bounds.Height - sectionHeight - selfPreviewHeight - 10,
            selfPreviewWidth, selfPreviewHeight);

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

    private void EndCallButton_TouchUpInside(object sender, EventArgs e)
    {
        RtcService?.Disconnect();
        DismissViewController(true, null);
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
                RtcService.Connect(async void (sdp, err) =>
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
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }

    private void SignalrServiceOnSignalingDataReceived(object sender, 
        (Client targetClient, 
        SignalingMessage signalingMessage) data)
    {
        if (data.signalingMessage is null) return;
        
        if (data.signalingMessage.Type?.Equals(RTCSdpType.Offer.ToString(), StringComparison.OrdinalIgnoreCase) == true)
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
        else if (data.signalingMessage.Type?.Equals(RTCSdpType.Answer.ToString(), StringComparison.OrdinalIgnoreCase) == true)
        {
            Logger.Log("Answer received");
            RtcService.AnswerReceived(new RTCSessionDescription(RTCSdpType.Answer, data.signalingMessage.Sdp),
                (_, err) =>
                {
                    Logger.Log(err?.LocalizedDescription);
                });
        }
        else if (data.signalingMessage.Candidate != null)
        {
            RtcService.CandiateReceived(new RTCIceCandidate(
                data.signalingMessage.Candidate.Sdp, 
                data.signalingMessage.Candidate.SdpMLineIndex, 
                data.signalingMessage.Candidate.SdpMid));
        }
    }

    private async void RtcServiceOnIceCandidateGenerated(object sender, RTCIceCandidate iceCandidate)
    {
        try
        {
            var can = new Candidate
            {                
                Sdp = iceCandidate.Sdp,
                SdpMLineIndex = iceCandidate.SdpMLineIndex,
                SdpMid = iceCandidate.SdpMid
            };
            var signalingMessage = new SignalingMessage { Candidate = can };
            await SignalrService.SendSignalingData(client, signalingMessage);
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString());
        }
    }

    private async Task SendSignalingMessage(RTCSessionDescription sdp)
    {
        try
        {
            var signalingMessage = new SignalingMessage
            {
                Type = sdp.Type.ToString(),
                Sdp = sdp.Sdp,                
            };
        
            await SignalrService.SendSignalingData(client, signalingMessage);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }
}