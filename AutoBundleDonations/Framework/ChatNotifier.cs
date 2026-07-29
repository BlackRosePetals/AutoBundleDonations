using StardewValley;

namespace AutoBundleDonations.Framework;

internal sealed class ChatNotifier
{
  public void NotifyDonation(string itemName, int quantity, string bundleName)
  {
    Game1.chatBox?.addInfoMessage(I18n.Chat_Donation(quantity, itemName, bundleName));
  }
}
