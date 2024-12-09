namespace WebRTC.Backend.Services.Call;

public class CallInfo
{
    public string CallerId { get; set; }
    public string CalleeId { get; set; }
    public CallState State { get; set; }
}