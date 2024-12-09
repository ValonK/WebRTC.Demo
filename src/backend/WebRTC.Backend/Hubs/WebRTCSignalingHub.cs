using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using WebRTC.Backend.Models;

namespace WebRTC.Backend.Hubs;

public class WebRtcSignalingHub : Hub
{
    private static readonly ConcurrentDictionary<string, Client> ConnectedClients = new();

    public override Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        var newClient = new Client(connectionId);
        ConnectedClients[connectionId] = newClient;

        Clients.Caller.SendAsync("ConnectedClients", ConnectedClients.Values);

        Console.WriteLine($"Client connected: {connectionId}");
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        if (ConnectedClients.TryRemove(connectionId, out var client))
        {
            await Clients.All.SendAsync("ClientDisconnected", client);
            await Clients.All.SendAsync("ConnectedClients", ConnectedClients.Values);

            Console.WriteLine($"Client disconnected: {client.Name} ({connectionId})");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Client> Login(Client client)
    {
        var connectionId = Context.ConnectionId;

        if (ConnectedClients.ContainsKey(connectionId))
        {
            client.Id = connectionId;
            ConnectedClients[connectionId] = client;

            await Clients.All.SendAsync("ConnectedClients", ConnectedClients.Values.Where(x => !string.IsNullOrEmpty(x.Name)));

            Console.WriteLine($"Client logged in: {client.Name} ({client.Id})");

            return client;
        }

        return null;
    }

    public async Task RequestCall(Client targetClient)
    {
        var caller = ConnectedClients[Context.ConnectionId];

        if (ConnectedClients.Values.Contains(targetClient))
        {
            await Clients.Client(targetClient.Id).SendAsync("IncomingCall", caller);
            Console.WriteLine($"Call requested from {caller.Name} to {targetClient.Name}");
        }
    }

    public async Task StopCall(Client targetClient)
    {
        var caller = ConnectedClients[Context.ConnectionId];

        if (ConnectedClients.Values.Contains(targetClient))
        {
            await Clients.Client(targetClient.Id).SendAsync("CallStopped", caller);
            Console.WriteLine($"Call stopped by {caller.Id} for {targetClient.Id}");
        }
    }
    
    public async Task DeclineCall(Client targetClient)
    {
        var caller = ConnectedClients[Context.ConnectionId];

        if (ConnectedClients.Values.Contains(targetClient))
        {
            await Clients.Client(targetClient.Id).SendAsync("DeclineCall", caller);
            Console.WriteLine($"Call declined by {caller.Id} for {targetClient.Id}");
        }
    }

    public async Task AnswerCall(Client caller)
    {
        var answerer = ConnectedClients[Context.ConnectionId];

        if (ConnectedClients.Values.Contains(caller))
        {
            await Clients.Client(caller.Id).SendAsync("CallAnswered", answerer);
            Console.WriteLine($"Call answered by {answerer.Id} for {caller.Id}");
        }
    }

    public async Task SendSignalingData(Client targetClient, string signalingData)
    {
        var sender = ConnectedClients[Context.ConnectionId];

        if (ConnectedClients.Values.Contains(targetClient))
        {
            await Clients.Client(targetClient.Id).SendAsync("ReceiveSignalingData", sender, signalingData);
            Console.WriteLine($"Signaling data sent from {sender.Id} to {targetClient.Id}");
        }
    }
}
