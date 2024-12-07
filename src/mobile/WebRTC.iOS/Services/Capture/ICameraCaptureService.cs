namespace WebRTC.iOS.Services.Capture;

internal interface ICameraCaptureService
{
    UIView PreviewView { get; }

    void StartCapture();
    void StopCapture();
    void ToggleLight();
    void SwitchCamera();
    void AttachToView(UIView view);
    bool IsTorchAvailable { get; }
    bool IsRunning { get; }
}