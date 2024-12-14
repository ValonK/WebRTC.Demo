using RTCDemo.iOS.Services.Logging;
using RTCDemo.iOS.Services.SignalR;
using RTCDemo.iOS.ViewControllers;

namespace RTCDemo.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    private UIWindow _window;
    private static readonly Lazy<ISignalRService> _signalRService = new(() => new SignalRService());
    public static ISignalRService SignalrService => _signalRService.Value;

    private static readonly Lazy<ILoggingService> _logger = new(() => new LoggingService());
    public static ILoggingService Logger => _logger.Value;
    
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        
        var mainViewController = new MainViewController();
        _window = new UIWindow(UIScreen.MainScreen.Bounds)
        {
            RootViewController = mainViewController
        };
        
        _window.MakeKeyAndVisible();
        return true;
    }

    private UIViewController CreateMainViewController()
    {
        var controller = new UIViewController
        {
            View = { BackgroundColor = UIColor.White }
        };

        // _captureService.AttachToView(controller.View);
        //
        // var startButton = CreateButton("Start", 50, () => _captureService.StartCapture());
        // var stopButton = CreateButton("Stop", 120, () => _captureService.StopCapture());
        // var lightButton = CreateButton("Light", 190, () => _captureService.ToggleLight());
        // var switchButton = CreateButton("Switch", 260, () => _captureService.SwitchCamera());
        //
        // controller.View.AddSubviews(startButton, stopButton, lightButton, switchButton);

        return controller;
    }

    private UIButton CreateButton(string title, nfloat y, Action onClick)
    {
        var button = new UIButton(UIButtonType.System)
        {
            Frame = new CGRect(20, y, 100, 50),
            BackgroundColor = UIColor.LightGray,
            TintColor = UIColor.Black
        };

        button.SetTitle(title, UIControlState.Normal);
        button.Layer.CornerRadius = 10;
        button.TouchUpInside += (_, e) => onClick();
        return button;
    }
}