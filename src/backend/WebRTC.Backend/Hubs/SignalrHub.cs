using Microsoft.AspNetCore.SignalR;
using WebRTC.Backend.Models;
using WebRTC.Backend.Services;
using WebRTC.Backend.Services.Call;

namespace WebRTC.Backend.Hubs;

public class SignalrHub(
    IClientManager clientManager,
    ICallManager callManager,
    ILogger<SignalrHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var newClient = new Client(connectionId);
        clientManager.AddClient(newClient);

        var allWithNames = clientManager.GetAll().Where(x => !string.IsNullOrEmpty(x.Name)).ToList();
        var others = allWithNames.Where(x => x.Id != connectionId).ToList();
        await Clients.Caller.SendAsync("ConnectedClients", others);

        logger.LogInformation($"Client connected: {connectionId}");
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var connectionId = Context.ConnectionId;

        if (callManager.EndCall(connectionId, out var call))
        {
            var otherPartyId = call.CallerId == connectionId ? call.CalleeId : call.CallerId;
            await Clients.Client(otherPartyId).SendAsync("CallEnded", new { InitiatorId = connectionId });
        }

        if (clientManager.RemoveClient(connectionId, out var removedClient))
        {
            await Clients.All.SendAsync("ClientDisconnected", removedClient);
            await BroadcastConnectedClients();
            logger.LogInformation($"Client disconnected: {removedClient.Name} ({connectionId})");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Client> Login(string name)
    {
        var connectionId = Context.ConnectionId;
        if (clientManager.TryGetClient(connectionId, out var client))
        {
            client.Name = name;
            clientManager.UpdateClient(client);

            await BroadcastConnectedClients();
            logger.LogInformation($"Client logged in: {client.Name} ({client.Id})");
            return client;
        }

        return null;
    }

    public async Task RequestCall(string targetClientId)
    {
        var callerId = Context.ConnectionId;

        if (!clientManager.TryGetClient(callerId, out var caller) || string.IsNullOrEmpty(caller.Name))
        {
            logger.LogInformation($"Caller not found or not logged in: {callerId}");
            return;
        }

        if (!clientManager.TryGetClient(targetClientId, out var callee) || string.IsNullOrEmpty(callee.Name))
        {
            logger.LogInformation($"Callee not found or not logged in: {targetClientId}");
            return;
        }

        if (callManager.StartCall(callerId, targetClientId))
        {
            await Clients.Client(targetClientId).SendAsync("IncomingCall", caller);
            logger.LogInformation(
                $"Call requested from {caller.Name} ({caller.Id}) to {callee.Name} ({callee.Id})");
        }
        else
        {
            logger.LogInformation($"Caller {callerId} is already in a call.");
        }
    }

    public async Task AcceptCall(string callerId)
    {
        var calleeId = Context.ConnectionId;
        if (callManager.AcceptCall(callerId, out var callInfo))
        {
            if (clientManager.TryGetClient(callInfo.CallerId, out var caller) &&
                clientManager.TryGetClient(callInfo.CalleeId, out var callee))
            {
                await Clients.Client(caller.Id).SendAsync("CallAccepted", callee);
                await Clients.Client(callee.Id).SendAsync("CallStarted", caller);

                logger.LogInformation(
                    $"Call accepted: {caller.Name} ({caller.Id}) <-> {callee.Name} ({callee.Id})");
            }
        }
        else
        {
            logger.LogInformation("Failed to accept call. No such ringing call found.");
        }
    }

    public async Task DeclineCall(string callerId)
    {
        if (callManager.DeclineCall(callerId, out var callInfo))
        {
            if (clientManager.TryGetClient(callInfo.CallerId, out var caller) &&
                clientManager.TryGetClient(callInfo.CalleeId, out var callee))
            {
                await Clients.Client(caller.Id).SendAsync("CallDeclined", callee);
                logger.LogInformation(
                    $"Call declined by {callee.Name} ({callee.Id}) for {caller.Name} ({caller.Id})");
            }
        }
        else
        {
            logger.LogInformation("No call to decline.");
        }
    }

    public async Task EndCall(string peerId)
    {
        var initiatorId = Context.ConnectionId;
        if (callManager.EndCall(initiatorId, out var callInfo))
        {
            var otherPartyId = callInfo.CallerId == initiatorId ? callInfo.CalleeId : callInfo.CallerId;

            await Clients.Client(otherPartyId).SendAsync("CallEnded", new { InitiatorId = initiatorId });
            await Clients.Client(initiatorId).SendAsync("CallEnded", new { InitiatorId = initiatorId });

            logger.LogInformation(
                $"Call ended by {initiatorId}. Caller: {callInfo.CallerId}, Callee: {callInfo.CalleeId}");
        }
        else
        {
            logger.LogInformation("No active call found to end.");
        }
    }

    public async Task SendSignalingData(Client client, SignalingMessage signalingData)
    {
        var senderId = Context.ConnectionId;
        if (clientManager.TryGetClient(senderId, out var sender) &&
            clientManager.TryGetClient(client.Id, out var targetClient))
        {
            var call = callManager.GetCallByParty(senderId);
            if (call is { State: CallState.Active } &&
                (call.CallerId == senderId || call.CalleeId == senderId) &&
                (call.CallerId == client.Id || call.CalleeId == client.Id))
            {
                await Clients.Client(client.Id).SendAsync("ReceiveSignalingData", sender, signalingData);
                logger.LogInformation(
                    $"Signaling data sent from {sender.Name} ({sender.Id}) to {targetClient.Name} ({targetClient.Id})");
            }
        }
    }

    private async Task BroadcastConnectedClients()
    {
        var allClients = clientManager.GetAll().Where(x => !string.IsNullOrEmpty(x.Name)).ToList();
        var tasks = new List<Task>();
        foreach (var c in allClients)
        {
            var others = allClients.Where(x => x.Id != c.Id).ToList();
            tasks.Add(Clients.Client(c.Id).SendAsync("ConnectedClients", others));
        }

        await Task.WhenAll(tasks);
    }
    
    public async Task CancelCalls()
    {
        callManager.Clear();
        await Clients.All.SendAsync("CancelAllCalls");
    }
}