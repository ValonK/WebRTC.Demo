namespace WebRTC.Backend.Models;

public class Client(string id)
{
    public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));
    public string Name { get; set; }
}