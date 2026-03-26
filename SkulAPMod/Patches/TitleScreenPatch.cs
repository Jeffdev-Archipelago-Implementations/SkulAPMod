using HarmonyLib;
using UnityEngine.IO;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Scenes.Main), "StartGame")]
    public static class TitleScreenPatch
    {
        // Set to true when we're programmatically calling StartGame after connection
        // so we don't intercept our own call.
        internal static bool Suppress;

        static bool Prefix(Scenes.Main __instance)
        {
            if (Suppress) return true;

            SkulAPMod.CreateUI();

            if (!SkulAPMod.APClient.IsConnected)
            {
                APSessionManager.PendingMainInstance = __instance;
                Log.Message("[AP] Waiting for Archipelago connection");
                return false;
            }
            
            return true;
        }
    }
}
