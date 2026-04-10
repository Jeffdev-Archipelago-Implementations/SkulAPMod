using Characters.Gear.Weapons;
using Data;
using Level.Npc;
using Services;
using Singletons;
using SkulAPMod.Patches;
using UnityEngine;

namespace SkulAPMod
{
    public static class ArchipelagoItemHandler
    {
        public static float QuartzMultiplier = 1f;
        public static int ReqRoomCount = 8;
        public static int ShrineChecksCount = 5;
        public static bool TrapsEnabled = true;

        public static void LoadOptions()
        {
            if (float.TryParse(SkulAPMod.APClient.GetSlotDataValue(ArchipelagoConstants.QuartzMultOption),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float mult))
            {
                QuartzMultiplier = mult;
                Log.Message($"[Slot Data] quartz_mult = {QuartzMultiplier}");
            }

            if (int.TryParse(SkulAPMod.APClient.GetSlotDataValue(ArchipelagoConstants.ReqRoomCountOption),
                    out int roomCount))
            {
                ReqRoomCount = roomCount;
                Log.Message($"[Slot Data] req_room_count = {ReqRoomCount}");
            }

            if (int.TryParse(SkulAPMod.APClient.GetSlotDataValue(ArchipelagoConstants.ShrineChecksOption),
                    out int shrineCount))
            {
                ShrineChecksCount = shrineCount;
                Log.Message($"[Slot Data] shrine_checks_count = {ShrineChecksCount}");
            }

            string trapsVal = null;
            try { trapsVal = SkulAPMod.APClient.GetSlotDataValue(ArchipelagoConstants.TrapsEnabledOption); } catch { }
            if (trapsVal != null)
            {
                TrapsEnabled = trapsVal == "1" || trapsVal.Equals("true", System.StringComparison.OrdinalIgnoreCase);
                Log.Message($"[Slot Data] traps_enabled = {TrapsEnabled}");
            }
        }

        public static void GrantItem(long itemId)
        {
            switch (itemId)
            {
                case ArchipelagoConstants.GoldItem:
                    GameData.Currency.gold.Earn(ArchipelagoConstants.GoldAmount);
                    break;

                case ArchipelagoConstants.DarkQuartzItem:
                    GameData.Currency.darkQuartz.Earn(ArchipelagoConstants.DarkQuartzAmount);
                    break;

                case ArchipelagoConstants.BoneItem:
                    GameData.Currency.bone.Earn(ArchipelagoConstants.BoneAmount);
                    break;

                case ArchipelagoConstants.MarrowTransplant:
                case ArchipelagoConstants.QuickDislocation:
                case ArchipelagoConstants.NutritionSupply:
                case ArchipelagoConstants.ExoskeletonReinforcement:
                case ArchipelagoConstants.ThickBone:
                case ArchipelagoConstants.FracturePrevention:
                case ArchipelagoConstants.HeavyFrame:
                case ArchipelagoConstants.Reassemble:
                case ArchipelagoConstants.SpiritAcceleration:
                case ArchipelagoConstants.AncestralFortitude:
                case ArchipelagoConstants.FatalMind:
                case ArchipelagoConstants.AncientAlchemy:
                    WitchStatApplicator.Apply(itemId);
                    break;

                case ArchipelagoConstants.DeSkullTrap:
                    if (!TrapsEnabled) break;
                    var player = Singleton<Service>.Instance.levelManager.player;
                    if (player == null) break;
                    var weaponInventory = player.playerComponents.inventory.weapon;
                    int secondaryIndex = 1 - weaponInventory.currentIndex;
                    Weapon secondary = weaponInventory.weapons[secondaryIndex];
                    if (secondary == null) break;
                    Log.Message($"[AP] De-Skull Trap: removing secondary skull at slot {secondaryIndex}");
                    weaponInventory.Unequip(secondary);
                    Object.Destroy(secondary.gameObject);
                    break;

                case ArchipelagoConstants.FoxNpc:
                case ArchipelagoConstants.OgreNpc:
                case ArchipelagoConstants.DruidNpc:
                case ArchipelagoConstants.DeathKnightNpc:
                    Patches.GameDataProgress_SetRescued_Patch.BypassCheck = true;
                    GameData.Progress.SetRescued(itemId switch
                    {
                        ArchipelagoConstants.FoxNpc        => NpcType.Fox,
                        ArchipelagoConstants.OgreNpc       => NpcType.Ogre,
                        ArchipelagoConstants.DruidNpc      => NpcType.Druid,
                        _                                  => NpcType.DeathKnight,
                    }, true);
                    Patches.GameDataProgress_SetRescued_Patch.BypassCheck = false;
                    break;
            }
        }
    }
}
