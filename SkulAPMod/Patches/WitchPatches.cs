using Archipelago.MultiClient.Net.Models;
using Characters;
using HarmonyLib;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            long[][] baseLookup = { ArchipelagoConstants.SkullBonusLocations, 
                ArchipelagoConstants.BodyBonusLocations, 
                ArchipelagoConstants.SoulBonusLocations };
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
        internal static readonly Dictionary<long, ScoutedItemInfo> _scoutCache = new Dictionary<long, ScoutedItemInfo>();

        internal static void PreloadCache(Dictionary<long, ScoutedItemInfo> data)
        {
            foreach (var kv in data)
                _scoutCache[kv.Key] = kv.Value;
        }

        static void Postfix(
            WitchBonus.Bonus ____bonus,
            TMP_Text ____name,
            TMP_Text ____description,
            TMP_Text ____nextLevelDescription,
            GameObject ____nextLevelContainer)
        {
            if (!SkulAPMod.APClient.IsConnected) return;
            
            long? baseId = GetBaseLocationId(____bonus);
            if (baseId.HasValue && ____bonus.level < ____bonus.maxLevel)
            {
                long locationId = baseId.Value + ____bonus.level;
                var info = GetScoutInfo(locationId);
                if (info != null)
                {
                    string color = Utils.GetItemColor(info.Flags);
                    string name = $"<color=#{color}>{info.ItemName}</color>";
                    string desc = Utils.GetItemDescText(info.Flags, info.Player.Name);
                    string extraText = $"{____bonus.displayName} {____bonus.level + 1}";
                        
                    ____name.text = name;
                    ____description.text = desc;
                    if (____nextLevelContainer.activeSelf)
                        ____nextLevelDescription.text = extraText;
                    UpdateAPIcon(____name, true);
                    return;
                }
            }
            
            if (____nextLevelContainer.activeSelf)
                ____nextLevelDescription.text = "???";
            UpdateAPIcon(____name, false);
        }

        private static void UpdateAPIcon(TMP_Text nameText, bool visible)
        {
            const string iconName = "APIcon";
            const float iconSize = 36f;
            const float iconPadding = 10f;

            var existing = nameText.transform.Find(iconName);
            RectTransform rt;

            if (existing == null)
            {
                if (!visible) return;
                var go = new GameObject(iconName);
                go.transform.SetParent(nameText.transform, false);
                var img = go.AddComponent<Image>();
                img.sprite = SkulAPMod._archipelagoSprite;
                img.preserveAspect = true;
                rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(iconSize, iconSize);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                existing.gameObject.SetActive(visible);
                if (!visible) return;
                rt = existing.GetComponent<RectTransform>();
            }

            rt.anchoredPosition = new Vector2(nameText.preferredWidth + iconPadding, 0f);
        }

        private static ScoutedItemInfo GetScoutInfo(long locationId)
        {
            if (_scoutCache.TryGetValue(locationId, out var cached)) return cached;
            var info = SkulAPMod.APClient.TryScoutLocation(locationId, false);
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
