
namespace RTCDemo.iOS.Models;

[Preserve(AllMembers = true)]
public class Candidate
{
    public string Sdp { get; set; }
    public int SdpMLineIndex { get; set; }
    public string SdpMid { get; set; }
}