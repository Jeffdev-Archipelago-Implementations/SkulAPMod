using HarmonyLib;
using Level;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Chapter), "LoadStage")]
    public class Chapter_LoadStage_Patch
    {
        static void Postfix(Chapter __instance)
        {
            int newChapter = (int)__instance.type - 3; // Chapter1=0, Chapter2=1, Chapter3=2, Chapter4=3
            Log.Info($"{__instance.type}, {__instance.stageName} {__instance.chapterName}");
            if (newChapter != StageTracker.Chapter)
            {
                StageTracker.Chapter  = newChapter;
                StageTracker.MapIndex = -1; // Map.Awake will increment to 0 when the first map loads
            }
        }
    }

    [HarmonyPatch(typeof(Map), "Awake")]
    public class Map_Awake_Patch
    {
        static void Postfix(Map __instance)
        {
            Log.Info($"MAP TYPE: {__instance.type}");
            if (__instance.type != Map.Type.Normal) return;
            StageTracker.MapIndex++;
            Log.Info($"Map loaded: Chapter={StageTracker.Chapter}, MapIndex={StageTracker.MapIndex}");
            APSaveManager.CaptureStage(StageTracker.Chapter, StageTracker.MapIndex);
        }
    }

    [HarmonyPatch(typeof(LevelManager), "InvokeOnActivateMapReward")]
    public class LevelManager_InvokeOnActivateMapReward_Patch
    {
        static void Postfix()
        {
            if (Map.Instance == null || Map.Instance.type != Map.Type.Normal) return;
            if (Map.Instance.waveContainer == null || Map.Instance.waveContainer.enemyWaves.Length == 0) return;

            Log.Info($"Wave clear: Chapter={StageTracker.Chapter}, MapIndex={StageTracker.MapIndex}");
            if (!SkulAPMod.APClient.IsConnected) return;

            int chapter = StageTracker.Chapter;
            if (chapter < 0 || chapter >= ArchipelagoConstants.ChapterRoomBaseLocations.Length) return;

            long locationId = ArchipelagoConstants.ChapterRoomBaseLocations[chapter] + StageTracker.MapIndex;
            if (!ArchipelagoItemTracker.HasLocation(locationId))
                SkulAPMod.APClient.SendLocation(locationId);
        }
    }
}
