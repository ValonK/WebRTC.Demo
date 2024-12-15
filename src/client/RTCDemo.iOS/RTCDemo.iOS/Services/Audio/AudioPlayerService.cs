using AVFoundation;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.Services.Audio;

public class AudioPlayerService
{
    private AVAudioPlayer _audioPlayer;

    public void PlaySound(string sound, int numberOfLoops = -1)
    {
        try
        {
            var soundPath = NSBundle.MainBundle.PathForResource(sound, "mp3");
            if (string.IsNullOrEmpty(soundPath))
            {
                Logger.Log("Error: Sound file not found.");
                return;
            }

            var soundUrl = NSUrl.FromFilename(soundPath);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (soundUrl == null)
            {
                Logger.Log("Error: Unable to create URL for sound file.");
                return;
            }

            _audioPlayer = AVAudioPlayer.FromUrl(soundUrl);
            if (_audioPlayer != null)
            {
                _audioPlayer.NumberOfLoops = numberOfLoops;
                _audioPlayer.Play();
            }
            else
            {
                Logger.Log("Error: Failed to initialize the audio player.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }
    
    public void StopSound()
    {
        _audioPlayer?.Stop();
        _audioPlayer?.Dispose();
        _audioPlayer = null;
    }
}