using Archipelago.MultiClient.Net.Models;
using Characters;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Data.GameData.Progress.WitchMastery), "Save")]
    public class WitchMastery_SaveJson_Patch
    {
        static bool Prefix(Data.GameData.Progress.WitchMastery __instance)
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureWitchMastery(__instance);
            SendWitchLocationChecks(APSaveManager.SaveData.WitchMastery);
            return false;
        }

        private static void SendWitchLocationChecks(APSaveData.WitchMasteryEntry wm)
        {
            long[][] baseLookup = { ArchipelagoConstants.SkullBonusLocations, ArchipelagoConstants.BodyBonusLocations, ArchipelagoConstants.SoulBonusLocations };
            int[][] levels      = { wm.Skull, wm.Body, wm.Soul };

            for (int tree = 0; tree < 3; tree++)
                for (int i = 0; i < levels[tree].Length; i++)
                    for (int lvl = 0; lvl < levels[tree][i]; lvl++)
                        if (!ArchipelagoItemTracker.HasLocation(baseLookup[tree][i] + lvl))
                            SkulAPMod.APClient.SendLocation(baseLookup[tree][i] + lvl);
        }
    }

    [HarmonyPatch(typeof(UI.Witch.Option), "UpdateTexts")]
    public class WitchOption_UpdateTexts_Patch
    {
        private static readonly Dictionary<long, ScoutedItemInfo> _scoutCache = new Dictionary<long, ScoutedItemInfo>();

        static void Postfix(
            WitchBonus.Bonus ____bonus,
            TMP_Text ____name,
            TMP_Text ____description,
            TMP_Text ____nextLevelDescription,
            GameObject ____nextLevelContainer)
        {
            if (!SkulAPMod.APClient.IsConnected) return;

            ____name.text = "AP Item";

            long? baseId = GetBaseLocationId(____bonus);
            if (baseId.HasValue && ____bonus.level < ____bonus.maxLevel)
            {
                long locationId = baseId.Value + ____bonus.level;
                var info = GetScoutInfo(locationId);
                if (info != null)
                {
                    string text = $"{info.ItemName}\nfor {info.Player.Name}";
                    ____description.text = text;
                    if (____nextLevelContainer.activeSelf)
                        ____nextLevelDescription.text = text;
                    return;
                }
            }

            ____description.text = "A mysterious item from another world...";
            if (____nextLevelContainer.activeSelf)
                ____nextLevelDescription.text = "???";
        }

        private static ScoutedItemInfo GetScoutInfo(long locationId)
        {
            if (_scoutCache.TryGetValue(locationId, out var cached)) return cached;
            var info = SkulAPMod.APClient.TryScoutLocation(locationId);
            if (info != null) _scoutCache[locationId] = info;
            return info;
        }

        private static long? GetBaseLocationId(WitchBonus.Bonus bonus)
        {
            var wb = WitchBonus.instance;
            if (wb == null || bonus == null) return null;

            int i = bonus.indexInTree;
            if (i < 0 || i >= 4) return null;

            if (ReferenceEquals(bonus.tree, wb.skull)) return ArchipelagoConstants.SkullBonusLocations[i];
            if (ReferenceEquals(bonus.tree, wb.body))  return ArchipelagoConstants.BodyBonusLocations[i];
            if (ReferenceEquals(bonus.tree, wb.soul))  return ArchipelagoConstants.SoulBonusLocations[i];
            return null;
        }
    }
}
