namespace RTCDemo.iOS.Controls;

public sealed class EmptyView : UIView
{
    public EmptyView(string imageName, string message)
    {
        TranslatesAutoresizingMaskIntoConstraints = false;
        CreateEmptyView(imageName, message);
    }

    private void CreateEmptyView(string imageName, string message)
    {
        var imageView = new UIImageView
        {
            Image = UIImage.FromBundle(imageName),
            ContentMode = UIViewContentMode.ScaleAspectFit,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        var messageLabel = new UILabel
        {
            Text = message,
            TextAlignment = UITextAlignment.Center,
            Font = UIFont.SystemFontOfSize(16),
            TextColor = UIColor.Gray,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        AddSubview(imageView);
        AddSubview(messageLabel);

        NSLayoutConstraint.ActivateConstraints([
            imageView.CenterXAnchor.ConstraintEqualTo(CenterXAnchor),
            imageView.CenterYAnchor.ConstraintEqualTo(CenterYAnchor, -20),
            imageView.WidthAnchor.ConstraintEqualTo(100),
            imageView.HeightAnchor.ConstraintEqualTo(100),
            messageLabel.TopAnchor.ConstraintEqualTo(imageView.BottomAnchor, 10),
            messageLabel.CenterXAnchor.ConstraintEqualTo(CenterXAnchor)
        ]);
    }

    public void SetVisibility(bool isVisible)
    {
        Hidden = !isVisible;
    }
}