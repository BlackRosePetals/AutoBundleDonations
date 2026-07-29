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
}
