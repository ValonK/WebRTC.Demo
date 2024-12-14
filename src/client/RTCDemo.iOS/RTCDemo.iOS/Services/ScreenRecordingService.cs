using ReplayKit;

namespace RTCDemo.iOS.Services;

public class ScreenRecordingService
{
    private readonly RPScreenRecorder _screenRecorder = RPScreenRecorder.SharedRecorder;
    private RPPreviewViewController _previewController;

    /// <summary>
    /// Starts the screen recording.
    /// </summary>
    public void StartRecording()
    {
        if (_screenRecorder.Available)
        {
            _screenRecorder.StartRecording((NSError error) =>
            {
                if (error != null)
                {
                    Console.WriteLine($"Error starting recording: {error.LocalizedDescription}");
                }
                else
                {
                    Console.WriteLine("Screen recording started successfully.");
                }
            });
        }
        else
        {
            Console.WriteLine("Screen recording is not available.");
        }
    }

    /// <summary>
    /// Stops the screen recording and presents a preview of the recording.
    /// </summary>
    public void StopRecording()
    {
        _screenRecorder.StopRecording((RPPreviewViewController previewController, NSError error) =>
        {
            if (error != null)
            {
                Console.WriteLine($"Error stopping recording: {error.LocalizedDescription}");
                return;
            }

            _previewController = previewController;

            if (previewController != null)
            {
                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(previewController, true, null);
            }
        });
    }

    /// <summary>
    /// Dismisses the recording preview.
    /// </summary>
    public void DiscardRecording()
    {
        _previewController?.DismissViewController(true, null);
        _previewController = null;
    }
}