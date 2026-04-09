using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkulAPMod
{
    public class APSaveData
    {
        // itemId → number of times received and granted; used to filter replayed AP items on reconnect
        public Dictionary<long, int> ReceivedItems = new Dictionary<long, int>();
        // location IDs the player has checked; re-sent to server on connect if not yet confirmed
        public HashSet<long> CheckedLocations = new HashSet<long>();
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
                    Log.Message($"[APSave] Loaded AP save for slot '{slotName}'");
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
                Log.Message($"[APSave] No existing AP save for '{slotName}', starting fresh");
            }
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
