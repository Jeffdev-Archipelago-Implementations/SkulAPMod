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
                __result = true;
            }
        }
    }
}
