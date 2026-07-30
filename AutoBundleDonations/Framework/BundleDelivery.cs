using System;
using System.Collections.Generic;
using System.Linq;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using SObject = StardewValley.Object;

namespace AutoBundleDonations.Framework;

/// <summary>
///   Scans a player's inventory for items that satisfy an empty slot in an unlocked, incomplete Community Center
///   bundle (including the bonus "Missing Bundle" in the Abandoned Joja Mart) and deposits them automatically.
///   Mirrors the same code paths the game itself uses when a bundle is donated remotely via the
///   GameMenu/Collections "Bundles" tab (see <c>JunimoNoteMenu(fromGameMenu: true)</c>), which is the only vanilla
///   flow that completes bundles without the player standing in the Community Center - including its choice to
///   skip the restoreAreaCutscene() visual/cutscene entirely in that case. We deliberately do the same: bundle
///   slots are still filled and rewards are still granted, but a room only visually redecorates itself the next
///   time the player is physically there (exactly like remote donation already behaves in vanilla). The Missing
///   Bundle needs no special-cased completion bookkeeping at all: <see cref="AbandonedJojaMart" />'s own
///   resetSharedState() already detects when bundle 36 is fully filled and finishes the job (grants the
///   "ccMovieTheater" mail, sets the theater build date) the next time that location's shared state resets.
/// </summary>
internal sealed class BundleDelivery
{
  private const string CommunityCenterLocationName = "CommunityCenter";

  // Vanilla itself hardcodes this bundle index for the bonus room (see AbandonedJojaMart.resetSharedState).
  private const int MissingBundleArea = 6;

  private readonly ModConfig _config;
  private readonly ChatNotifier _chat;
  private readonly IMonitor _monitor;

  public BundleDelivery(ModConfig config, ChatNotifier chat, IMonitor monitor)
  {
    _config = config;
    _chat = chat;
    _monitor = monitor;
  }

  public void Run(Farmer player)
  {
    if (!_config.Enabled || !Context.IsMainPlayer)
    {
      return;
    }

    if (Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
    {
      return;
    }

    // The Community Center building itself isn't walk-in-able until this mail flag arrives (usually a few days
    // into the game). Without this check we'd donate to the always-open Crafts Room before the player could
    // possibly have visited the CC yet.
    if (!Game1.MasterPlayer.mailReceived.Contains("ccDoorUnlock"))
    {
      return;
    }

    if (Game1.getLocationFromName(CommunityCenterLocationName) is not CommunityCenter cc)
    {
      return;
    }

    // Deliberately not gated on cc.areAllAreasComplete() here: that flag only tracks the 6 main rooms, not the
    // Missing Bundle bonus room, so it would wrongly skip Missing Bundle donations right when they're most likely
    // to be relevant (after the main rooms are already done).
    Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;
    var bundleIndicesByArea = new Dictionary<int, List<int>>();
    var donatedAnything = false;

    foreach (KeyValuePair<string, string> entry in bundleData)
    {
      string[] keyParts = entry.Key.Split('/');
      if (keyParts.Length < 2 || !int.TryParse(keyParts[1], out int bundleIndex))
      {
        continue;
      }

      int area = CommunityCenter.getAreaNumberFromName(keyParts[0]);
      if (area < 0)
      {
        continue;
      }

      if (!bundleIndicesByArea.TryGetValue(area, out List<int>? bundleIndices))
      {
        bundleIndices = new List<int>();
        bundleIndicesByArea[area] = bundleIndices;
      }

      bundleIndices.Add(bundleIndex);

      if (!IsAreaEnabled(area) || !IsAreaUnlocked(cc, area))
      {
        continue;
      }

      if (!cc.bundles.FieldDict.TryGetValue(bundleIndex, out NetArray<bool, NetBool>? slotsFilled))
      {
        continue;
      }

      string[] valueParts = entry.Value.Split('/');
      if (valueParts.Length < 7)
      {
        continue;
      }

      string[] ingredientTokens = valueParts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
      string bundleName = valueParts[6];

      for (var i = 0; i + 2 < ingredientTokens.Length; i += 3)
      {
        int slot = i / 3;
        if (slot >= slotsFilled.Length || slotsFilled[slot])
        {
          continue;
        }

        if (TryDonateSlot(player, slotsFilled, slot, ingredientTokens[i], ingredientTokens[i + 1], ingredientTokens[i + 2], bundleName))
        {
          donatedAnything = true;
        }
      }

      if (IsBundleComplete(slotsFilled) && !cc.bundleRewards[bundleIndex])
      {
        cc.checkForNewJunimoNotes();
        cc.bundleRewards[bundleIndex] = true;
        _monitor.Log($"Bundle '{bundleName}' (index {bundleIndex}, area {area}) is now complete.", LogLevel.Debug);
      }
    }

    if (!donatedAnything)
    {
      return;
    }

    foreach (KeyValuePair<int, List<int>> areaEntry in bundleIndicesByArea)
    {
      // The Missing Bundle's completion isn't tracked through CommunityCenter.areasComplete (a fixed-size array
      // of just the 6 main rooms) or areaCompleteReward's switch (which has no case for it) - AbandonedJojaMart
      // finishes it on its own the next time its shared state resets, so there's nothing to do here.
      if (areaEntry.Key == MissingBundleArea)
      {
        continue;
      }

      int completedCount = areaEntry.Value.Count(idx =>
        cc.bundles.FieldDict.TryGetValue(idx, out NetArray<bool, NetBool>? slots) && IsBundleComplete(slots));
      bool areaComplete = completedCount == areaEntry.Value.Count;
      if (!areaComplete)
      {
        continue;
      }

      bool wasAlreadyComplete = areaEntry.Key < 6 && cc.areasComplete[areaEntry.Key];
      cc.markAreaAsComplete(areaEntry.Key);
      cc.areaCompleteReward(areaEntry.Key);
      if (!wasAlreadyComplete)
      {
        _monitor.Log(
          $"All {completedCount} bundles in area {areaEntry.Key} are complete; called markAreaAsComplete + "
          + $"areaCompleteReward (Game1.currentLocation is CommunityCenter: {Game1.currentLocation == cc}).",
          LogLevel.Info
        );
      }
    }

    // Keep the cache other mods (and the vanilla note icons) read from in sync with what we just donated.
    cc.refreshBundlesIngredientsInfo();
  }

  private bool IsAreaEnabled(int area)
  {
    return area switch
    {
      0 => _config.DonatePantry,
      1 => _config.DonateCraftsRoom,
      2 => _config.DonateFishTank,
      3 => _config.DonateBoilerRoom,
      4 => true, // Vault: money-only bundles, no item will ever match, so this toggle is moot
      5 => _config.DonateBulletinBoard,
      MissingBundleArea => _config.DonateMissingBundle,
      _ => false,
    };
  }

  private static bool IsAreaUnlocked(CommunityCenter cc, int area)
  {
    // shouldNoteAppearInArea(6) only checks that the prerequisite story event has been *seen*, not that the
    // room's door mail flag has actually been received - those can be days apart while waiting for a stormy
    // night. Gate on the same flag the game itself uses to decide whether the room is walk-in-able.
    if (area == MissingBundleArea)
    {
      return Game1.MasterPlayer.mailReceived.Contains("abandonedJojaMartAccessible");
    }

    return cc.shouldNoteAppearInArea(area);
  }

  private bool TryDonateSlot(
    Farmer player,
    NetArray<bool, NetBool> slots,
    int slot,
    string itemIdToken,
    string countToken,
    string qualityToken,
    string bundleName
  )
  {
    if (!int.TryParse(countToken, out int requiredCount) || !int.TryParse(qualityToken, out int requiredQuality))
    {
      return false;
    }

    bool isCategoryMatch = int.TryParse(itemIdToken, out int category) && category < 0;
    string? qualifiedId = isCategoryMatch ? null : ItemRegistry.GetData(itemIdToken)?.QualifiedItemId ?? "(O)" + itemIdToken;

    bool Matches(SObject obj)
    {
      if (obj.bigCraftable.Value || obj.Quality < requiredQuality)
      {
        return false;
      }

      return isCategoryMatch ? obj.Category == category : obj.QualifiedItemId == qualifiedId;
    }

    var candidateIndices = new List<int>();
    var totalAvailable = 0;
    for (var i = 0; i < player.Items.Count; i++)
    {
      if (player.Items[i] is SObject obj && Matches(obj))
      {
        candidateIndices.Add(i);
        totalAvailable += obj.Stack;
      }
    }

    if (totalAvailable < requiredCount)
    {
      return false;
    }

    // Consume from the lowest-quality qualifying stacks first, so any higher-quality stock is left untouched.
    candidateIndices.Sort((a, b) => ((SObject)player.Items[a]!).Quality.CompareTo(((SObject)player.Items[b]!).Quality));

    string donatedItemName = ((SObject)player.Items[candidateIndices[0]]!).DisplayName;
    int remaining = requiredCount;
    foreach (int index in candidateIndices)
    {
      if (remaining <= 0)
      {
        break;
      }

      var obj = (SObject)player.Items[index]!;
      int take = Math.Min(remaining, obj.Stack);
      obj.Stack -= take;
      remaining -= take;
      if (obj.Stack <= 0)
      {
        player.Items[index] = null;
      }
    }

    slots[slot] = true;

    if (_config.ShowNotifications)
    {
      _chat.NotifyDonation(donatedItemName, requiredCount, bundleName);
    }

    _monitor.Log($"Auto-donated {requiredCount}x {donatedItemName} to the {bundleName} bundle.", LogLevel.Trace);

    return true;
  }

  private static bool IsBundleComplete(NetArray<bool, NetBool> slots)
  {
    for (var i = 0; i < slots.Length; i++)
    {
      if (!slots[i])
      {
        return false;
      }
    }

    return true;
  }
}
