using HarmonyLib;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Data.GameData.Generic.Tutorial), "isPlayed")]
    public static class Tutorial_isPlayed_Patch
    {
        static void Postfix(ref bool __result)
        {
            if (SkulAPMod.APClient.IsConnected)
            {
                // Always set Tutorial to true so we don't have to deal with tutorial every time.
                __result = true;
            }
        }
    }
}
