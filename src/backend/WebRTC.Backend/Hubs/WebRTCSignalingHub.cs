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

        ConnectedClients[connectionId] = new Client(connectionId);
        Console.WriteLine($"Client connected: ({connectionId})");

        Clients.Caller.SendAsync("ConnectedClients", ConnectedClients.Values);

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

            await Clients.All.SendAsync("ClientConnected", client);
            await Clients.All.SendAsync("ConnectedClients", ConnectedClients.Values);

            Console.WriteLine($"Client logged in: {client.Name} ({client.Id})");

            return client;
        }

        return null;
    }
}
