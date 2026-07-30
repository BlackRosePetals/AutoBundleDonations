using AutoBundleDonations.Framework;
using AutoBundleDonations.Framework.UnlockableBundles;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutoBundleDonations;

public class ModEntry : Mod
{
  private ModConfig _config = null!;
  private BundleDelivery _delivery = null!;
  private UnlockableBundlesDelivery _unlockableBundlesDelivery = null!;

  public override void Entry(IModHelper helper)
  {
    I18n.Init(helper.Translation);
    _config = helper.ReadConfig<ModConfig>();
    var chat = new ChatNotifier();
    _delivery = new BundleDelivery(_config, chat, Monitor);
    _unlockableBundlesDelivery = new UnlockableBundlesDelivery(_config, chat, helper, Monitor);

    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.Player.InventoryChanged += OnInventoryChanged;
    helper.Events.GameLoop.TimeChanged += OnTimeChanged;
  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    GenericModConfigMenuIntegration.Register(Helper, ModManifest, _config, () => Helper.WriteConfig(_config));
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    TryRunDelivery();
  }

  private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
  {
    if (e.IsLocalPlayer)
    {
      TryRunDelivery();
    }
  }

  private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
  {
    TryRunDelivery();
  }

  private void TryRunDelivery()
  {
    if (!Context.IsWorldReady)
    {
      return;
    }

    _delivery.Run(Game1.player);
    _unlockableBundlesDelivery.Run(Game1.player);
  }
}
