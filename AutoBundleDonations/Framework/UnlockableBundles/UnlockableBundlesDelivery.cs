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
///     <item>Only the "CCBundle" shop type is touched at all. Other shop types (a single-item "repair this for
///     N wood?" prompt, a parrot perch, etc.) finish through their own UI-coupled event/cutscene flow that this
///     integration cannot safely trigger headlessly - calling ProcessPurchase() without going through it marks
///     the bundle permanently Completed while the actual unlock (a map patch, typically) never applies, which is
///     a real soft-lock, not just a missed convenience. See <see cref="TryDonateToBundle" />.</item>
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
  private MethodInfo? _unlockableHasShopEvent;
  private MethodInfo? _unlockableProcessShopEvent;
  private PropertyInfo? _unlockableShopType;
  private PropertyInfo? _unlockableBundleSlots;
  private PropertyInfo? _unlockableBookId;

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

    // Despite the name, IUnlockableBundlesApi.getBundles() keys this dictionary by bundle ID, not by location -
    // confirmed by reading UnlockableBundlesAPI.GetAllBundleStates(): dictionary2.Add(key, list) where key is
    // unlockableSaveDatum.Key (the bundle's own ID) and list is that same bundle's states across locations. So
    // each IList<IBundle> here is "this one bundle's states," never a group of sibling bundles - it can't be
    // used to look up a page's parent book. Flatten everything into a single by-ID lookup instead.
    List<IBundle> allBundles = bundlesByLocation.Values.SelectMany(list => list).ToList();
    Dictionary<string, IBundle> bundlesByKey = allBundles
      .GroupBy(b => b.Key)
      .ToDictionary(g => g.Key, g => g.First());

    foreach (IBundle bundle in allBundles)
    {
      if (bundle.Purchased)
      {
        continue;
      }

      TryDonateToBundle(player, bundle, bundlesByKey);
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
      // _api.GetType().Assembly is NOT Unlockable Bundles.dll here - SMAPI's GetApi<T>() hands back a dynamically
      // generated proxy that implements IUnlockableBundlesApi, living in its own "StardewModdingAPI.Proxies"
      // assembly, so reflecting off that object's type only ever finds the proxy's own members. The mod's real
      // assembly has to be located separately, by its DLL name, among everything the game has already loaded.
      Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unlockable Bundles")
        ?? throw new TypeLoadException("Could not find the loaded 'Unlockable Bundles' assembly among the game's loaded assemblies.");

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
      _unlockableHasShopEvent = RequireMethod(unlockableType, "HasShopEvent");
      _unlockableProcessShopEvent = RequireMethod(unlockableType, "ProcessShopEvent", typeof(Action));
      _unlockableShopType = unlockableType.GetProperty("ShopType", BindingFlags.Public | BindingFlags.Instance)
        ?? throw new MissingMemberException(unlockableType.FullName, "ShopType");
      _unlockableBundleSlots = unlockableType.GetProperty("BundleSlots", BindingFlags.Public | BindingFlags.Instance)
        ?? throw new MissingMemberException(unlockableType.FullName, "BundleSlots");
      _unlockableBookId = unlockableType.GetProperty("BookId", BindingFlags.Public | BindingFlags.Instance)
        ?? throw new MissingMemberException(unlockableType.FullName, "BookId");

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

  private void TryDonateToBundle(Farmer player, IBundle bundle, IReadOnlyDictionary<string, IBundle> bundlesByKey)
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

    // A bundle needs to be Discovered before we'll touch it, mirroring vanilla's shouldNoteAppearInArea gate -
    // the physical, in-world confirmation that the player has actually clicked on it at least once. Pages
    // (BookId non-empty, per Unlockable.IsPage()) are a wrinkle: confirmed by reading the framework's own code
    // that a page's Unlockable never gets its own physical ShopObject in the world - it only ever lives inside
    // the parent book's ShopObject.Pages list (see ShopPlacement.PlaceShop) - and neither BundleBookMenu nor
    // BundlePageMenu ever calls SetDiscovered for a page, so a page's own Discovered flag never becomes true no
    // matter what the player does. But that doesn't mean pages should skip the gate entirely: confirmed in
    // testing that doing so let items auto-donate into a page whose parent book had never been clicked on in
    // the world at all (i.e. the player never had any way to even know that bundle existed). The correct proxy
    // is the *book's* Discovered flag - it's the one thing in this framework that actually flips only when the
    // player physically interacts with something in the world - so pages are gated on their parent book instead
    // of themselves.
    string? bookId;
    try
    {
      bookId = _unlockableBookId!.GetValue(liveUnlockable)?.ToString();
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: failed to read BookId for '{bundle.Key}'. {e}", LogLevel.Trace);
      return;
    }

    bool discovered;
    if (string.IsNullOrEmpty(bookId))
    {
      discovered = bundle.Discovered;
    }
    else if (bundlesByKey.TryGetValue(bookId, out IBundle? parentBook))
    {
      discovered = parentBook.Discovered;
    }
    else
    {
      // Can't find the parent book to check - fail safe rather than risk donating into something the player
      // has never seen.
      _monitor.Log(
        $"Unlockable Bundles integration: skipping '{bundle.Key}' - could not find its parent book '{bookId}' to check Discovered.",
        LogLevel.Trace
      );
      return;
    }

    if (!discovered)
    {
      _monitor.Log($"Unlockable Bundles integration: skipping '{bundle.Key}' - not yet Discovered.", LogLevel.Trace);
      return;
    }

    // Only the "CCBundle" shop type (a JunimoNoteMenu-style grid of slots, like this integration was built and
    // verified against) completes safely through ProcessContribution + AllRequirementsPaid + ProcessPurchase
    // alone. The other types (Dialogue, SpeechBubble, ParrotPerch - e.g. a "repair this bridge for 100 Stone?"
    // prompt) route completion through their own UI interaction (a mutex lock, a squawk-delay timer, then
    // SpeechBubble.WaitingForProcessShopEvent) that eventually calls Unlockable.ProcessShopEvent() to actually
    // apply the map patch/cutscene. Calling ProcessPurchase() ourselves without ever going through that flow
    // marks the bundle Completed (so the player can never re-trigger the prompt) while the world-visible unlock
    // never happens - a permanent soft-lock, confirmed in testing. So: leave anything that isn't CCBundle alone
    // entirely, never even touching the player's inventory for it.
    string? shopType;
    try
    {
      shopType = _unlockableShopType!.GetValue(liveUnlockable)?.ToString();
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: failed to read ShopType for '{bundle.Key}'. {e}", LogLevel.Trace);
      return;
    }

    if (shopType != "CCBundle")
    {
      _monitor.Log($"Unlockable Bundles integration: skipping '{bundle.Key}' - ShopType is '{shopType}', not CCBundle.", LogLevel.Trace);
      return;
    }

    // Even CCBundle bundles can have a scripted ShopEvent attached (e.g. an intro cutscene) - confirmed by
    // reading BundlePageMenu.CheckIfBundleIsComplete()/update(): completing a bundle only calls
    // Unlockable.ProcessPurchase() (which just flags Completed + queues a mail letter), and the ACTUAL reward
    // grant + map patch application (Unlockable.ProcessShopEvent(), which fades to black and starts a
    // location-bound Event if HasShopEvent() is true) only fires from the menu's own update() loop after an
    // 800ms UI timer - something that only runs while that menu is open. Confirmed in testing: auto-completing
    // 'Lumisteria.MtVapius_StartingBundle' (which has a shop event) headlessly soft-locked it - Completed got
    // set, but the cutscene/reward never ran and the player had no way to trigger it afterward. Bundles without
    // a shop event are unaffected (ProcessShopEvent's non-event branch just calls MapPatches.ApplyUnlockable +
    // OpenRewardsMenu directly, no UI/location dependency), which is why donating to those completed cleanly.
    // So: skip shop-event bundles entirely, same as non-CCBundle types - leave them for the player to finish
    // manually in person, where the normal menu flow handles the cutscene safely.
    bool hasShopEvent;
    try
    {
      hasShopEvent = (bool)_unlockableHasShopEvent!.Invoke(liveUnlockable, null)!;
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: failed to read HasShopEvent for '{bundle.Key}'. {e}", LogLevel.Trace);
      return;
    }

    if (hasShopEvent)
    {
      _monitor.Log($"Unlockable Bundles integration: skipping '{bundle.Key}' - has a ShopEvent (cutscene-driven completion).", LogLevel.Trace);
      return;
    }

    // Unlockable._alreadyPaidIndex[key] is later used as a direct index into BundlePageMenu.AlreadyPaidSlots
    // (a list sized to BundleSlots) - see UpdateAlreadyPaidSlots(). The framework's own donation code
    // (BundlePageMenu.TryPayExceptionItem) assigns this by finding the first still-empty slot, which - since
    // slots fill strictly in order and are never freed early - is equivalent to "how many requirements are
    // already paid, before this one." We have to reproduce that ourselves and pass it explicitly: leaving
    // ProcessContribution's index at its default (-1) stores -1 into _alreadyPaidIndex, and the next time the
    // player opens that bundle's menu, AlreadyPaidSlots[-1] throws ArgumentOutOfRangeException on every single
    // update tick (confirmed in testing - the game log showed exactly this exception spamming from
    // BundlePageMenu.UpdateAlreadyPaidSlots via ShopObject.OpenMenu).
    //
    // The count has to come from bundle.AlreadyPaid (the API snapshot, backed by ModData.SetPartiallyPurchased -
    // a plain synchronous Dictionary write), not from reflecting Unlockable's own live "_alreadyPaid" netcode
    // dictionary field: confirmed in testing that reading the latter via a raw IEnumerable cast returns a
    // constant, wrong count across separate donation passes (repeatedly reads back the count from before our
    // first-ever donation, even though the framework's own AllRequirementsPaid() - which reads the same field
    // through its proper typed accessor - tracks the true count fine). That stale count made every donation
    // after the first reuse the same slot index, so the AlreadyPaidSlots UI visibly overwrote one donated item
    // with the next instead of showing both.
    int bundleSlots;
    try
    {
      bundleSlots = (int)_unlockableBundleSlots!.GetValue(liveUnlockable)!;
    }
    catch (Exception e)
    {
      _monitor.Log($"Unlockable Bundles integration: failed to read BundleSlots for '{bundle.Key}'. {e}", LogLevel.Trace);
      return;
    }

    int nextSlotIndex = bundle.AlreadyPaid.Count;

    _monitor.Log(
      $"Unlockable Bundles integration: '{bundle.Key}' has BundleSlots={bundleSlots}, "
      + $"{bundle.Price.Count} priced requirement(s), already-paid count={nextSlotIndex} before this pass.",
      LogLevel.Trace
    );

    var donatedAnything = false;

    foreach (KeyValuePair<string, int> requirement in bundle.Price)
    {
      if (bundle.AlreadyPaid.ContainsKey(requirement.Key) || IsMoneyRequirement(requirement.Key))
      {
        continue;
      }

      if (nextSlotIndex >= bundleSlots)
      {
        break;
      }

      if (TryDonateRequirement(player, liveUnlockable, requirement, nextSlotIndex, bundle.Key))
      {
        donatedAnything = true;
        nextSlotIndex++;
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
        // ProcessPurchase() alone doesn't grant the reward or apply the map patch - see the HasShopEvent
        // remarks above. We already confirmed this bundle has no ShopEvent, so ProcessShopEvent()'s
        // no-event branch is a pure state mutation (MapPatches.ApplyUnlockable + OpenRewardsMenu), safe to
        // call immediately without a menu open.
        _unlockableProcessShopEvent!.Invoke(liveUnlockable, new object?[] { null });
        _monitor.Log($"Unlockable Bundles integration: completed and purchased '{bundle.Key}'.", LogLevel.Info);
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

  private bool TryDonateRequirement(Farmer player, object liveUnlockable, KeyValuePair<string, int> requirement, int slotIndex, string bundleName)
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
      _unlockableProcessContribution!.Invoke(liveUnlockable, new object?[] { requirement, slotIndex, null });
      _inventoryRemoveItemsOfRequirement!.Invoke(null, new object[] { player, requirement });
      _monitor.Log(
        $"Unlockable Bundles integration: donated '{requirement.Key}' toward '{bundleName}' at slot index {slotIndex}.",
        LogLevel.Trace
      );
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
