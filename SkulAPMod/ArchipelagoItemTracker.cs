using System.Collections.Generic;
using System.Linq;

namespace SkulAPMod
{
    public static class ArchipelagoItemTracker
    {
        private static Dictionary<long, int> ReceivedItems   => APSaveManager.SaveData.ReceivedItems;
        private static HashSet<long>          CheckedLocations => APSaveManager.SaveData.CheckedLocations;

        public static void AddReceivedItem(long itemId)
        {
            ReceivedItems[itemId] = ReceivedItems.TryGetValue(itemId, out int count) ? count + 1 : 1;
            APSaveManager.WriteToDisk();
        }

        public static bool HasItem(long itemId) => ReceivedItems.ContainsKey(itemId);
        public static int AmountOfItem(long itemId) => ReceivedItems.TryGetValue(itemId, out int val) ? val : 0;

        public static void AddCheckedLocation(long locationId)
        {
            if (CheckedLocations.Add(locationId))
                APSaveManager.WriteToDisk();
        }

        public static bool HasLocation(long locationId) => CheckedLocations.Contains(locationId);
        public static int GetCheckedLocationCount() => CheckedLocations.Count;
        
        public static int LoadFromServer()
        {
            try
            {
                var session = SkulAPMod.APClient.GetSession();
                if (session == null) return 0;

                var allItems = session.Items.AllItemsReceived;
                int serverCount = allItems?.Count ?? 0;
                int grantedCount = ReceivedItems.Values.Sum();

                if (serverCount > grantedCount)
                {
                    Log.Message($"[AP] Granting {serverCount - grantedCount} offline item(s)");
                    foreach (var item in allItems.Skip(grantedCount))
                    {
                        long itemId = item.ItemId;
                        SkulAPMod.QueueMainThreadAction(() =>
                        {
                            AddReceivedItem(itemId);
                            ArchipelagoItemHandler.GrantItem(itemId);
                        });
                    }
                }

                var serverChecked = new HashSet<long>(session.Locations.AllLocationsChecked ?? Enumerable.Empty<long>());
                var pending = CheckedLocations.Where(id => !serverChecked.Contains(id)).ToArray();
                if (pending.Length > 0)
                {
                    Log.Message($"[AP] Re-sending {pending.Length} pending location check(s)");
                    session.Locations.CompleteLocationChecks(pending);
                }

                return serverCount;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[AP] LoadFromServer exception: {ex}");
                return 0;
            }
        }

        public static void LogAllReceivedItems()
        {
            int total = ReceivedItems.Values.Sum();
            Log.Message($"[AP Debug] === Received Items ({total} total) ===");
            foreach (var kv in ReceivedItems.OrderBy(kv => kv.Key))
                Log.Message($"[AP Debug] Item {kv.Key}: x{kv.Value}");
        }

        public static void LogAllCheckedLocations()
        {
            Log.Message($"[AP Debug] === Checked Locations ({CheckedLocations.Count} total) ===");
            foreach (var id in CheckedLocations.OrderBy(x => x))
                Log.Message($"[AP Debug] Location {id}");
        }
    }
}
