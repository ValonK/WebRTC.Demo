using System.Collections.Concurrent;

namespace WebRTC.Backend.Services.Call;

public class InMemoryCallManager : ICallManager
{
    private readonly ConcurrentDictionary<string, CallInfo> _callsByCaller = new();

    public bool StartCall(string callerId, string calleeId)
    {
        if (_callsByCaller.ContainsKey(callerId))
            return false;

        var callInfo = new CallInfo { CallerId = callerId, CalleeId = calleeId, State = CallState.Ringing };
        return _callsByCaller.TryAdd(callerId, callInfo);
    }

    public bool AcceptCall(string callerId, out CallInfo callInfo)
    {
        if (!_callsByCaller.TryGetValue(callerId, out callInfo)) return false;
        callInfo.State = CallState.Active;
        return true;
    }

    public bool DeclineCall(string callerId, out CallInfo callInfo) => _callsByCaller.TryRemove(callerId, out callInfo);

    public bool EndCall(string connectionId, out CallInfo callInfo)
    {
        callInfo = _callsByCaller.Values.FirstOrDefault(c => c.CallerId == connectionId || c.CalleeId == connectionId);
        if (callInfo == null) return false;
        _callsByCaller.TryRemove(callInfo.CallerId, out _);
        return true;
    }
    
    public CallInfo GetCallByParty(string connectionId) => 
        _callsByCaller.Values.FirstOrDefault(c => c.CallerId == connectionId || c.CalleeId == connectionId);

    public CallInfo GetCallByCaller(string callerId)
    {
        _callsByCaller.TryGetValue(callerId, out var callInfo);
        return callInfo;
    }

    public void Clear() => _callsByCaller?.Clear();
}