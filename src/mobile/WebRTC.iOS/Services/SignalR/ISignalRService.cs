using WebRTC.iOS.Models;

namespace WebRTC.iOS.Services.SignalR;

public interface ISignalRService
{
    // Events
    event EventHandler Closed;
    event EventHandler<Client> ClientConnected;
    event EventHandler<Client> ClientDisconnected;
    event EventHandler<List<Client>> ConnectedClientsUpdated;
    event EventHandler<Client> IncomingCallReceived;
    event EventHandler<Client> CallStopped;
    event EventHandler<Client> CallAnswered;
    event Action<Client, string> SignalingDataReceived;

    // Connection Management
    Task StartConnectionAsync(string url);
    Task StopConnectionAsync();

    // Client Management
    Task Login(Client client);

    // Call Management
    Task RequestCall(Client targetClient);
    Task StopCall(Client targetClient);
    Task AnswerCall(Client caller);

    // WebRTC Signaling
    Task SendSignalingData(Client targetClient, string signalingData);

    // Connection State
    bool IsConnected { get; }
}
