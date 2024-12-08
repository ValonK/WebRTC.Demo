using WebRTC.iOS.Models;

namespace WebRTC.iOS.Controls;

public sealed class ContactsCollectionViewCell : UICollectionViewCell
{
    private readonly UIImageView _profileImageView;
    private readonly UILabel _nameLabel;
    private readonly UILabel _connectionIdLabel;

    [Export("initWithFrame:")]
    public ContactsCollectionViewCell(CGRect frame) : base(frame)
    {
        ContentView.Layer.CornerRadius = 8;
        ContentView.Layer.BorderColor = UIColor.LightGray.CGColor;
        ContentView.Layer.BorderWidth = 1;
        ContentView.BackgroundColor = UIColor.White;

        _profileImageView = new UIImageView
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            ContentMode = UIViewContentMode.ScaleAspectFit,
            Layer = { CornerRadius = 30, MasksToBounds = true },
        };

        _nameLabel = new UILabel
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            Font = UIFont.BoldSystemFontOfSize(16),
            TextColor = UIColor.Black
        };

        _connectionIdLabel = new UILabel
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            Font = UIFont.SystemFontOfSize(14),
            TextColor = UIColor.Gray
        };

        ContentView.AddSubviews(_profileImageView, _nameLabel, _connectionIdLabel);

        NSLayoutConstraint.ActivateConstraints([
            _profileImageView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 10),
            _profileImageView.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),
            _profileImageView.WidthAnchor.ConstraintEqualTo(60), 
            _profileImageView.HeightAnchor.ConstraintEqualTo(60),

            _nameLabel.TopAnchor.ConstraintEqualTo(ContentView.CenterYAnchor, -20),
            _nameLabel.LeadingAnchor.ConstraintEqualTo(_profileImageView.TrailingAnchor, 10),
            _nameLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -10),

            _connectionIdLabel.TopAnchor.ConstraintEqualTo(_nameLabel.BottomAnchor, 5),
            _connectionIdLabel.LeadingAnchor.ConstraintEqualTo(_profileImageView.TrailingAnchor, 10),
            _connectionIdLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -10)
        ]);
    }

    public void Configure(Client client)
    {
        _nameLabel.Text = client.Name;
        _connectionIdLabel.Text = client.Id;
        _profileImageView.Image = UIImage.FromBundle("ic_user.png"); 
    }
}
