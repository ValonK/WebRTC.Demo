using WebRTC.Bindings.iOS;

namespace RTCDemo.iOS.Services.RTC;

public interface IRtcService
{
    public bool IsConnected { get; }
    
    public event EventHandler<NSObject> DataReceived;
    public event EventHandler<string> MessageReceived;
    public event EventHandler<bool> RtcConnectionChanged;
    public event EventHandler<RTCIceConnectionState> IceConnectionChanged;
    public event EventHandler<RTCIceCandidate> IceCandidateGenerated;
    public event EventHandler DataChannelOpened;
    
    public UIView LocalVideoView { get; }
    public UIView RemoteVideoView { get; }

    void SetupMediaTracks();
    
    void Connect(Action<RTCSessionDescription, NSError> completionHandler);
    void OfferReceived(RTCSessionDescription offerSdp, Action<RTCSessionDescription, NSError> completionHandler);
    void AnswerReceived(RTCSessionDescription answerSdp, Action<RTCSessionDescription, NSError> completionHandler);
    void CandiateReceived(RTCIceCandidate candidate);
    void Disconnect();
}