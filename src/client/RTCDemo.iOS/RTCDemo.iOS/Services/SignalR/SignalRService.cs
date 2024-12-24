using Microsoft.AspNetCore.SignalR.Client;
using RTCDemo.iOS.Models;

namespace RTCDemo.iOS.Services.SignalR;

public class SignalRService : ISignalRService
{
    private HubConnection _hubConnection;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    public event EventHandler Closed;
    public event EventHandler<Client> ClientDisconnected;
    public event EventHandler<List<Client>> ConnectedClientsUpdated;
    public event EventHandler<Client> IncomingCallReceived;
    public event EventHandler<Client> CallDeclined;
    public event EventHandler<Client> CallAccepted;
    public event EventHandler CallStarted;
    public event EventHandler CallEnded;
    public event EventHandler CancelCalls;
    public event EventHandler<(Client, SignalingMessage)> SignalingDataReceived;
    public event EventHandler<ConnectionState> ConnectionStatusChanged;

    public Client Self { get; set; }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task InitializeAsync()
    {
        if (_hubConnection != null && IsConnected) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://192.168.0.190:5136/signalhub")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Closed += async _ =>
        {
            ConnectionStatusChanged?.Invoke(this, ConnectionState.Disconnected);
            Closed?.Invoke(this, EventArgs.Empty);
            await RetryConnectAsync();
        };

        _hubConnection.Reconnecting += _ =>
        {
            ConnectionStatusChanged?.Invoke(this, ConnectionState.Connecting);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += _ =>
        {
            ConnectionStatusChanged?.Invoke(this, ConnectionState.Connected);
            return Task.CompletedTask;
        };

        RegisterHubEvents();

        await RetryConnectAsync();
    }

    private async Task RetryConnectAsync()
    {
        while (_hubConnection.State != HubConnectionState.Connected)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, ConnectionState.Connecting);
                await _hubConnection.StartAsync();
                ConnectionStatusChanged?.Invoke(this, ConnectionState.Connected);
                Console.WriteLine("SignalR connection established.");
                await Login($"{UIDevice.CurrentDevice.Name} ({UIDevice.CurrentDevice.SystemVersion})");
                return;
            }
            catch
            {
                ConnectionStatusChanged?.Invoke(this, ConnectionState.Failed);
                Console.WriteLine("Failed to connect to SignalR server. Retrying...");
                await Task.Delay(_retryDelay);
            }
        }
    }

    private void RegisterHubEvents()
    {
        _hubConnection.On<Client>("ClientDisconnected", client => ClientDisconnected?.Invoke(this, client));
        
        _hubConnection.On<IEnumerable<Client>>("ConnectedClients", clients =>
            ConnectedClientsUpdated?.Invoke(this, clients.Where(x => x.Id != Self?.Id).ToList()));
        
        _hubConnection.On<Client>("IncomingCall", caller => IncomingCallReceived?.Invoke(this, caller));
        
        _hubConnection.On<Client>("CallDeclined", callee => CallDeclined?.Invoke(this, callee));
        
        _hubConnection.On<Client>("CallAccepted", callee => CallAccepted?.Invoke(this, callee));
        
        _hubConnection.On<Client>("CallStarted", _ => CallStarted?.Invoke(this, EventArgs.Empty));
        
        _hubConnection.On<object>("CallEnded", _ => CallEnded?.Invoke(this, EventArgs.Empty));
        
        _hubConnection.On("CancelAllCalls", () => CancelCalls?.Invoke(this, EventArgs.Empty));
        
        _hubConnection.On<Client, SignalingMessage>("ReceiveSignalingData", (client, message) => 
            SignalingDataReceived?.Invoke(this, (client, message)));
    }

    public async Task StopConnectionAsync()
    {
        if (IsConnected)
        {
            await _hubConnection.StopAsync();
            _hubConnection = null;
        }
    }

    private async Task Login(string name)
    {
        if (IsConnected)
            Self = await _hubConnection.InvokeAsync<Client>("Login", name);
    }

    public async Task RequestCall(string targetClientId)
    {
        if (IsConnected && !string.IsNullOrEmpty(targetClientId))
            await _hubConnection.InvokeAsync("RequestCall", targetClientId);
    }

    public async Task AcceptCall(string callerId)
    {
        if (IsConnected && !string.IsNullOrEmpty(callerId)) 
            await _hubConnection.InvokeAsync("AcceptCall", callerId);
    }

    public async Task DeclineCall(string callerId)
    {
        if (IsConnected && !string.IsNullOrEmpty(callerId))
        {
            await _hubConnection.InvokeAsync("DeclineCall", callerId);
        }
    }

    public async Task EndCall(string peerId)
    {
        if (IsConnected && !string.IsNullOrEmpty(peerId)) 
            await _hubConnection.InvokeAsync("EndCall", peerId);
    }

    public async Task CancelCall()
    {
        if (IsConnected) await _hubConnection.InvokeAsync("CancelCalls");
    }

    public async Task SendSignalingData(Client targetClient, SignalingMessage signalingMessage)
    {
        if (IsConnected) 
            await _hubConnection.InvokeAsync("SendSignalingData", targetClient, signalingMessage);
    }
}