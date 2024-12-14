using RTCDemo.iOS.Models;
using RTCDemo.iOS.Services.RTC;
using WebRTC.Bindings.iOS;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.ViewControllers;

public class CallViewController(Client client, bool isCaller) : UIViewController, IWebRtcService
{
    private UIView _buttonSection;
    private UIButton _endCallButton;
    private UIView _targetPreview;
    private UIView _selfPreview;
    private UILabel _endCallLabel;

    private WebRtcService _webRtcService;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        SignalrService.SignalingDataReceived += OnSignalingDataReceived;

        _webRtcService = new WebRtcService(this);
        _webRtcService.SetupMediaTracks();

        InitializeUI();


        if (_webRtcService.LocalVideoView != null)
        {
            _selfPreview.AddSubview(_webRtcService.LocalVideoView);
            _webRtcService.LocalVideoView.Frame = _selfPreview.Bounds;
            _webRtcService.LocalVideoView.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }

        if (_webRtcService.RemoteVideoView != null)
        {
            _targetPreview.AddSubview(_webRtcService.RemoteVideoView);
            _webRtcService.RemoteVideoView.Frame = _targetPreview.Bounds;
            _webRtcService.RemoteVideoView.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }
    }

    private void InitializeUI()
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

        View.AddSubviews(_targetPreview, _selfPreview, _buttonSection);
        _buttonSection.AddSubviews(_endCallButton, _endCallLabel);

        StartCall();
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        const int sectionHeight = 200;
        _buttonSection.Frame = new CGRect(0, View.Bounds.Height - sectionHeight, View.Bounds.Width, sectionHeight);

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

    private async void EndCallButton_TouchUpInside(object sender, EventArgs e)
    {
        _webRtcService?.Disconnect();
        DismissViewController(true, null);
    }

    #region IWebRtcService Implementation

    public void DidConnectWebRtc()
    {
        Console.WriteLine("WebRTC connected");
    }

    public void DidDisconnectWebRtc()
    {
        Console.WriteLine("WebRTC disconnected");
    }

    public void DidGenerateCandiate(RTCIceCandidate candidate)
    {
        // Send this candidate via SignalR to the remote peer
        // Example:
        // signalRConnection.SendAsync("SendCandidate", new {
        //     candidate = candidate.Sdp,
        //     sdpMid = candidate.SdpMid,
        //     sdpMLineIndex = candidate.SdpMLineIndex
        // });
    }

    public void DidIceConnectionStateChanged(RTCIceConnectionState state)
    {
        Console.WriteLine($"ICE Connection State Changed: {state}");
    }

    public void DidReceiveMessage(NSString message)
    {
        Console.WriteLine($"Received message: {message}");
    }

    public void DidReceiveData(NSData data)
    {
        Console.WriteLine("Received binary data");
    }

    public void DidOpenDataChannel()
    {
        Console.WriteLine("Data channel opened");
    }

    #endregion

    #region Signaling Handling Methods (Call these when you get messages from SignalR)

    public void HandleRemoteOffer(string sdp)
    {
        var remoteOffer = new RTCSessionDescription(RTCSdpType.Offer, sdp);
        _webRtcService.ReceiveOffer(remoteOffer, (answerSdp, error) =>
        {
            if (error == null)
            {
                // Send answerSdp.Sdp back to the caller via SignalR
            }
        });
    }

    public void HandleRemoteAnswer(string sdp)
    {
        var remoteAnswer = new RTCSessionDescription(RTCSdpType.Answer, sdp);
        _webRtcService.ReceiveAnswer(remoteAnswer, (answer, error) =>
        {
            if (error == null)
            {
                Console.WriteLine("Answer set successfully");
            }
        });
    }

    public void HandleRemoteCandidate(string candidate, string sdpMid, int sdpMLineIndex)
    {
        var iceCandidate = new RTCIceCandidate(candidate, Convert.ToInt32(sdpMid), sdpMLineIndex.ToString());
        _webRtcService.ReceiveCandidate(iceCandidate);
    }

    #endregion

    private void StartCall()
    {
        if (isCaller)
        {
            _webRtcService.StartCall(async (offerSdp, error) =>
            {
                if (error == null)
                {
                    // Send the created offer SDP to the remote peer via SignalR
                    var signalingMessage = new SignalingMessage
                    {
                        Type = "offer",
                        Sdp = offerSdp.Sdp
                    };
    
                    // Assuming you have the targetClientId from when you initiated the call
                    await SignalrService.SendSignalingData(client, signalingMessage);
                }
            });
        }
    }

    private void OnSignalingDataReceived(object o, (Client cl, SignalingMessage signalingData) data)
    {
        Console.WriteLine($"Signaling data received from {client.Name}: {data.signalingData}");

        switch (data.signalingData.Type)
        {
            case "offer":
                var remoteOffer = new RTCSessionDescription(RTCSdpType.Offer, data.signalingData.Sdp);
                _webRtcService.ReceiveOffer(remoteOffer, (answerSdp, error) =>
                {
                    if (error == null)
                    {
                        var answerMessage = new SignalingMessage
                        {
                            Type = "answer",
                            Sdp = answerSdp.Sdp
                        };
                        SignalrService.SendSignalingData(client, answerMessage);
                    }
                    else
                    {
                        Console.WriteLine("Error receiving offer: " + error.LocalizedDescription);
                    }
                });
                break;

            case "answer":
                // This means we are the caller and we got the callee's answer.
                var remoteAnswer = new RTCSessionDescription(RTCSdpType.Answer, data.signalingData.Sdp);
                _webRtcService.ReceiveAnswer(remoteAnswer, (ans, error) =>
                {
                    if (error == null)
                    {
                        Console.WriteLine("Remote answer set successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Error receiving answer: " + error.LocalizedDescription);
                    }
                });
                break;

            case "candidate":
                // Received an ICE candidate from the remote peer
                if (data.signalingData.Candidate != null)
                {
                    var iceCandidate = new RTCIceCandidate(data.signalingData.Candidate.Sdp,
                        Convert.ToInt32(data.signalingData.Candidate.SdpMid),
                        data.signalingData.Candidate.SdpMLineIndex.ToString());
                    _webRtcService.ReceiveCandidate(iceCandidate);
                }

                break;
        }
    }
}