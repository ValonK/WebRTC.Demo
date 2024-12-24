using RTCDemo.iOS.Models;
using RTCDemo.iOS.Services.RTC;
using WebRTC.Bindings.iOS;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.Services.Call;

public class CallManager(IRtcService rtcService, Client client, bool isCaller)
{
    private readonly Client _client = client ?? throw new ArgumentNullException(nameof(client));

    public event Action CallEnded;
    public event Action CallCanceled;

    /// <summary>
    /// Initializes RTC and starts the call if the user is the caller.
    /// </summary>
    public void StartCall()
    {
        SignalrService.SignalingDataReceived += OnSignalingDataReceived;
        SignalrService.CancelCalls += OnCancelCalls;
        rtcService.IceCandidateGenerated += OnIceCandidateGenerated;

        rtcService.SetupMediaTracks();

        if (isCaller)
        {
            rtcService.Connect(async (sdp, err) =>
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

    public void EndCall()
    {
        SignalrService.CancelCall();
        CallEnded?.Invoke();
    }

    private void OnCancelCalls(object sender, EventArgs e) => CallCanceled?.Invoke();

    private async void OnIceCandidateGenerated(object sender, RTCIceCandidate iceCandidate)
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
        catch (Exception ex)
        {
            Logger.Log($"Error sending ICE candidate: {ex}");
        }
    }

    private void OnSignalingDataReceived(object sender, (Client targetClient, SignalingMessage signalingMessage) data)
    {
        if (data.signalingMessage == null) return;

        var messageType = data.signalingMessage.Type;

        // Offer
        if (messageType?.Equals(RTCSdpType.Offer.ToString(), StringComparison.OrdinalIgnoreCase) == true)
        {
            Logger.Log("Offer received");

            rtcService.OfferReceived(
                new RTCSessionDescription(RTCSdpType.Offer, data.signalingMessage.Sdp),
                async void (sdp, err) =>
                {
                    try
                    {
                        if (err == null)
                        {
                            await SendSignalingMessage(sdp);
                        }
                        else
                        {
                            Logger.Log(err.LocalizedDescription);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString());
                    }
                }
            );
        }
        // Answer
        else if (messageType?.Equals(RTCSdpType.Answer.ToString(), StringComparison.OrdinalIgnoreCase) == true)
        {
            Logger.Log("Answer received");

            rtcService.AnswerReceived(
                new RTCSessionDescription(RTCSdpType.Answer, data.signalingMessage.Sdp),
                (_, err) => Logger.Log(err?.LocalizedDescription)
            );
        }
        else if (data.signalingMessage.Candidate != null)
        {
            var candidate = data.signalingMessage.Candidate;
            rtcService.CandiateReceived(
                new RTCIceCandidate(candidate.Sdp, candidate.SdpMLineIndex, candidate.SdpMid)
            );
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

    public void Dispose()
    {
        SignalrService.SignalingDataReceived -= OnSignalingDataReceived;
        SignalrService.CancelCalls -= OnCancelCalls;
        rtcService.IceCandidateGenerated -= OnIceCandidateGenerated;

        SignalrService.CancelCall();
        rtcService?.Disconnect();
    }
}