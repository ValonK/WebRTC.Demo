using WebRTC.iOS.Models;

namespace WebRTC.iOS.Services.SignalR;

public interface ISignalRService
{
    event EventHandler Closed;
    event EventHandler<Client> ClientDisconnected;
    event EventHandler<List<Client>> ConnectedClientsUpdated;
    event EventHandler<Client> IncomingCallReceived;
    event EventHandler<Client> CallDeclined;
    event EventHandler<Client> CallAccepted;
    event EventHandler CallStarted;
    event EventHandler CallEnded;
    event EventHandler CancelCalls;
    event Action<Client, string> SignalingDataReceived;
    public Client Self { get; set; }
    bool IsConnected { get; }
    Task StartConnectionAsync(string url, string clientName);
    Task StopConnectionAsync();
    Task RequestCall(string targetClientId);
    Task AcceptCall(string callerId);
    Task DeclineCall(string callerId);
    Task EndCall(string peerId);
    Task CancelCall();
    Task SendSignalingData(string targetClientId, string signalingData);
}
