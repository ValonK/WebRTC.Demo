using WebRTC.Backend.Models;

namespace WebRTC.Backend.Services;

public interface IClientManager
{
    void AddClient(Client client);
    bool RemoveClient(string connectionId, out Client removedClient);
    bool TryGetClient(string connectionId, out Client client);
    IEnumerable<Client> GetAll();
    void UpdateClient(Client client);
}