namespace AutoBundleDonations;

public sealed class ModConfig
{
  public bool Enabled { get; set; } = true;

  public bool ShowNotifications { get; set; } = true;

  public bool DonatePantry { get; set; } = true;

  public bool DonateCraftsRoom { get; set; } = true;

  public bool DonateFishTank { get; set; } = true;

  public bool DonateBoilerRoom { get; set; } = true;

  public bool DonateBulletinBoard { get; set; } = true;

  public bool DonateMissingBundle { get; set; } = true;

  /// <summary>
  ///   Also auto-donate to bundles defined by the "Unlockable Bundles" framework mod (used by content packs like
  ///   Visit Mount Vapius or Joja Civic Center), if it's installed. This works by reflecting into that framework's
  ///   internal (undocumented) implementation, since it doesn't publish a way to contribute items itself - see
  ///   UnlockableBundlesDelivery for the safety guards around that. Disable this if you'd rather not take that risk
  ///   while still using vanilla Community Center auto-donation.
  /// </summary>
  public bool EnableUnlockableBundlesIntegration { get; set; } = true;

  /// <summary>
  ///   When the "Auto Museum Donations" mod is also installed, some items (artifacts and minerals) are eligible
  ///   for both it and a vanilla Community Center bundle slot. Since both mods react to the same InventoryChanged
  ///   event, whichever one runs first wins the item for that tick - by default that's this mod, since Community
  ///   Center donation is checked first. Enabling this defers those specific items so Auto Museum Donations gets
  ///   first claim on them; this mod will still donate them to the Community Center afterward if the museum
  ///   doesn't want them (already donated, or Auto Museum Donations has that category disabled). Has no effect
  ///   unless Auto Museum Donations is installed.
  /// </summary>
  public bool PrioritizeMuseum { get; set; } = false;

  /// <summary>
  ///   Keep a small curated set of iconic items (Prismatic Shard, Dinosaur Egg, and the 7 basic gems - Diamond,
  ///   Ruby, Emerald, Jade, Aquamarine, Topaz, Amethyst) in your inventory instead of auto-donating them to a
  ///   Community Center bundle slot that accepts them (namely the Dye Bundle's Aquamarine, and the Missing
  ///   Bundle's Prismatic Shard - see BundleDelivery.ValuableItemIds). Mirrors Auto Museum Donations' own
  ///   "donate valuable items" setting (also off by default) for the same items, so a single Aquamarine or
  ///   Prismatic Shard isn't silently spent the moment it's picked up, before you've decided whether you'd
  ///   rather keep it for a Crystalarium, gifting, or shipping.
  /// </summary>
  public bool WithholdValuableItems { get; set; } = false;
}
