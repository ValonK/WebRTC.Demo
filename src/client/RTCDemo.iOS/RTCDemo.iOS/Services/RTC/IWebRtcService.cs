
using WebRTC.Bindings.iOS;

namespace RTCDemo.iOS.Services.RTC;

public interface IWebRtcService
{
    void DidConnectWebRtc();
    void DidDisconnectWebRtc();
    void DidGenerateCandiate(RTCIceCandidate candidate);
    void DidIceConnectionStateChanged(RTCIceConnectionState state);
    void DidReceiveMessage(NSString message);
    void DidReceiveData(NSData data);
    void DidOpenDataChannel();
}
