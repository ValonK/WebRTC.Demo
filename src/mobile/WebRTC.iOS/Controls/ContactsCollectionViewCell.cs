using WebRTC.iOS.Models;

namespace WebRTC.iOS.Controls;

public sealed class ContactsCollectionViewCell: UICollectionViewCell
{
    private readonly UILabel _nameLabel;
    private readonly UILabel _connectionIdLabel;

    [Export("initWithFrame:")]
    public ContactsCollectionViewCell(CGRect frame) : base(frame)
    {
        ContentView.Layer.CornerRadius = 8;
        ContentView.Layer.BorderColor = UIColor.LightGray.CGColor;
        ContentView.Layer.BorderWidth = 1;
        ContentView.BackgroundColor = UIColor.White;

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

        ContentView.AddSubviews(_nameLabel, _connectionIdLabel);

        NSLayoutConstraint.ActivateConstraints([
            _nameLabel.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, 10),
            _nameLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 10),
            _nameLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -10),

            _connectionIdLabel.TopAnchor.ConstraintEqualTo(_nameLabel.BottomAnchor, 5),
            _connectionIdLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 10),
            _connectionIdLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -10),
            _connectionIdLabel.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -10)
        ]);
    }

    public void Configure(Client client)
    {
        _nameLabel.Text = client.Name;
        _connectionIdLabel.Text = client.Id;
    }
}