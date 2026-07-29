using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;

namespace AutoBundleDonations.Framework.UnlockableBundles;

/// <summary>
///   Auto-donates items toward bundles defined by the "Unlockable Bundles" framework mod (DLX.Bundles), used by
///   content packs like Visit Mount Vapius and Joja Civic Center to add their own bundle systems outside the
///   vanilla Community Center. Generic by design: it only talks to the framework itself, never to any specific
///   content pack, so it works for anything built on Unlockable Bundles.
/// </summary>
/// <remarks>
///   The framework's <em>published</em> API (<see cref="IUnlockableBundlesApi" />) is read-only - it has no method
///   to actually credit a contribution. That logic lives in internal classes not meant for external consumption, so
///   this class reflects into them. To keep that safe:
///   <list type="bullet">
///     <item>Every reflected member is a `public` method on a `public` (or at least externally-reachable) class
///     that the framework's own donation code paths already exercise (BundlePageMenu.cs, SpeechBubble.cs) - not
///     obscure, never-called internals.</item>
///     <item>All required members are resolved once up front (<see cref="ProbeCapabilities" />). If even one is
///     missing - e.g. a future Unlockable Bundles update renamed something - the whole integration disables itself
///     for the session with a single warning, rather than guessing or partially working.</item>
///     <item>Per item, the contribution is credited (<c>ProcessContribution</c>) before the item is actually
///     removed from the player's inventory. If anything throws in between, the player keeps the item and the
///     bundle records an unearned credit - an unwanted but harmless outcome, chosen deliberately over the
///     alternative (item silently destroyed with nothing to show for it).</item>
///   </list>
/// </remarks>
internal sealed class UnlockableBundlesDelivery
{
  private const string ModId = "DLX.Bundles";

  private readonly ModConfig _config;
  private readonly ChatNotifier _chat;
  private readonly IModHelper _helper;
  private readonly IMonitor _monitor;

  private IUnlockableBundlesApi? _api;
  private bool _probed;
  private bool _available;

  private MethodInfo? _bundleDictionaryTryGet;
  private MethodInfo? _inventoryHasEnoughItems;
  private MethodInfo? _inventoryRemoveItemsOfRequirement;
  private MethodInfo? _unlockableProcessContribution;
  private MethodInfo? _unlockableAllRequirementsPaid;
  private MethodInfo? _unlockableProcessPurchase;

  public UnlockableBundlesDelivery(ModConfig config, ChatNotifier chat, IModHelper helper, IMonitor monitor)
  {
    _config = config;
    _chat = chat;
    _helper = helper;
    _monitor = monitor;
  }

  public void Run(Farmer player)
  {
    if (!_config.Enabled || !_config.EnableUnlockableBundlesIntegration || !Context.IsMainPlayer)
    {
      return;
    }

    if (!_probed)
    {
      ProbeCapabilities();
    }

    if (!_available || _api == null)
    {
      return;
    }

    IDictionary<string, IList<IBundle>>? bundlesByLocation;
    try
    {
      bundlesByLocation = _api.getBundles();
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: getBundles() failed, disabling for this session. {e}", LogLevel.Warn);
      _available = false;
      return;
    }

    if (bundlesByLocation == null)
    {
      return;
    }

    foreach (IBundle bundle in bundlesByLocation.Values.SelectMany(list => list))
    {
      if (!bundle.Discovered || bundle.Purchased)
      {
        continue;
      }

      TryDonateToBundle(player, bundle);
    }
  }

  private void ProbeCapabilities()
  {
    _probed = true;

    _api = _helper.ModRegistry.GetApi<IUnlockableBundlesApi>(ModId);
    if (_api == null)
    {
      // Unlockable Bundles isn't installed - nothing to do, not an error.
      return;
    }

    try
    {
      Assembly assembly = _api.GetType().Assembly;

      Type bundleDictionaryType = RequireType(assembly, "Unlockable_Bundles.Lib.BundleDictionary");
      Type inventoryType = RequireType(assembly, "Unlockable_Bundles.Lib.ShopTypes.Inventory");
      Type unlockableType = RequireType(assembly, "Unlockable_Bundles.Lib.Unlockable");

      _bundleDictionaryTryGet = RequireMethod(bundleDictionaryType, "TryGet", typeof(string), typeof(string));
      _inventoryHasEnoughItems = RequireMethod(inventoryType, "HasEnoughItems", typeof(Farmer), typeof(KeyValuePair<string, int>));
      _inventoryRemoveItemsOfRequirement =
        RequireMethod(inventoryType, "RemoveItemsOfRequirement", typeof(Farmer), typeof(KeyValuePair<string, int>));
      _unlockableProcessContribution = RequireMethod(
        unlockableType,
        "ProcessContribution",
        typeof(KeyValuePair<string, int>),
        typeof(int),
        typeof(HashSet<string>)
      );
      _unlockableAllRequirementsPaid = RequireMethod(unlockableType, "AllRequirementsPaid");
      _unlockableProcessPurchase = RequireMethod(unlockableType, "ProcessPurchase");

      _available = true;
      _monitor.Log("Unlockable Bundles detected - auto-donation enabled for its bundles too.", LogLevel.Info);
    }
    catch (Exception e)
    {
      _monitor.Log(
        "Unlockable Bundles was detected, but its internals don't match what this integration expects "
        + "(likely a version change on their end). Auto-donation for Unlockable-Bundles-based bundles is "
        + $"disabled for this session; vanilla Community Center donation is unaffected. Details: {e.Message}",
        LogLevel.Warn
      );
      _api = null;
      _available = false;
    }
  }

  private static Type RequireType(Assembly assembly, string fullName)
  {
    return assembly.GetType(fullName) ?? throw new TypeLoadException($"Type '{fullName}' not found in {assembly.GetName().Name}.");
  }

  private static MethodInfo RequireMethod(Type type, string name, params Type[] parameterTypes)
  {
    MethodInfo? method = type.GetMethod(
      name,
      BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
      null,
      parameterTypes,
      null
    );
    return method ?? throw new MissingMethodException(type.FullName, name);
  }

  private void TryDonateToBundle(Farmer player, IBundle bundle)
  {
    object? liveUnlockable;
    try
    {
      liveUnlockable = _bundleDictionaryTryGet!.Invoke(null, new object?[] { bundle.LocationOrUnique, bundle.Key });
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: failed to resolve live bundle '{bundle.Key}'. {e}", LogLevel.Trace);
      return;
    }

    if (liveUnlockable == null)
    {
      return;
    }

    var donatedAnything = false;

    foreach (KeyValuePair<string, int> requirement in bundle.Price)
    {
      if (bundle.AlreadyPaid.ContainsKey(requirement.Key) || IsMoneyRequirement(requirement.Key))
      {
        continue;
      }

      if (TryDonateRequirement(player, liveUnlockable, requirement, bundle.Key))
      {
        donatedAnything = true;
      }
    }

    if (!donatedAnything)
    {
      return;
    }

    try
    {
      var allPaid = (bool)_unlockableAllRequirementsPaid!.Invoke(liveUnlockable, null)!;
      if (allPaid)
      {
        _unlockableProcessPurchase!.Invoke(liveUnlockable, null);
      }
    }
    catch (Exception e)
    {
      _monitor.Log(
        $"Unlockable Bundles integration: donated items toward '{bundle.Key}' but failed to finalize "
        + $"completion; it should still complete normally next time you visit it in person. {e}",
        LogLevel.Warn
      );
    }
  }

  private bool TryDonateRequirement(Farmer player, object liveUnlockable, KeyValuePair<string, int> requirement, string bundleName)
  {
    bool hasEnough;
    try
    {
      hasEnough = (bool)_inventoryHasEnoughItems!.Invoke(null, new object[] { player, requirement })!;
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: HasEnoughItems check failed for '{requirement.Key}'. {e}", LogLevel.Trace);
      return false;
    }

    if (!hasEnough)
    {
      return false;
    }

    try
    {
      // Credit first, remove second - see class remarks for why.
      _unlockableProcessContribution!.Invoke(liveUnlockable, new object?[] { requirement, -1, null });
      _inventoryRemoveItemsOfRequirement!.Invoke(null, new object[] { player, requirement });
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: failed to process a contribution toward '{bundleName}'. {e}", LogLevel.Warn);
      return false;
    }

    if (_config.ShowNotifications)
    {
      _chat.NotifyDonation(DescribeRequirement(requirement), 1, bundleName);
    }

    return true;
  }

  private static bool IsMoneyRequirement(string requirementKey)
  {
    return requirementKey.Split(',').Any(part => part.Trim().Equals("money", StringComparison.OrdinalIgnoreCase));
  }

  private static string DescribeRequirement(KeyValuePair<string, int> requirement)
  {
    string firstAlternative = requirement.Key.Split(',')[0].Trim();
    string idPart = firstAlternative.Split(':')[0].Trim();
    if (idPart.Length > 0 && idPart[0] != '(')
    {
      idPart = "(O)" + idPart;
    }

    return ItemRegistry.GetData(idPart)?.DisplayName ?? firstAlternative;
  }
}
