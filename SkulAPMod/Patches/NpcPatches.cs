using Data;
using HarmonyLib;
using Level.Npc;
using SkulAPMod;

namespace SkulAPMod.Patches
{
    // Block in-game NPC rescues until the corresponding AP item has been received.
    [HarmonyPatch(typeof(GameData.Progress), "SetRescued")]
    public class GameDataProgress_SetRescued_Patch
    {
        // Set this before calling SetRescued from GrantItem so we don't block ourselves.
        public static bool BypassCheck;

        static bool Prefix(NpcType npcType, bool value)
        {
            if (!value) return true;
            if (!SkulAPMod.APClient.IsConnected) return true;
            if (BypassCheck) return true;

            long? required = npcType switch
            {
                NpcType.Fox         => ArchipelagoConstants.FoxNpc,
                NpcType.Ogre        => ArchipelagoConstants.OgreNpc,
                NpcType.Druid       => ArchipelagoConstants.DruidNpc,
                NpcType.DeathKnight => ArchipelagoConstants.DeathKnightNpc,
                _                   => null
            };

            if (required == null) return true;

            bool allowed = ArchipelagoItemTracker.HasItem(required.Value);
            if (!allowed)
                Log.Message($"[AP] Blocked rescue of {npcType} — item not yet received.");
            return allowed;
        }
    }

    // When the player purchases a renovation, send the next unsent check.
    [HarmonyPatch(typeof(GameData.Progress), "housingPoint", MethodType.Setter)]
    public class Progress_HousingPoint_Set_Patch
    {
        static void Postfix()
        {
            if (!SkulAPMod.APClient.IsConnected) return;

            int sent = 0;
            for (int i = 0; i < 4; i++)
                if (ArchipelagoItemTracker.HasLocation(ArchipelagoConstants.CastleRepair1 + i)) sent++;

            if (sent >= 4) return;
            SkulAPMod.APClient.SendLocation(ArchipelagoConstants.CastleRepair1 + sent);
        }
    }
}
