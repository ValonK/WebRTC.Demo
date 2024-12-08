using Microsoft.AspNetCore.SignalR.Client;
using WebRTC.iOS.Models;

namespace WebRTC.iOS.Services.SignalR;

public class SignalRService : ISignalRService
{
    private HubConnection _hubConnection;
    private Client _client;

    public event EventHandler Closed;
    public event EventHandler<Client> ClientConnected;
    public event EventHandler<Client> ClientDisconnected;
    public event EventHandler<List<Client>> ConnectedClientsUpdated;
    public event Action<string> ConnectionRequestReceived;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StartConnectionAsync(string url)
    {
        if (_hubConnection != null && IsConnected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();
        
        _hubConnection.Closed += _ =>
        {
            Closed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };
        
        _hubConnection.On<Client>("ClientConnected", client =>
        {
        });

        _hubConnection.On<Client>("ClientDisconnected", client =>
        {
            ClientDisconnected?.Invoke(this, client);
        });

        _hubConnection.On<IEnumerable<Client>>("ConnectedClients", clients =>
        {
            ConnectedClientsUpdated?.Invoke(this, clients.Where(x => !x.Equals(_client)).ToList());
        });

        try
        {
            await _hubConnection.StartAsync();
            
            Console.WriteLine("SignalR connection started.");

            _client = new Client
            {
                Name = $"{UIDevice.CurrentDevice.Name} ({UIDevice.CurrentDevice.SystemVersion})"
            };
            await Login(_client);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting SignalR connection: {ex.Message}");
        }
    }

    public async Task StopConnectionAsync()
    {
        if (_hubConnection != null && IsConnected)
        {
            try
            {
                await _hubConnection.StopAsync();
                Console.WriteLine("SignalR connection stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping SignalR connection: {ex.Message}");
            }
        }
    }

    public async Task Login(Client client)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            _client = await _hubConnection.InvokeAsync<Client>("login", client);
        }
    }

    public async Task RequestConnection(string targetConnectionId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StartWebRtcConnection", targetConnectionId);
        }
    }
}