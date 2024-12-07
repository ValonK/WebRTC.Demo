using System.Diagnostics.CodeAnalysis;
using AVFoundation;
using WebRTC.iOS.Services.Logging;
// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace WebRTC.iOS.Services.Capture;

[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
public class CameraCaptureService : ICameraCaptureService
{
    private readonly AVCaptureSession _captureSession;
    private AVCaptureDeviceInput _currentCameraInput;
    private AVCaptureVideoPreviewLayer _previewLayer;
    private readonly object _sessionLock = new();

    private readonly ILoggingService _logger;

    public UIView PreviewView { get; private set; }

    public bool IsTorchAvailable => _currentCameraInput?.Device is { HasTorch: true, TorchAvailable: true };

    public bool IsRunning => _captureSession.Running;

    public CameraCaptureService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _captureSession = new AVCaptureSession
        {
            SessionPreset = AVCaptureSession.PresetHigh
        };

        InitializeCamera();
    }

    private void InitializeCamera()
    {
        try
        {
            var camera = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
            if (camera == null)
            {
                _logger.Log("No camera available.");
                return;
            }

            _currentCameraInput = AVCaptureDeviceInput.FromDevice(camera, out var error);
            if (error != null)
            {
                _logger.Log($"Error initializing camera: {error.LocalizedDescription}");
                return;
            }

            lock (_sessionLock)
            {
                if (_captureSession.CanAddInput(_currentCameraInput))
                {
                    _captureSession.AddInput(_currentCameraInput);
                }
            }

            _previewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
            {
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };

            PreviewView = new UIView
            {
                BackgroundColor = UIColor.Black
            };
            PreviewView.Layer.AddSublayer(_previewLayer);
        }
        catch (Exception ex)
        {
            _logger.Log($"Exception during camera initialization: {ex.Message}");
        }
    }

    public void StartCapture()
    {
        lock (_sessionLock)
        {
            if (_captureSession.Running) return;
            
            _captureSession.StartRunning();
            _logger.Log("Camera capture started.");
        }
    }

    public void StopCapture()
    {
        lock (_sessionLock)
        {
            if (!_captureSession.Running) return;
            
            _captureSession.StopRunning();
            _logger.Log("Camera capture stopped.");
        }
    }

    public void ToggleLight()
    {
        try
        {
            var camera = _currentCameraInput?.Device;
            if (camera is not { HasTorch: true, TorchAvailable: true }) return;
            
            camera.LockForConfiguration(out var error);
            if (error == null)
            {
                camera.TorchMode = camera.TorchMode == AVCaptureTorchMode.On
                    ? AVCaptureTorchMode.Off
                    : AVCaptureTorchMode.On;
                camera.UnlockForConfiguration();
            }
            else
            {
                _logger.Log($"Error locking torch configuration: {error.LocalizedDescription}");
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Exception during torch toggle: {ex.Message}");
        }
    }

    public void SwitchCamera()
    {
        try
        {
            var currentPosition = _currentCameraInput?.Device.Position ?? AVCaptureDevicePosition.Unspecified;
            var newPosition = currentPosition == AVCaptureDevicePosition.Front
                ? AVCaptureDevicePosition.Back
                : AVCaptureDevicePosition.Front;

            var devices = AVCaptureDevice.Devices;
            var newCamera = devices.FirstOrDefault(device => device.Position == newPosition);

            if (newCamera == null)
            {
                _logger.Log($"No camera found for position {newPosition}");
                return;
            }

            var newInput = AVCaptureDeviceInput.FromDevice(newCamera, out var error);
            if (error != null)
            {
                _logger.Log($"Error creating camera input: {error.LocalizedDescription}");
                return;
            }

            lock (_sessionLock)
            {
                _captureSession.BeginConfiguration();
                _captureSession.RemoveInput(_currentCameraInput);
                if (_captureSession.CanAddInput(newInput))
                {
                    _captureSession.AddInput(newInput);
                    _currentCameraInput = newInput;
                }
                else
                {
                    _logger.Log("Cannot add new camera input.");
                }
                _captureSession.CommitConfiguration();
            }

            _logger.Log("Camera switched successfully.");
        }
        catch (Exception ex)
        {
            _logger.Log($"Exception during camera switch: {ex.Message}");
        }
    }

    public void AttachToView(UIView view)
    {
        if (view == null)
        {
            throw new ArgumentNullException(nameof(view), "The view cannot be null.");
        }

        _previewLayer.Frame = view.Bounds;
        view.Layer.AddSublayer(_previewLayer);
    }
}