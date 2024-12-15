
namespace RTCDemo.iOS.Models; 

[Preserve(AllMembers = true)]
public class Client 
{
    public string Id { get; set; }

    public string Name { get; set; }

    public bool Equals(Client other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }
}