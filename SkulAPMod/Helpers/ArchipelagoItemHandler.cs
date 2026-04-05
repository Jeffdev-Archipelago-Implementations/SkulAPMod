using Data;
using Level.Npc;

namespace SkulAPMod
{
    public static class ArchipelagoItemHandler
    {
        // Set while GrantItem is calling SetRescued so the NpcPatches postfix
        // knows not to send a location check for an AP-granted NPC.
        internal static bool GrantingNpc = false;

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

                case ArchipelagoConstants.FoxNpc:
                case ArchipelagoConstants.OgreNpc:
                case ArchipelagoConstants.DruidNpc:
                case ArchipelagoConstants.DeathKnightNpc:
                    GrantingNpc = true;
                    GameData.Progress.SetRescued(itemId switch
                    {
                        ArchipelagoConstants.FoxNpc        => NpcType.Fox,
                        ArchipelagoConstants.OgreNpc       => NpcType.Ogre,
                        ArchipelagoConstants.DruidNpc      => NpcType.Druid,
                        _                                  => NpcType.DeathKnight,
                    }, true);
                    GrantingNpc = false;
                    break;
            }
        }
    }
}
