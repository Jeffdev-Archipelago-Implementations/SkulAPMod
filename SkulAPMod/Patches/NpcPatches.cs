using Data;
using HarmonyLib;
using Level.Npc;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(GameData.Progress), "SetRescued")]
    public class Progress_SetRescued_Patch
    {
        static void Postfix(NpcType npcType, bool value)
        {
            if (!value || !SkulAPMod.APClient.IsConnected || ArchipelagoItemHandler.GrantingNpc) return;

            long? locationId = npcType switch
            {
                NpcType.Fox        => ArchipelagoConstants.FoxNpcFreed,
                NpcType.Ogre       => ArchipelagoConstants.OgreNpcFreed,
                NpcType.Druid      => ArchipelagoConstants.DruidNpcFreed,
                NpcType.DeathKnight => ArchipelagoConstants.KnightNpcFreed,
                _                  => null
            };

            if (locationId.HasValue && !ArchipelagoItemTracker.HasLocation(locationId.Value))
                SkulAPMod.APClient.SendLocation(locationId.Value);
        }
    }
}
