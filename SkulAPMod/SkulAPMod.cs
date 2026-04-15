using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
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
        private const string PluginVersion = "0.1.3";
        public const string Version = PluginVersion;

        public static SkulAPMod Instance { get; private set; }

        private Harmony harmony;
        public static Dictionary<string, object> sessionSlotData;
        public static ArchipelagoClient APClient { get; private set; }
        public static Sprite _archipelagoSprite;

        private static GameObject uiObject;
        private static bool uiCreated;
        private static readonly Queue<Action> _mainThreadQueue = new();

        // Location sends are queued and drip-sent with a delay so the AP server
        // doesn't drop rapid-fire checks.
        private static readonly Queue<long> _locationSendQueue = new();
        private const float LocationSendDelay = 0.5f;
        private bool _sendingLocations;

        public void Awake()
        {
            Instance = this;
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

        private FileWriter fileWriter;

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

            // Patch WitchBonus.ReviveOnce.Revive():
            //   - sets WitchLevelOverride so the heal/duration use the AP level (not _data.value=0)
            //   - sets PlayerJustRevived so DeathLink is not sent for this revive
            var reviveOnceType = AccessTools.TypeByName("Characters.WitchBonus+ReviveOnce");
            var reviveMethod = reviveOnceType != null ? AccessTools.Method(reviveOnceType, "Revive") : null;
            if (reviveMethod != null)
            {
                harmony.Patch(reviveMethod,
                    prefix:  new HarmonyMethod(typeof(ReviveOnce_Revive_Patch), nameof(ReviveOnce_Revive_Patch.Prefix)),
                    postfix: new HarmonyMethod(typeof(ReviveOnce_Revive_Patch), nameof(ReviveOnce_Revive_Patch.Postfix)));
            }
            else
            {
                Log.Warning("[AP] Could not find WitchBonus+ReviveOnce.Revive — Reassemble AP level and DeathLink guard inactive.");
            }
        }

        /// <summary>
        /// Enqueue a location check to be sent with a 0.5s gap between each send,
        /// so the AP server doesn't drop rapid-fire checks.
        /// </summary>
        public static void EnqueueLocationSend(long locationId)
        {
            if (ArchipelagoItemTracker.HasLocation(locationId)) return;
            lock (_locationSendQueue)
                _locationSendQueue.Enqueue(locationId);
            if (Instance != null && !Instance._sendingLocations)
                Instance.StartCoroutine(Instance.ProcessLocationSendQueue());
        }

        private IEnumerator ProcessLocationSendQueue()
        {
            _sendingLocations = true;
            while (true)
            {
                long locationId;
                lock (_locationSendQueue)
                {
                    if (_locationSendQueue.Count == 0) break;
                    locationId = _locationSendQueue.Dequeue();
                }
                if (!ArchipelagoItemTracker.HasLocation(locationId))
                    APClient.SendLocation(locationId);
                yield return new WaitForSeconds(LocationSendDelay);
            }
            _sendingLocations = false;
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
