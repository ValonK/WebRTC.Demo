namespace WebRTC.Backend.Models;

public class SignalingMessage
{
    public string Type { get; set; }
    public string Sdp { get; set; }
    public Candidate Candidate { get; set; }
}