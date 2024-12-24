namespace RTCDemo.iOS.ViewControllers;

public class BaseViewController : UIViewController
{
    public override void ViewWillAppear(bool animated)
    {
        base.ViewWillAppear(animated);
        UIDevice.CurrentDevice.SetValueForKey(
            new NSNumber((int)UIInterfaceOrientation.Portrait), new NSString("orientation"));
    }
}