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
        static void Postfix(Data.GameData.Progress.WitchMastery __instance)
        {
            if (!SkulAPMod.APClient.IsConnected) return;
            SendWitchLocationChecks(__instance);
        }

        private static void SendWitchLocationChecks(Data.GameData.Progress.WitchMastery wm)
        {
            for (int i = 0; i < 4; i++)
            {
                CheckTree(ArchipelagoConstants.SkullBonusLocations[i], wm.skull[i].value);
                CheckTree(ArchipelagoConstants.BodyBonusLocations[i],  wm.body[i].value);
                CheckTree(ArchipelagoConstants.SoulBonusLocations[i],  wm.soul[i].value);
            }
        }

        private static void CheckTree(long baseId, int level)
        {
            for (int lvl = 0; lvl < level; lvl++)
                if (!ArchipelagoItemTracker.HasLocation(baseId + lvl))
                    SkulAPMod.APClient.SendLocation(baseId + lvl);
        }
    }
    
    public static class WitchLevelOverride
    {
        [System.ThreadStatic]
        public static int? Value;
    }

    // Intercept the level getter so Update()/Attach() see the AP count when we set the override.
    [HarmonyPatch(typeof(Characters.WitchBonus.Bonus), "level", MethodType.Getter)]
    public class WitchBonus_Level_Get_Patch
    {
        static bool Prefix(ref int __result)
        {
            if (WitchLevelOverride.Value.HasValue)
            {
                __result = WitchLevelOverride.Value.Value;
                return false;
            }
            return true;
        }
    }

    // When AP is connected, run LevelUp's side-effects (quartz, save) but bypass the
    // property setter so Attach()/Update() are not triggered — stats come from AP items only.
    [HarmonyPatch(typeof(Characters.WitchBonus.Bonus), "LevelUp")]
    public class WitchBonus_LevelUp_Patch
    {
        static bool Prefix(Characters.WitchBonus.Bonus __instance, ref bool __result)
        {
            if (!SkulAPMod.APClient.IsConnected) return true;

            if (!__instance.ready || __instance.level == __instance.maxLevel ||
                !Data.GameData.Currency.darkQuartz.Consume(__instance.levelUpCost))
            {
                __result = false;
                return false;
            }

            // Write directly to the backing data, skipping the property setter
            // so Attach() and Update() are not called.
            ((Data.Data<int>)(object)__instance._data).value++;
            Data.GameData.Currency.SaveAll();
            Data.GameData.Progress.SaveAll();
            __result = true;
            return false;
        }
    }

    // At game load, Initialize() would normally call Attach()/Update() based on the
    // saved purchase count. Override so it uses the AP-granted count instead.
    [HarmonyPatch(typeof(Characters.WitchBonus.Bonus), "Initialize")]
    public class WitchBonus_Initialize_Patch
    {
        static bool Prefix(Characters.WitchBonus.Bonus __instance)
        {
            if (!SkulAPMod.APClient.IsConnected) return true;

            int apLevel = ArchipelagoItemTracker.AmountOfWitchBonus(__instance._key);
            WitchLevelOverride.Value = apLevel;
            try
            {
                if (apLevel > 0) __instance.Attach();
                __instance.Update();
            }
            finally
            {
                WitchLevelOverride.Value = null;
            }
            return false;
        }
    }
    
    public static class WitchStatApplicator
    {
        public static void Apply(long itemId)
        {
            var wb = WitchBonus.instance;
            if (wb?.skull == null) return;

            var bonus = itemId switch
            {
                ArchipelagoConstants.MarrowTransplant         => (WitchBonus.Bonus)wb.skull.marrowImplant,
                ArchipelagoConstants.QuickDislocation         => wb.skull.fastDislocation,
                ArchipelagoConstants.NutritionSupply          => wb.skull.nutritionSupply,
                ArchipelagoConstants.ExoskeletonReinforcement => wb.skull.enhanceExoskeleton,
                ArchipelagoConstants.ThickBone                => wb.body.strongBone,
                ArchipelagoConstants.FracturePrevention       => wb.body.fractureImmunity,
                ArchipelagoConstants.HeavyFrame               => wb.body.heavyFrame,
                ArchipelagoConstants.Reassemble               => wb.body.reassemble,
                ArchipelagoConstants.SpiritAcceleration       => wb.soul.soulAcceleration,
                ArchipelagoConstants.AncestralFortitude       => wb.soul.willOfAncestor,
                ArchipelagoConstants.FatalMind                => wb.soul.fatalMind,
                ArchipelagoConstants.AncientAlchemy           => wb.soul.ancientAlchemy,
                _ => null
            };

            if (bonus == null) return;

            int apLevel    = ArchipelagoItemTracker.AmountOfItem(itemId);
            int prevLevel  = apLevel - 1;

            WitchLevelOverride.Value = apLevel;
            try
            {
                if (prevLevel == 0) bonus.Attach();
                bonus.Update();
            }
            finally
            {
                WitchLevelOverride.Value = null;
            }
        }
    }

    [HarmonyPatch(typeof(UI.Witch.TreeElement), "Set")]
    public class WitchTreeElement_Set_Patch
    {
        static void Postfix(ref UI.Witch.TreeElement __instance)
        {
            bool isInteractable = ArchipelagoItemTracker.CheckWitchTreeAvailability(
                __instance._bonus.tree.ToString(),
                __instance._bonus.indexInTree);
            
            Log.Info(isInteractable);

            __instance.interactable = isInteractable;
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
            TMP_Text ____level,
            TMP_Text ____description,
            TMP_Text ____nextLevelDescription,
            GameObject ____nextLevelContainer)
        {
            if (!SkulAPMod.APClient.IsConnected) return;
            
            long? baseId = GetBaseLocationId(____bonus);
            if (baseId.HasValue && ____bonus.level < ____bonus.maxLevel)
            {
                bool isInteractable = ArchipelagoItemTracker.CheckWitchTreeAvailability(
                    ____bonus.tree.ToString(),
                    ____bonus.indexInTree);
                
                long locationId = baseId.Value + ____bonus.level;
                var info = GetScoutInfo(locationId);
                ____level.text = $"Checks Sent: {____bonus.level}/{____bonus.maxLevel}";
                if (info != null)
                {
                    string color = isInteractable ? Utils.GetItemColor(info.Flags) : "ff0000";
                    string name = isInteractable ? $"<color=#{color}>{info.ItemName}</color>" : $"<color=#{color}>Locked</color>";
                    string desc = isInteractable ? Utils.GetItemDescText(info.Flags, info.Player.Name) : "You need more of this Progressive Tree to view this!";
                    int received = ArchipelagoItemTracker.AmountOfWitchBonus(____bonus._key);
                    string extraText = $"{____bonus.displayName} {____bonus.level + 1} (You currently have {received} of {____bonus.displayName} sent to you.)";

                    ____name.text = name;
                    ____description.text = desc;
                    if (____nextLevelContainer.activeSelf)
                        ____nextLevelDescription.text = extraText;
                    UpdateAPIcon(____name, true);
                    return;
                }
            }

            int totalReceived = ArchipelagoItemTracker.AmountOfWitchBonus(____bonus._key);
            ____description.text =
                $"{____description.text}\nYou have sent all checks attached to this. You currently have {totalReceived} of {____bonus.displayName} sent to you.";
            
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
