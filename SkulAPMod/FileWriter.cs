using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Data;
using UnityEngine;

namespace SkulAPMod
{
    public class APSaveData
    {
        public class CurrencyEntry
        {
            public int Balance;
            public int Income;
            public int Outcome;
            public int TotalIncome;
        }

        public class WitchMasteryEntry
        {
            public int[] Skull = new int[4];
            public int[] Body = new int[4];
            public int[] Soul = new int[4];
        }

        public class GenericEntry
        {
            public bool NormalEnding;
            public int SkinIndex;
        }

        public class HardmodeProgressEntry
        {
            public int Level;
            public int ClearedLevel = -1;
            public int ClearedCount;
            public bool Enabled;
            public bool LowerMapParts;
            public bool EvilswordmanAlter;
            public Dictionary<string, bool> DarktechUnlocked = new Dictionary<string, bool>();
            public Dictionary<string, bool> DarktechActivated = new Dictionary<string, bool>();
        }

        public class ProgressEntry
        {
            public bool FoxRescued;
            public bool OgreRescued;
            public bool DruidRescued;
            public bool DeathknightRescued;
        }

        public class RunSaveEntry
        {
            public bool HasSave;
            public int Health;
            public int ChapterIndex = -1;
            public int StageIndex = -1;
            public int PathIndex = -1;
            public int NodeIndex = -1;
            public string CurrentWeapon = "";
            public string NextWeapon = "";
            public float CurrentWeaponStack;
            public float NextWeaponStack;
            public string CurrentWeaponSkill1 = "";
            public string CurrentWeaponSkill2 = "";
            public string NextWeaponSkill1 = "";
            public string NextWeaponSkill2 = "";
            public string Essence = "";
            public float EssenceStack;
            public string Fragment = "";
            public float FragmentStack;
            public string AbilityBuffs = "";
            public string[] Items = new string[] { "", "", "", "", "", "", "", "", "" };
            public float[] ItemStacks = new float[9];
            public int[] ItemKeywords1 = new int[9];
            public int[] ItemKeywords2 = new int[9];
            public string[] Upgrades = new string[] { "", "", "", "" };
            public int[] UpgradeLevels = new int[4];
            public float[] UpgradeStacks = new float[4];
        }

        public CurrencyEntry Gold = new CurrencyEntry();
        public CurrencyEntry DarkQuartz = new CurrencyEntry();
        public CurrencyEntry Bone = new CurrencyEntry();
        public CurrencyEntry HeartQuartz = new CurrencyEntry();
        public WitchMasteryEntry WitchMastery = new WitchMasteryEntry();
        public GenericEntry Generic = new GenericEntry();
        public HardmodeProgressEntry HardmodeProgress = new HardmodeProgressEntry();
        public ProgressEntry Progress = new ProgressEntry();
        public RunSaveEntry RunSave = new RunSaveEntry();
        // itemId → number of times received and granted; used to filter replayed AP items on reconnect
        public Dictionary<long, int> ReceivedItems = new Dictionary<long, int>();
        // location IDs the player has checked; re-sent to server on connect if not yet confirmed
        public HashSet<long> CheckedLocations = new HashSet<long>();
        // last known map position, persisted so it survives reconnects mid-run
        public int CurrentChapter  = -1;
        public int CurrentMapIndex = -1;
    }

    public static class APSaveManager
    {
        public static APSaveData SaveData { get; private set; } = new APSaveData();
        private static string _slotName;
        private static string _seed;
        
        private static string SavePath => (string.IsNullOrEmpty(_slotName) || string.IsNullOrEmpty(_seed))
            ? null
            : Path.Combine(Application.persistentDataPath, $"{_slotName}_ap.json");

        public static void Load(string slotName, string seed)
        {
            _slotName = slotName;
            _seed = seed;
            string path = SavePath;
            if (path != null && File.Exists(path))
            {
                try
                {
                    SaveData = JsonConvert.DeserializeObject<APSaveData>(File.ReadAllText(path)) ?? new APSaveData();
                    Log.Message($"[APSave] Loaded save for slot '{slotName}'");
                }
                catch (Exception ex)
                {
                    Log.Error($"[APSave] Failed to load: {ex.Message}");
                    SaveData = new APSaveData();
                }
            }
            else
            {
                SaveData = new APSaveData();
                Log.Message($"[APSave] No existing save for '{slotName}', starting fresh");
            }
            ApplyToGame();
        }

        public static void WriteToDisk()
        {
            if (string.IsNullOrEmpty(_slotName)) return;
            try
            {
                File.WriteAllText(SavePath, JsonConvert.SerializeObject(SaveData, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"[APSave] Write failed: {ex.Message}");
            }
        }

        // Save Captures

        public static void CaptureCurrency(GameData.Currency currency)
        {
            APSaveData.CurrencyEntry entry;
            if (currency == GameData.Currency.gold)           entry = SaveData.Gold;
            else if (currency == GameData.Currency.darkQuartz) entry = SaveData.DarkQuartz;
            else if (currency == GameData.Currency.bone)       entry = SaveData.Bone;
            else if (currency == GameData.Currency.heartQuartz) entry = SaveData.HeartQuartz;
            else return;

            entry.Balance     = currency.balance;
            entry.Income      = currency.income;
            entry.Outcome     = currency.outcome;
            entry.TotalIncome = currency.totalIncome;
            WriteToDisk();
        }

        public static void CaptureWitchMastery(GameData.Progress.WitchMastery witch)
        {
            var wm = SaveData.WitchMastery;
            for (int i = 0; i < 4; i++)
            {
                wm.Skull[i] = witch.skull[i].value;
                wm.Body[i] = witch.body[i].value;
                wm.Soul[i] = witch.soul[i].value;
            }
            WriteToDisk();
        }

        public static void CaptureGeneric()
        {
            SaveData.Generic.NormalEnding = GameData.Generic.normalEnding;
            SaveData.Generic.SkinIndex    = GameData.Generic.skinIndex;
            WriteToDisk();
        }

        public static void CaptureHardmodeProgress()
        {
            var hp = SaveData.HardmodeProgress;
            hp.Level           = GameData.HardmodeProgress.hardmodeLevel;
            hp.ClearedLevel    = GameData.HardmodeProgress.clearedLevel;
            hp.ClearedCount    = GameData.HardmodeProgress.clearedCount;
            hp.Enabled         = GameData.HardmodeProgress.hardmode;
            hp.LowerMapParts   = GameData.HardmodeProgress.lowerMapParts;
            hp.EvilswordmanAlter = GameData.HardmodeProgress.evilswordmanAlter;

            hp.DarktechUnlocked.Clear();
            hp.DarktechActivated.Clear();
            foreach (Hardmode.Darktech.DarktechData.Type t in Enum.GetValues(typeof(Hardmode.Darktech.DarktechData.Type)))
            {
                hp.DarktechUnlocked[t.ToString()]  = GameData.HardmodeProgress.unlocked.GetData(t);
                hp.DarktechActivated[t.ToString()] = GameData.HardmodeProgress.activated.GetData(t);
            }
            WriteToDisk();
        }

        public static void CaptureProgress()
        {
            SaveData.Progress.FoxRescued       = GameData.Progress.foxRescued;
            SaveData.Progress.OgreRescued      = GameData.Progress.ogreRescued;
            SaveData.Progress.DruidRescued     = GameData.Progress.druidRescued;
            SaveData.Progress.DeathknightRescued = GameData.Progress.deathknightRescued;
            WriteToDisk();
        }

        public static void CaptureRunSave(GameData.Save save)
        {
            if (save == null || !save.initilaized) return;
            var rs = SaveData.RunSave;
            rs.HasSave             = save.hasSave;
            rs.Health              = save.health;
            rs.ChapterIndex        = save.chapterIndex;
            rs.StageIndex          = save.stageIndex;
            rs.PathIndex           = save.pathIndex;
            rs.NodeIndex           = save.nodeIndex;
            rs.CurrentWeapon       = save.currentWeapon ?? "";
            rs.NextWeapon          = save.nextWeapon ?? "";
            rs.CurrentWeaponStack  = save.currentWeaponStack;
            rs.NextWeaponStack     = save.nextWeaponStack;
            rs.CurrentWeaponSkill1 = save.currentWeaponSkill1 ?? "";
            rs.CurrentWeaponSkill2 = save.currentWeaponSkill2 ?? "";
            rs.NextWeaponSkill1    = save.nextWeaponSkill1 ?? "";
            rs.NextWeaponSkill2    = save.nextWeaponSkill2 ?? "";
            rs.Essence             = save.essence ?? "";
            rs.EssenceStack        = save.essenceStack;
            rs.Fragment            = save.fragment ?? "";
            rs.FragmentStack       = save.fragmentStack;
            rs.AbilityBuffs        = save.abilityBuffs ?? "";
            for (int i = 0; i < 9; i++)
            {
                rs.Items[i]         = save.items[i] ?? "";
                rs.ItemStacks[i]    = save.itemStacks[i];
                rs.ItemKeywords1[i] = save.itemKeywords1[i];
                rs.ItemKeywords2[i] = save.itemKeywords2[i];
            }
            for (int i = 0; i < 4; i++)
            {
                rs.Upgrades[i]      = save.upgrades[i] ?? "";
                rs.UpgradeLevels[i] = save.upgradeLevels[i];
                rs.UpgradeStacks[i] = save.upgradeStacks[i];
            }
            WriteToDisk();
        }

        public static void CaptureStage(int chapter, int mapIndex)
        {
            SaveData.CurrentChapter  = chapter;
            SaveData.CurrentMapIndex = mapIndex;
            WriteToDisk();
        }

        // Apply methods

        public static void ApplyToGame()
        {
            ApplyCurrency(GameData.Currency.gold,          SaveData.Gold);
            ApplyCurrency(GameData.Currency.darkQuartz,    SaveData.DarkQuartz);
            ApplyCurrency(GameData.Currency.bone,          SaveData.Bone);
            ApplyCurrency(GameData.Currency.heartQuartz,   SaveData.HeartQuartz);
            ApplyWitchMastery();
            ApplyGeneric();
            ApplyHardmodeProgress();
            ApplyProgress();
            ApplyRunSave();
            if (SaveData.CurrentChapter >= 0)
            {
                Patches.StageTracker.Chapter  = SaveData.CurrentChapter;
                Patches.StageTracker.MapIndex = SaveData.CurrentMapIndex;
            }
        }

        private static void ApplyCurrency(GameData.Currency currency, APSaveData.CurrencyEntry entry)
        {
            if (currency == null || entry == null) return;
            currency.balance     = entry.Balance;
            currency.income      = entry.Income;
            currency.outcome     = entry.Outcome;
            currency.totalIncome = entry.TotalIncome;
        }

        private static void ApplyWitchMastery()
        {
            var witch = GameData.Progress.witch;
            if (witch == null) return;
            var wm = SaveData.WitchMastery;
            for (int i = 0; i < 4; i++)
            {
                witch.skull[i].value = wm.Skull[i];
                witch.body[i].value  = wm.Body[i];
                witch.soul[i].value  = wm.Soul[i];
            }
        }

        private static void ApplyGeneric()
        {
            GameData.Generic.normalEnding = SaveData.Generic.NormalEnding;
            GameData.Generic.skinIndex    = SaveData.Generic.SkinIndex;
        }

        private static void ApplyHardmodeProgress()
        {
            var hp = SaveData.HardmodeProgress;
            GameData.HardmodeProgress.hardmodeLevel    = hp.Level;
            GameData.HardmodeProgress.clearedLevel     = hp.ClearedLevel;
            GameData.HardmodeProgress.clearedCount     = hp.ClearedCount;
            GameData.HardmodeProgress.hardmode         = hp.Enabled;
            GameData.HardmodeProgress.lowerMapParts    = hp.LowerMapParts;
            GameData.HardmodeProgress.evilswordmanAlter = hp.EvilswordmanAlter;

            foreach (Hardmode.Darktech.DarktechData.Type t in Enum.GetValues(typeof(Hardmode.Darktech.DarktechData.Type)))
            {
                if (hp.DarktechUnlocked.TryGetValue(t.ToString(), out bool unlocked))
                    GameData.HardmodeProgress.unlocked.SetData(t, unlocked);
                if (hp.DarktechActivated.TryGetValue(t.ToString(), out bool activated))
                    GameData.HardmodeProgress.activated.SetData(t, activated);
            }
        }

        private static void ApplyProgress()
        {
            GameData.Progress.foxRescued        = SaveData.Progress.FoxRescued;
            GameData.Progress.ogreRescued       = SaveData.Progress.OgreRescued;
            GameData.Progress.druidRescued      = SaveData.Progress.DruidRescued;
            GameData.Progress.deathknightRescued = SaveData.Progress.DeathknightRescued;
        }

        private static void ApplyRunSave()
        {
            var save = GameData.Save.instance;
            if (save == null || !save.initilaized) return;
            var rs = SaveData.RunSave;
            if (!rs.HasSave) return;
            save.hasSave             = true;
            save.health              = rs.Health;
            save.chapterIndex        = rs.ChapterIndex;
            save.stageIndex          = rs.StageIndex;
            save.pathIndex           = rs.PathIndex;
            save.nodeIndex           = rs.NodeIndex;
            save.currentWeapon       = rs.CurrentWeapon ?? "";
            save.nextWeapon          = rs.NextWeapon ?? "";
            save.currentWeaponStack  = rs.CurrentWeaponStack;
            save.nextWeaponStack     = rs.NextWeaponStack;
            save.currentWeaponSkill1 = rs.CurrentWeaponSkill1 ?? "";
            save.currentWeaponSkill2 = rs.CurrentWeaponSkill2 ?? "";
            save.nextWeaponSkill1    = rs.NextWeaponSkill1 ?? "";
            save.nextWeaponSkill2    = rs.NextWeaponSkill2 ?? "";
            save.essence             = rs.Essence ?? "";
            save.essenceStack        = rs.EssenceStack;
            save.fragment            = rs.Fragment ?? "";
            save.fragmentStack       = rs.FragmentStack;
            save.abilityBuffs        = rs.AbilityBuffs ?? "";
            for (int i = 0; i < 9; i++)
            {
                save.items[i]         = rs.Items[i] ?? "";
                save.itemStacks[i]    = rs.ItemStacks[i];
                save.itemKeywords1[i] = rs.ItemKeywords1[i];
                save.itemKeywords2[i] = rs.ItemKeywords2[i];
            }
            for (int i = 0; i < 4; i++)
            {
                save.upgrades[i]      = rs.Upgrades[i] ?? "";
                save.upgradeLevels[i] = rs.UpgradeLevels[i];
                save.upgradeStacks[i] = rs.UpgradeStacks[i];
            }
        }
    }

    public class FileWriter : MonoBehaviour
    {
        private const string LastConnectionFileName = "last_connection.txt";

        public void WriteLastConnection(string host, int port, string slotName, string password)
        {
            try
            {
                string path = Application.persistentDataPath + "/" + LastConnectionFileName;
                var lines = new List<string> { host ?? "", port.ToString(), slotName ?? "", password ?? "" };
                File.WriteAllLines(path, lines);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write last connection: {ex.Message}");
            }
        }

        public static (string host, string port, string slotName, string password) ReadLastConnection()
        {
            try
            {
                string path = Application.persistentDataPath + "/" + LastConnectionFileName;
                if (!File.Exists(path)) return (null, null, null, null);
                string[] lines = File.ReadAllLines(path);
                return (
                    lines.Length > 0 ? lines[0] : null,
                    lines.Length > 1 ? lines[1] : null,
                    lines.Length > 2 ? lines[2] : null,
                    lines.Length > 3 ? lines[3] : null
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to read last connection: {ex.Message}");
                return (null, null, null, null);
            }
        }
    }
}
