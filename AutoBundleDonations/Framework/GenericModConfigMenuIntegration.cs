using System;
using StardewModdingAPI;

namespace AutoBundleDonations.Framework;

internal static class GenericModConfigMenuIntegration
{
  private const string GmcmUniqueId = "spacechase0.GenericModConfigMenu";

  public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action save)
  {
    IGenericModConfigMenuApi? api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(GmcmUniqueId);
    if (api == null)
    {
      return;
    }

    api.Register(manifest, () => Reset(config), save);

    api.AddBoolOption(
      manifest,
      () => config.Enabled,
      v => config.Enabled = v,
      () => I18n.Config_Enabled_Name(),
      () => I18n.Config_Enabled_Tooltip()
    );
    api.AddBoolOption(
      manifest,
      () => config.ShowNotifications,
      v => config.ShowNotifications = v,
      () => I18n.Config_ShowNotifications_Name(),
      () => I18n.Config_ShowNotifications_Tooltip()
    );

    api.AddSectionTitle(manifest, () => I18n.Config_Rooms_Title());
    api.AddBoolOption(
      manifest,
      () => config.DonatePantry,
      v => config.DonatePantry = v,
      () => I18n.Config_DonatePantry_Name()
    );
    api.AddBoolOption(
      manifest,
      () => config.DonateCraftsRoom,
      v => config.DonateCraftsRoom = v,
      () => I18n.Config_DonateCraftsRoom_Name()
    );
    api.AddBoolOption(
      manifest,
      () => config.DonateFishTank,
      v => config.DonateFishTank = v,
      () => I18n.Config_DonateFishTank_Name()
    );
    api.AddBoolOption(
      manifest,
      () => config.DonateBoilerRoom,
      v => config.DonateBoilerRoom = v,
      () => I18n.Config_DonateBoilerRoom_Name()
    );
    api.AddBoolOption(
      manifest,
      () => config.DonateBulletinBoard,
      v => config.DonateBulletinBoard = v,
      () => I18n.Config_DonateBulletinBoard_Name()
    );
    api.AddBoolOption(
      manifest,
      () => config.DonateMissingBundle,
      v => config.DonateMissingBundle = v,
      () => I18n.Config_DonateMissingBundle_Name(),
      () => I18n.Config_DonateMissingBundle_Tooltip()
    );
    api.AddSectionTitle(manifest, () => I18n.Config_ValuableItems_Title());
    api.AddBoolOption(
      manifest,
      () => config.WithholdValuableItems,
      v => config.WithholdValuableItems = v,
      () => I18n.Config_WithholdValuableItems_Name(),
      () => I18n.Config_WithholdValuableItems_Tooltip()
    );
    api.AddSectionTitle(manifest, () => I18n.Config_OtherBundleMods_Title(), () => I18n.Config_OtherBundleMods_Tooltip());
    api.AddBoolOption(
      manifest,
      () => config.EnableUnlockableBundlesIntegration,
      v => config.EnableUnlockableBundlesIntegration = v,
      () => I18n.Config_EnableUnlockableBundles_Name(),
      () => I18n.Config_EnableUnlockableBundles_Tooltip()
    );
    api.AddSectionTitle(manifest, () => I18n.Config_ModCompatibility_Title());
    api.AddBoolOption(
      manifest,
      () => config.PrioritizeMuseum,
      v => config.PrioritizeMuseum = v,
      () => I18n.Config_PrioritizeMuseum_Name(),
      () => I18n.Config_PrioritizeMuseum_Tooltip()
    );
  }

  private static void Reset(ModConfig config)
  {
    var defaults = new ModConfig();
    config.Enabled = defaults.Enabled;
    config.ShowNotifications = defaults.ShowNotifications;
    config.DonatePantry = defaults.DonatePantry;
    config.DonateCraftsRoom = defaults.DonateCraftsRoom;
    config.DonateFishTank = defaults.DonateFishTank;
    config.DonateBoilerRoom = defaults.DonateBoilerRoom;
    config.DonateBulletinBoard = defaults.DonateBulletinBoard;
    config.DonateMissingBundle = defaults.DonateMissingBundle;
    config.EnableUnlockableBundlesIntegration = defaults.EnableUnlockableBundlesIntegration;
    config.PrioritizeMuseum = defaults.PrioritizeMuseum;
    config.WithholdValuableItems = defaults.WithholdValuableItems;
  }
}
