using Data;
using HarmonyLib;

namespace SkulAPMod.Patches;

public static class OtherPatches
{
    [HarmonyPatch(typeof(GameData.Currency), "Earn", new[] { typeof(int) })]
    public class GameDataCurrency_Earn
    {
        static void Prefix(GameData.Currency __instance, ref int amount)
        {
            if (!SkulAPMod.APClient.IsConnected) return;
            if (!ReferenceEquals(__instance, GameData.Currency.darkQuartz)) return;
            amount = (int)(amount * ArchipelagoItemHandler.QuartzMultiplier);
        }
    }
}