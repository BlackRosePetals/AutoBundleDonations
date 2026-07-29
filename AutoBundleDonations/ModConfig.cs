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
}
