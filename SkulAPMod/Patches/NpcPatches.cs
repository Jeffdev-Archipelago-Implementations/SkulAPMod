using Data;
using HarmonyLib;
using Level.Npc;
using SkulAPMod;

namespace SkulAPMod.Patches
{
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
