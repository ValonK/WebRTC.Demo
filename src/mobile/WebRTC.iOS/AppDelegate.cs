using WebRTC.iOS.Services.SignalR;
using WebRTC.iOS.ViewControllers;

namespace WebRTC.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    private UIWindow _window;

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var signalRService = new SignalRService();

        var mainController = new MainViewController(signalRService);
        _window = new UIWindow(UIScreen.MainScreen.Bounds)
        {
            RootViewController  = mainController
        };

        _window.MakeKeyAndVisible();
        return true;
    }
}





// public class AppDelegate : UIApplicationDelegate
// {
//     private UIWindow _window;
//     private ICameraCaptureService _cameraService;
//     private ILoggingService _logger;
//
//     public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
//     {
//         _logger = new LoggingService();
//         _cameraService = new CameraCaptureService(_logger);
//
//         _window = new UIWindow(UIScreen.MainScreen.Bounds)
//         {
//             RootViewController = CreateMainViewController()
//         };
//
//         _window.MakeKeyAndVisible();
//         return true;
//     }
//
//     private UIViewController CreateMainViewController()
//     {
//         var controller = new UIViewController
//         {
//             View = { BackgroundColor = UIColor.White }
//         };
//
//         _cameraService.AttachToView(controller.View);
//
//         var startButton = CreateButton("Start", 50, () => _cameraService.StartCapture());
//         var stopButton = CreateButton("Stop", 120, () => _cameraService.StopCapture());
//         var lightButton = CreateButton("Light", 190, () => _cameraService.ToggleLight());
//         var switchButton = CreateButton("Switch", 260, () => _cameraService.SwitchCamera());
//
//         controller.View.AddSubviews(startButton, stopButton, lightButton, switchButton);
//
//         return controller;
//     }
//
//     private UIButton CreateButton(string title, nfloat y, Action onClick)
//     {
//         var button = new UIButton(UIButtonType.System)
//         {
//             Frame = new CGRect(20, y, 100, 50),
//             BackgroundColor = UIColor.LightGray,
//             TintColor = UIColor.Black
//         };
//
//         button.SetTitle(title, UIControlState.Normal);
//         button.Layer.CornerRadius = 10;
//         button.TouchUpInside += (_, e) => onClick();
//         return button;
//     }
// }