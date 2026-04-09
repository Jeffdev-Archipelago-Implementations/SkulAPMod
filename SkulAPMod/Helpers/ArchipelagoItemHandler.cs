using Data;
using Level.Npc;
using SkulAPMod.Patches;

namespace SkulAPMod
{
    public static class ArchipelagoItemHandler
    {
        public static float QuartzMultiplier = 1f;
        public static int ReqRoomCount = 8;

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

                case ArchipelagoConstants.FoxNpc:
                case ArchipelagoConstants.OgreNpc:
                case ArchipelagoConstants.DruidNpc:
                case ArchipelagoConstants.DeathKnightNpc:
                    GameData.Progress.SetRescued(itemId switch
                    {
                        ArchipelagoConstants.FoxNpc        => NpcType.Fox,
                        ArchipelagoConstants.OgreNpc       => NpcType.Ogre,
                        ArchipelagoConstants.DruidNpc      => NpcType.Druid,
                        _                                  => NpcType.DeathKnight,
                    }, true);
                    break;
            }
        }
    }
}
