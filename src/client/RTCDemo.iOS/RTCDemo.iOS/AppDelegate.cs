using RTCDemo.iOS.Services.Audio;
using RTCDemo.iOS.Services.Logging;
using RTCDemo.iOS.Services.RTC;
using RTCDemo.iOS.Services.SignalR;
using RTCDemo.iOS.ViewControllers;

namespace RTCDemo.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    private UIWindow _window;
    private static readonly Lazy<ISignalRService> _signalRService = new(() => new SignalRService());
    private static readonly Lazy<ILoggingService> _logger = new(() => new LoggingService());
    private static readonly Lazy<IRtcService> _rtcService = new(() => new RtcService());
    private static readonly Lazy<AudioPlayerService> _audioService = new(() => new AudioPlayerService());
    
    public static ISignalRService SignalrService => _signalRService.Value;
    public static ILoggingService Logger => _logger.Value;
    public static IRtcService RTCService => _rtcService.Value;
    public static AudioPlayerService AudioService => _audioService.Value;
    
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var mainViewController = new MainViewController();
        var navigationController = new UINavigationController(mainViewController)
        {
            NavigationBarHidden = true 
        };
        _window = new UIWindow(UIScreen.MainScreen.Bounds)
        {
            RootViewController = navigationController
        };
        
        _window.MakeKeyAndVisible();
        return true;
    }
}