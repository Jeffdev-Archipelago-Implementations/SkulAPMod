using HarmonyLib;
using Level;
using Services;
using Singletons;

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

    [HarmonyPatch(typeof(Gate), "OnActivate")]
    public class Gate_OnActivate_Patch
    {
        static void Postfix(Gate.Type ____type)
        {
            if (!SkulAPMod.APClient.IsConnected) return;
            Log.Info($"GATE TYPE: {____type}");
            if (____type != Gate.Type.Boss && ____type != Gate.Type.Adventurer) return;

            var chapter = Singleton<Service>.Instance.levelManager.currentChapter;
            if (chapter == null) return;

            int chapterIndex = (int)chapter.type - 3; // Chapter1=0, Chapter2=1, Chapter3=2, Chapter4=3
            if (chapterIndex is < 0 or > 3) return;

            long? locationId = ____type == Gate.Type.Boss
                ? chapterIndex switch
                {
                    0 => ArchipelagoConstants.ForestBossDefeated,
                    1 => ArchipelagoConstants.GrandHallBossDefeated,
                    2 => ArchipelagoConstants.BlackLabBossDefeated,
                    3 => ArchipelagoConstants.FortressBossDefeated,
                    _ => (long?)null
                }
                : chapterIndex switch // Adventurer = mini-boss (Fortress has none)
                {
                    0 => ArchipelagoConstants.ForestMiniBossDefeated,
                    1 => ArchipelagoConstants.GrandHallMiniBossDefeated,
                    2 => ArchipelagoConstants.BlackLabMiniBossDefeated,
                    _ => (long?)null
                };

            if (locationId.HasValue && !ArchipelagoItemTracker.HasLocation(locationId.Value))
                SkulAPMod.APClient.SendLocation(locationId.Value);
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
