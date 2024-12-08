using WebRTC.iOS.Models;

namespace WebRTC.iOS.Controls;

public class ContactsCollectionViewSource(List<Client> clients, Action<Client> onClientSelected)
    : UICollectionViewDataSource, IUICollectionViewDelegate
{
    public override nint GetItemsCount(UICollectionView collectionView, nint section) => clients.Count;

    public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
    {
        var cell = collectionView.DequeueReusableCell("ContactsViewCell", indexPath) as ContactsCollectionViewCell;
        var client = clients[indexPath.Row];
        cell?.Configure(client);
        return cell;
    }

    [Export("collectionView:didSelectItemAtIndexPath:")]
    public void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
    {
        var client = clients[indexPath.Row];
        onClientSelected?.Invoke(client);
    }
}