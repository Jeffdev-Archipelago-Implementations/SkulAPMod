using HarmonyLib;

namespace SkulAPMod.Patches
{
    // Currency.Save (instance) — SaveAll is covered indirectly since it calls this per-currency
    [HarmonyPatch(typeof(Data.GameData.Currency), "Save")]
    public class Currency_Save_Patch
    {
        static bool Prefix(Data.GameData.Currency __instance)
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureCurrency(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Data.GameData.Generic), "SaveAll")]
    public class Generic_SaveAll_Patch
    {
        static bool Prefix()
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureGeneric();
            return false;
        }
    }

    [HarmonyPatch(typeof(Data.GameData.Generic), "SaveSkin")]
    public class Generic_SaveSkin_Patch
    {
        static bool Prefix()
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureGeneric();
            return false;
        }
    }

    [HarmonyPatch(typeof(Data.GameData.HardmodeProgress), "SaveAll")]
    public class HardmodeProgress_SaveAll_Patch
    {
        static bool Prefix()
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureHardmodeProgress();
            return false;
        }
    }

    // DemonCastleDefense saves — not AP-relevant, just block them
    [HarmonyPatch(typeof(Data.GameData.HardmodeProgress.DemonCastleDefense), "Save")]
    public class DemonCastleDefense_Save_Patch
    {
        static bool Prefix() => !SkulAPMod.APClient.IsConnected;
    }

    [HarmonyPatch(typeof(Data.GameData.HardmodeProgress.DemonCastleDefense), "SaveHiddneBossClearedLevel")]
    public class DemonCastleDefense_SaveHiddneBossClearedLevel_Patch
    {
        static bool Prefix() => !SkulAPMod.APClient.IsConnected;
    }

    [HarmonyPatch(typeof(Data.GameData.HardmodeProgress.DemonCastleDefense), "SaveHiddenBossDoor")]
    public class DemonCastleDefense_SaveHiddenBossDoor_Patch
    {
        static bool Prefix() => !SkulAPMod.APClient.IsConnected;
    }

    [HarmonyPatch(typeof(Data.GameData.HardmodeProgress.DemonCastleDefense), "SaveEmperorCutscneSkip")]
    public class DemonCastleDefense_SaveEmperorCutscneSkip_Patch
    {
        static bool Prefix() => !SkulAPMod.APClient.IsConnected;
    }

    [HarmonyPatch(typeof(Data.GameData.Progress), "SaveAll")]
    public class Progress_SaveAll_Patch
    {
        static bool Prefix()
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureProgress();
            APSaveManager.CaptureWitchMastery(Data.GameData.Progress.witch);
            return false;
        }
    }

    [HarmonyPatch(typeof(Data.GameData.Save), "SaveAll")]
    public class Save_SaveAll_Patch
    {
        static bool Prefix(Data.GameData.Save __instance)
        {
            if (!SkulAPMod.APClient.IsConnected) return true;
            APSaveManager.CaptureRunSave(__instance);
            return false;
        }
    }
}
