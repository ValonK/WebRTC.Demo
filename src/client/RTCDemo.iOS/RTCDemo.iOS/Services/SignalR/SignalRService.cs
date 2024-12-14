using Microsoft.AspNetCore.SignalR.Client;
using RTCDemo.iOS.Models;

namespace RTCDemo.iOS.Services.SignalR;

public class SignalRService : ISignalRService
{
    private HubConnection _hubConnection;

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
    
    public Client Self { get; set; }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StartConnectionAsync(string url, string clientName)
    {
        if (_hubConnection != null && IsConnected) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Closed += _ =>
        {
            Closed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };

        _hubConnection.On<Client>("ClientDisconnected", client => ClientDisconnected?.Invoke(this, client));
        _hubConnection.On<IEnumerable<Client>>("ConnectedClients", clients =>
            ConnectedClientsUpdated?.Invoke(this, clients.Where(x => x.Id != Self?.Id).ToList()));
        _hubConnection.On<Client>("IncomingCall", caller => IncomingCallReceived?.Invoke(this, caller));
        _hubConnection.On<Client>("CallDeclined", callee =>
        {
            CallDeclined?.Invoke(this, callee);
        });
        
        _hubConnection.On<Client>("CallAccepted", callee => CallAccepted?.Invoke(this, callee));
        _hubConnection.On<Client>("CallStarted", caller => CallStarted?.Invoke(this, EventArgs.Empty));
        _hubConnection.On<object>("CallEnded", _ => CallEnded?.Invoke(this, EventArgs.Empty));
        _hubConnection.On("CancelAllCalls", () => CancelCalls?.Invoke(this, EventArgs.Empty));
        _hubConnection.On<Client, SignalingMessage>("ReceiveSignalingData", (client, message) => SignalingDataReceived?.Invoke(this, (client, message)));
        
        await _hubConnection.StartAsync();
        Console.WriteLine("SignalR connection started.");

        await Login(clientName);
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
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            Self = await _hubConnection.InvokeAsync<Client>("Login", name);
        }
    }

    public async Task RequestCall(string targetClientId)
    {
        if (IsConnected && !string.IsNullOrEmpty(targetClientId))
        {
            await _hubConnection.InvokeAsync("RequestCall", targetClientId);
        }
    }

    public async Task AcceptCall(string callerId)
    {
        if (IsConnected && !string.IsNullOrEmpty(callerId))
        {
            await _hubConnection.InvokeAsync("AcceptCall", callerId);
        }
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
        {
            await _hubConnection.InvokeAsync("EndCall", peerId);
        }
    }
    
    public async Task CancelCall()
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("CancelCalls");
        }
    }

    public async Task SendSignalingData(Client targetClient, SignalingMessage signalingMessage)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("SendSignalingData", targetClient, signalingMessage);
        }
    }
}