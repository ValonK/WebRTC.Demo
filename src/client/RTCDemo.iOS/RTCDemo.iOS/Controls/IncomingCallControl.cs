using RTCDemo.iOS.Models;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.Controls;

public sealed class IncomingCallControl : UIView
{
    public event EventHandler<Client> OnAccept;
    public event EventHandler OnDecline;

    private readonly UILabel _titleLabel;
    private UIView _backgroundView;
    private Client _client;

    public IncomingCallControl()
    {
        BackgroundColor = UIColor.FromRGB(36, 36, 36);
        TranslatesAutoresizingMaskIntoConstraints = false;

        Layer.CornerRadius = 10;
        Layer.BorderColor = UIColor.Green.CGColor;
        Layer.BorderWidth = 1;

        Layer.ShadowColor = UIColor.Black.CGColor;
        Layer.ShadowOpacity = 0.5f;
        Layer.ShadowRadius = 8;

        _titleLabel = new UILabel
        {
            TextAlignment = UITextAlignment.Left,
            Font = UIFont.BoldSystemFontOfSize(27),
            TextColor = UIColor.White,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        var acceptButton = CreateButton("Accept", UIColor.Green, HandleAccept);
        var declineButton = CreateButton("Decline", UIColor.Red, HandleDecline);

        var buttonStack = new UIStackView([acceptButton, declineButton])
        {
            Axis = UILayoutConstraintAxis.Horizontal,
            Distribution = UIStackViewDistribution.FillEqually,
            Spacing = 10,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        AddSubview(_titleLabel);
        AddSubview(buttonStack);

        NSLayoutConstraint.ActivateConstraints([
            _titleLabel.TopAnchor.ConstraintEqualTo(TopAnchor, 15),
            _titleLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, 15),
            _titleLabel.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -15),

            buttonStack.BottomAnchor.ConstraintEqualTo(BottomAnchor, -15),
            buttonStack.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, 15),
            buttonStack.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -15),
            buttonStack.HeightAnchor.ConstraintEqualTo(40)
        ]);
    }

    private static UIButton CreateButton(string title, UIColor backgroundColor, EventHandler touchUpInsideHandler)
    {
        var button = new UIButton(UIButtonType.System)
        {
            BackgroundColor = backgroundColor,
            Layer = { CornerRadius = 5 },
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        button.SetTitle(title, UIControlState.Normal);
        button.SetTitleColor(UIColor.White, UIControlState.Normal);
        button.TitleLabel.Font = UIFont.BoldSystemFontOfSize(UIFont.LabelFontSize);
        button.TouchUpInside += touchUpInsideHandler;

        return button;
    }

    private void HandleAccept(object sender, EventArgs e)
    {
        OnAccept?.Invoke(this, _client);
        Close();
    }

    private void HandleDecline(object sender, EventArgs e)
    {
        OnDecline?.Invoke(this, EventArgs.Empty);
        Close();
    }

    public void ShowInView(UIView parentView, Client client)
    {
        _client = client;
        _backgroundView = new UIView
        {
            BackgroundColor = UIColor.FromWhiteAlpha(0, 0.2f), 
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        parentView.AddSubview(_backgroundView);

        _backgroundView.AddSubview(this);

        NSLayoutConstraint.ActivateConstraints([
            _backgroundView.TopAnchor.ConstraintEqualTo(parentView.TopAnchor),
            _backgroundView.BottomAnchor.ConstraintEqualTo(parentView.BottomAnchor),
            _backgroundView.LeadingAnchor.ConstraintEqualTo(parentView.LeadingAnchor),
            _backgroundView.TrailingAnchor.ConstraintEqualTo(parentView.TrailingAnchor)
        ]);

        NSLayoutConstraint.ActivateConstraints([
            LeadingAnchor.ConstraintEqualTo(_backgroundView.LeadingAnchor, 20),
            TrailingAnchor.ConstraintEqualTo(_backgroundView.TrailingAnchor, -20),
            HeightAnchor.ConstraintEqualTo(150),
            BottomAnchor.ConstraintEqualTo(_backgroundView.BottomAnchor, -40)
        ]);

        _titleLabel.Text = client.Name;
    
        AudioService.PlaySound("ringtone");

        Transform = CGAffineTransform.MakeTranslation(0, 200);
        Animate(0.3, () =>
        {
            Transform = CGAffineTransform.MakeIdentity(); 
        });
    }
    
    public void Close()
    {
        AudioService.StopSound();
        
        Animate(0.3, () =>
        {
            Transform = CGAffineTransform.MakeTranslation(0, 200); 
        }, () =>
        {
            _backgroundView?.RemoveFromSuperview(); 
            _backgroundView = null;
            RemoveFromSuperview(); 
        });
    }
}
