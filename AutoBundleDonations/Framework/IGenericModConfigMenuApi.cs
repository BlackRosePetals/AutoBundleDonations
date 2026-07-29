using System;
using StardewModdingAPI;

namespace AutoBundleDonations.Framework;

/// <summary>The minimal subset of Generic Mod Config Menu's API used by this mod (standard SMAPI interop stub).</summary>
public interface IGenericModConfigMenuApi
{
  void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

  void AddBoolOption(
    IManifest mod,
    Func<bool> getValue,
    Action<bool> setValue,
    Func<string> name,
    Func<string>? tooltip = null,
    string? fieldId = null
  );

  void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
}
