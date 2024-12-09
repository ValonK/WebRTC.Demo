namespace WebRTC.Backend.Services.Call;

public interface ICallManager
{
    bool StartCall(string callerId, string calleeId);
    bool AcceptCall(string callerId, out CallInfo callInfo);
    bool DeclineCall(string callerId, out CallInfo callInfo);
    bool EndCall(string initiatorId, out CallInfo callInfo);
    CallInfo GetCallByParty(string connectionId);
    CallInfo GetCallByCaller(string callerId);
}