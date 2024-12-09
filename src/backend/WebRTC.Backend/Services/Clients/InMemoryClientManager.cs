using System.Collections.Concurrent;
using WebRTC.Backend.Models;

namespace WebRTC.Backend.Services;

public class InMemoryClientManager : IClientManager
{
    private readonly ConcurrentDictionary<string, Client> _clients = new();

    public void AddClient(Client client) => _clients[client.Id] = client;

    public bool RemoveClient(string connectionId, out Client removedClient) => 
        _clients.TryRemove(connectionId, out removedClient);

    public bool TryGetClient(string connectionId, out Client client) => 
        _clients.TryGetValue(connectionId, out client);

    public IEnumerable<Client> GetAll() => _clients.Values;

    public void UpdateClient(Client client) => _clients[client.Id] = client;
}