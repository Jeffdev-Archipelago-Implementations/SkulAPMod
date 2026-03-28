using HarmonyLib;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Data.GameData.Currency), "Save")]
    public class Currency_SaveJson_Patch
    {
        static bool Prefix(Data.GameData.Currency __instance)
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureCurrency(__instance);
            return false;
        }
    }
}
