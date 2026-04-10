using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Services;
using UnityEngine;
using SkulAPMod.Patches;

// #if DEBUG
// using UnityHotReloadNS;
// #endif

namespace SkulAPMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class SkulAPMod : BaseUnityPlugin
    {
        private const string PluginGuid = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "Jeffdev";
        private const string PluginName = "SkulAPMod";
        private const string PluginVersion = "0.1.1";

        private Harmony harmony;
        public static Dictionary<string, object> sessionSlotData;
        public static ArchipelagoClient APClient { get; private set; }
        public static Sprite _archipelagoSprite;

        private static GameObject uiObject;
        private static bool uiCreated;
        private FileWriter fileWriter;
        private static readonly Queue<Action> _mainThreadQueue = new();
        
        public void Awake()
        {
            InitializeLogging();
            ApplyPatches();
            InitializeComponents();

            Log.Info($"{PluginName} loaded successfully");
        }

        private void InitializeLogging()
        {
            Log.Init(Logger);
        }
        
        private void InitializeComponents()
        {
            fileWriter = gameObject.AddComponent<FileWriter>();
            _archipelagoSprite = LoadEmbeddedSprite("SkulAPMod.Resources.Sprites.skul_ap.png");

            APClient = new ArchipelagoClient();
            APClient.OnConnected += OnArchipelagoConnected;
            APClient.OnDisconnected += OnArchipelagoDisconnected;
        }

        private static Sprite LoadEmbeddedSprite(string resourceName)
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Log.Error($"Embedded resource not found: {resourceName}");
                    return null;
                }
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.FullRect);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load embedded sprite '{resourceName}': {ex.Message}");
                return null;
            }
        }

        private void ApplyPatches()
        {
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            var reviveType = AccessTools.TypeByName("ReviveComponent");
            var reviveOnce = reviveType != null ? AccessTools.Method(reviveType, "ReviveOnce") : null;
            if (reviveOnce != null)
                harmony.Patch(reviveOnce, prefix: new HarmonyMethod(typeof(Patches.ReviveDetector), nameof(Patches.ReviveDetector.Prefix)));
            else
                Log.Warning("[AP] Could not find ReviveComponent.ReviveOnce — revive DeathLink guard inactive.");
        }
        
        public static void QueueMainThreadAction(Action action)
        {
            lock (_mainThreadQueue) _mainThreadQueue.Enqueue(action);
        }

        public void Update()
        {
            while (true)
            {
                Action action;
                lock (_mainThreadQueue)
                {
                    if (_mainThreadQueue.Count == 0) break;
                    action = _mainThreadQueue.Dequeue();
                }
                try { action(); }
                catch (Exception ex) { Log.Error($"[MainThread] Action threw: {ex}"); }
            }
        }

        private void OnArchipelagoConnected()
        {
            Log.Message("Connected to Archipelago - loading items");
            uiObject.GetComponent<ConnectionUI>().ToggleUI();
            APSessionManager.OnConnected();
            // Pre-load Witch hints
            PreloadWitchScoutCache();
        }

        private static void PreloadWitchScoutCache()
        {
            // Accounts for all witch locations
            int count = (int)(ArchipelagoConstants.AncientAlchemy2 - ArchipelagoConstants.MarrowTransplant1 + 1);
            var ids = new long[count];
            for (int i = 0; i < count; i++)
                ids[i] = ArchipelagoConstants.MarrowTransplant1 + i;

            var scouted = APClient.BulkScoutLocations(ids);
            WitchOption_UpdateTexts_Patch.PreloadCache(scouted);
            Log.Message($"Pre-scouted {scouted.Count} witch bonus locations");
        }

        private void OnArchipelagoDisconnected()
        {
            Log.Message("Disconnected from Archipelago — returning to title screen");
            QueueMainThreadAction(() =>
            {
                try { Singletons.Singleton<Service>.Instance.ResetGameScene(); }
                catch (Exception ex) { Log.Error($"Failed to return to title screen: {ex.Message}"); }
            });
        }

        public static void CreateUI()
        {
            if (uiCreated) return;

            Log.Message("Creating Archipelago UI...");
            uiObject = new GameObject("ArchipelagoUI");
            DontDestroyOnLoad(uiObject);

            var ui = uiObject.AddComponent<ConnectionUI>();
            ui.Initialize(APClient);

            uiObject.AddComponent<MessageLogUI>();

            uiCreated = true;
        }

        public void OnDestroy()
        {
            APClient?.Disconnect();
            if (uiObject != null)
            {
                Destroy(uiObject);
            }
            harmony?.UnpatchSelf();
        }
    }
}

