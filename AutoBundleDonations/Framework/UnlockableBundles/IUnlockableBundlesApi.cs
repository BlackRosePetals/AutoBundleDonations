using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace AutoBundleDonations.Framework.UnlockableBundles;

/// <summary>
///   Interop stub matching the public API published by the "Unlockable Bundles" framework mod (DLX.Bundles), which
///   lets content packs (Visit Mount Vapius, Joja Civic Center, etc.) define their own bundle systems outside the
///   vanilla Community Center. Reproduced from decompiling Unlockable Bundles.dll v4.3.1 - this is the framework's
///   deliberately-published, stable surface (read state + events), not an internal implementation detail.
/// </summary>
public interface IUnlockableBundlesApi
{
  public delegate void BundlesPurchasedDelegate(object sender, IBundlePurchasedEventArgs e);

  public delegate void BundlesContributedDelegate(object sender, IBundleContributedEventArgs e);

  public delegate void BundlesDiscoveredDelegate(object sender, IBundleDiscoveredEventArgs e);

  public delegate void IsReadyDelegate(object sender, IIsReadyEventArgs e);

  IList<string> PurchasedBundles { get; }

  IDictionary<string, IList<string>> PurchaseBundlesByLocation { get; }

  event BundlesDiscoveredDelegate BundleDiscoveredEvent;

  event BundlesContributedDelegate BundleContributedEvent;

  event BundlesPurchasedDelegate BundlePurchasedEvent;

  event IsReadyDelegate IsReadyEvent;

  IDictionary<string, IList<IBundle>> getBundles();

  int getWalletCurrency(string currencyId, long who);

  int addWalletCurrency(string currencyId, long who, int addedValue, bool broadcast, bool registerBillboard);
}

public interface IBundle
{
  string Key { get; }

  string Location { get; }

  string LocationOrUnique { get; }

  Point? TileLocation { get; }

  IDictionary<string, int> Price { get; }

  IDictionary<string, int> AlreadyPaid { get; }

  bool Purchased { get; }

  int DaysSincePurchase { get; }

  bool AssetLoaded { get; }

  bool Discovered { get; }
}

public interface IBundlePurchasedEventArgs
{
  Farmer Who { get; }

  string Location { get; }

  string LocationOrUnique { get; }

  IBundle Bundle { get; }

  bool IsBuyer { get; }
}

public interface IBundleContributedEventArgs
{
  Farmer Who { get; }

  KeyValuePair<string, int> Contribution { get; }

  string Location { get; }

  string LocationOrUnique { get; }

  IBundle Bundle { get; }

  bool IsContributor { get; }
}

public interface IBundleDiscoveredEventArgs
{
  Farmer Who { get; }

  string Location { get; }

  string LocationOrUnique { get; }

  IBundle Bundle { get; }

  bool IsDiscoverer { get; }
}

public interface IIsReadyEventArgs
{
  Farmer Who { get; }
}
