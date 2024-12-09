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
    public event EventHandler<Client> IncomingCallReceived;
    public event EventHandler<Client> IncomingCallDeclined;
    public event EventHandler<Client> CallStopped;
    public event EventHandler<Client> CallAnswered;
    public event Action<Client, string> SignalingDataReceived;

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

        _hubConnection.On<Client>("ClientConnected", client => ClientConnected?.Invoke(this, client));
        _hubConnection.On<Client>("ClientDisconnected", client => ClientDisconnected?.Invoke(this, client));
        _hubConnection.On<IEnumerable<Client>>("ConnectedClients", clients => 
            ConnectedClientsUpdated?.Invoke(this, clients.Where(x => !x.Equals(_client)).ToList()));
        _hubConnection.On<Client>("IncomingCall", caller => IncomingCallReceived?.Invoke(this, caller));
        _hubConnection.On<Client>("DeclineCall", caller => IncomingCallDeclined?.Invoke(this, caller));
        _hubConnection.On<Client>("CallStopped", caller => CallStopped?.Invoke(this, caller));
        _hubConnection.On<Client>("CallAnswered", answerer => CallAnswered?.Invoke(this, answerer));
        _hubConnection.On<Client, string>("ReceiveSignalingData", (sender, signalingData) =>
            SignalingDataReceived?.Invoke(sender, signalingData));

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

    public Task StopConnectionAsync()
    {
        return Task.CompletedTask;
    }

    public async Task Login(Client client)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            _client = await _hubConnection.InvokeAsync<Client>("Login", client);
        }
    }

    public async Task RequestCall(Client targetClient)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("RequestCall", targetClient);
        }
    }

    public async Task StopCall(Client targetClient)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StopCall", targetClient);
        }
    }
    
    public async Task DeclineCall(Client targetClient)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("DeclineCall", targetClient);
        }
    }

    public async Task AnswerCall(Client caller)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("AnswerCall", caller);
        }
    }

    public async Task SendSignalingData(Client targetClient, string signalingData)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendSignalingData", targetClient, signalingData);
        }
    }
}
