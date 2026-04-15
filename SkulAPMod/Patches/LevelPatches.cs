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
                        3 => ArchipelagoConstants.FortressMiniBossDefeated,
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

    public static class ReviveDetector
    {
        public static bool PlayerJustRevived;
        public static void Prefix() => PlayerJustRevived = true;
    }

    // Send DeathLink when the player actually dies (health.dead == true).
    [HarmonyPatch(typeof(LevelManager), "ResetGame", new System.Type[0])]
    public class LevelManager_ResetGame_DeathLink_Patch
    {
        static void Prefix(LevelManager __instance)
        {
            if (ReviveDetector.PlayerJustRevived)
            {
                ReviveDetector.PlayerJustRevived = false;
                return;
            }
            if (!SkulAPMod.APClient.IsConnected) return;
            if (__instance.player?.health.dead == true)
                SkulAPMod.APClient.SendDeathLink();
        }
    }

    // Re-grant filler gold and bone items received from AP after each death/restart.
    [HarmonyPatch(typeof(LevelManager), "ResetGame", new System.Type[0])]
    public class LevelManager_ResetGame_RegrantFiller_Patch
    {
        static void Postfix()
        {
            if (!SkulAPMod.APClient.IsConnected) return;

            int goldCount = ArchipelagoItemTracker.AmountOfItem(ArchipelagoConstants.GoldItem);
            for (int i = 0; i < goldCount; i++)
                ArchipelagoItemHandler.GrantItem(ArchipelagoConstants.GoldItem);

            int boneCount = ArchipelagoItemTracker.AmountOfItem(ArchipelagoConstants.BoneItem);
            for (int i = 0; i < boneCount; i++)
                ArchipelagoItemHandler.GrantItem(ArchipelagoConstants.BoneItem);

            // Reset reassembleUsed so Reassemble is available again next run.
            // WitchBonus.Apply re-creates the bonus objects on run start, but
            // GameData.Progress.reassembleUsed persists and must be cleared here.
            if (ArchipelagoItemTracker.AmountOfItem(ArchipelagoConstants.Reassemble) > 0)
                Data.GameData.Progress.reassembleUsed = false;
        }
    }

    // Block chapter advancement if the player hasn't received enough ProgressiveStage items.
    // Chapter1=index 0 (free), Chapter2=needs 1, Chapter3=needs 2, Chapter4=needs 3.
    [HarmonyPatch(typeof(LevelManager), "ChangeChapter")]
    public class LevelManager_ChangeChapter_Patch
    {
        static bool Prefix(LevelManager __instance, Chapter.Type __0)
        {
            if (SkulAPMod.APClient == null || !SkulAPMod.APClient.IsConnected) return true;

            int chapterIndex = (int)__0 - 3; // Chapter1=0, Chapter2=1, Chapter3=2, Chapter4=3
            if (chapterIndex is <= 0 or > 3) return true; // Chapter1 is free, >3 is not a main chapter

            int allowed = ArchipelagoItemTracker.AmountOfItem(ArchipelagoConstants.ProgressiveStage);
            if (chapterIndex <= allowed) return true;

            Log.Message($"[AP] Chapter {chapterIndex} gated - have {allowed} Progressive Stage item(s), need {chapterIndex}.");
            __instance.ResetGame();
            return false;
        }
    }

    [HarmonyPatch(typeof(Altar), "Destroy")]
    public class Altar_Destroy_Patch
    {
        static void Postfix()
        {
            if (!SkulAPMod.APClient.IsConnected) return;

            int chapter = StageTracker.Chapter;
            if (chapter < 0 || chapter >= ArchipelagoConstants.ChapterShrineBaseLocations.Length) return;

            int cap = ArchipelagoItemHandler.ShrineChecksCount;
            long baseId = ArchipelagoConstants.ChapterShrineBaseLocations[chapter];
            int sent = 0;
            for (int i = 0; i < cap; i++)
                if (ArchipelagoItemTracker.HasLocation(baseId + i)) sent++;

            if (sent >= cap) return;

            Log.Info($"Shrine destroyed: Chapter={chapter}, ShrinesSentSoFar={sent}");
            SkulAPMod.APClient.SendLocation(baseId + sent);
        }
    }
}
