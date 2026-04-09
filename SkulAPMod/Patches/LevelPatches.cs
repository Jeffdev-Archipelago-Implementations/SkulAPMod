using Characters;
using Characters.Player;
using HarmonyLib;
using Level;
using Level.Altars;
using SkulAPMod.Helpers;

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Chapter), "LoadStage")]
    public class Chapter_LoadStage_Patch
    {
        static void Postfix(Chapter __instance)
        {
            int newChapter = (int)__instance.type - 3; // Chapter1=0, Chapter2=1, Chapter3=2, Chapter4=3
            Log.Info($"{__instance.type}, {__instance.stageName} {__instance.chapterName}");
            StageTracker.Chapter = newChapter;
        }
    }

    [HarmonyPatch(typeof(PlayerKillCounter), "CountKill")]
    public class PlayerKillCounter_CountKill_Patch
    {
        static void Postfix(ITarget target)
        {
            if (!SkulAPMod.APClient.IsConnected) return;
            if ((object)target?.character == null) return;
            if (target.character.type != Character.Type.Boss) return;

            long? locationId = target.character.key switch
            {
                Key.Yggdrasil    => ArchipelagoConstants.ForestBossDefeated,
                Key.AwakenLeiana => ArchipelagoConstants.GrandHallBossDefeated,
                Key.Chimera      => ArchipelagoConstants.BlackLabBossDefeated,
                Key.Pope         => ArchipelagoConstants.FortressBossDefeated,
                _                => null
            };

            if (locationId.HasValue)
            {
                Log.Info($"Boss killed: key={target.character.key}, locationId={locationId}");
                if (!ArchipelagoItemTracker.HasLocation(locationId.Value))
                    SkulAPMod.APClient.SendLocation(locationId.Value);
            }

            if (target.character.key == Key.FirstHero3)
                ArchipelagoGoalManager.CheckAndCompleteGoal();
        }
    }

    [HarmonyPatch(typeof(LevelManager), "InvokeOnActivateMapReward")]
    public class LevelManager_InvokeOnActivateMapReward_Patch
    {
        static void Postfix()
        {
            if (Map.Instance == null) return;
            if (!SkulAPMod.APClient.IsConnected) return;

            int chapter = StageTracker.Chapter;
            if (chapter < 0) return;

            switch (Map.Instance.type)
            {
                case Map.Type.Normal:
                case Map.Type.Special:
                    if (Map.Instance.waveContainer == null || Map.Instance.waveContainer.enemyWaves.Length == 0) return;
                    if (chapter >= ArchipelagoConstants.ChapterRoomBaseLocations.Length) return;
                    int sent = GetRoomChecksSent(chapter);
                    if (sent >= ArchipelagoItemHandler.ReqRoomCount) return;
                    Log.Info($"Wave clear: Chapter={chapter}, RoomsSentSoFar={sent}");
                    SendIfNew(ArchipelagoConstants.ChapterRoomBaseLocations[chapter] + sent);
                    break;

                case Map.Type.Manual:
                    if (Map.Instance.mapReward.type != MapReward.Type.Adventurer) break;
                    long? locationId = chapter switch
                    {
                        0 => ArchipelagoConstants.ForestMiniBossDefeated,
                        1 => ArchipelagoConstants.GrandHallMiniBossDefeated,
                        2 => ArchipelagoConstants.BlackLabMiniBossDefeated,
                        _ => null
                    };
                    Log.Info($"Mini-boss defeated: Chapter={chapter}, LocationId={locationId}");
                    if (locationId.HasValue) SendIfNew(locationId.Value);
                    break;
            }
        }

        private static int GetRoomChecksSent(int chapter)
        {
            long baseId = ArchipelagoConstants.ChapterRoomBaseLocations[chapter];
            int max = ArchipelagoItemHandler.ReqRoomCount;
            int count = 0;
            for (int i = 0; i < max; i++)
                if (ArchipelagoItemTracker.HasLocation(baseId + i)) count++;
            return count;
        }

        private static void SendIfNew(long locationId)
        {
            if (!ArchipelagoItemTracker.HasLocation(locationId))
                SkulAPMod.APClient.SendLocation(locationId);
        }
    }

    [HarmonyPatch(typeof(Altar), "Destroy")]
    public class Altar_Destroy_Patch
    {
        private const int ShrineCap = 5;

        static void Postfix()
        {
            if (!SkulAPMod.APClient.IsConnected) return;

            int chapter = StageTracker.Chapter;
            if (chapter < 0 || chapter >= ArchipelagoConstants.ChapterShrineBaseLocations.Length) return;

            long baseId = ArchipelagoConstants.ChapterShrineBaseLocations[chapter];
            int sent = 0;
            for (int i = 0; i < ShrineCap; i++)
                if (ArchipelagoItemTracker.HasLocation(baseId + i)) sent++;

            if (sent >= ShrineCap) return;

            Log.Info($"Shrine destroyed: Chapter={chapter}, ShrinesSentSoFar={sent}");
            SkulAPMod.APClient.SendLocation(baseId + sent);
        }
    }
}
