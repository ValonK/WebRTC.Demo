namespace WebRTC.Backend.Models;

public class Client(string id) : IEquatable<Client>
{
    public string Id { get; set; } = id;

    public string Name { get; set; } 


    public bool Equals(Client other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Name == other.Name;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Client)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}