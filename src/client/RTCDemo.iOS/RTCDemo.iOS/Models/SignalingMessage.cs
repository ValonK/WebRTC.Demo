
namespace RTCDemo.iOS.Models;

[Preserve(AllMembers = true)]
public class SignalingMessage
{
    public string Type { get; set; }
    public string Sdp { get; set; }
    public Candidate Candidate { get; set; }
}