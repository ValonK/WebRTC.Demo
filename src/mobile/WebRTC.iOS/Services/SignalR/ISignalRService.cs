using WebRTC.iOS.Models;

namespace WebRTC.iOS.Services.SignalR;

public interface ISignalRService
{
    event EventHandler Closed;
    event EventHandler<Client> ClientConnected;
    event EventHandler<Client> ClientDisconnected;
    event EventHandler<List<Client>> ConnectedClientsUpdated;
    
    Task StartConnectionAsync(string url);
    Task StopConnectionAsync();
    Task RequestConnection(string targetConnectionId);
}