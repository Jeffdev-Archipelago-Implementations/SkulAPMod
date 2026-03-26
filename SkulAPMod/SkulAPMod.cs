using BepInEx;
using BepInEx.Configuration;
using SkulAPMod.Helpers;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Characters;
using Level;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

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
        private const string PluginVersion = "0.1.0";

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
            InitializeAssemblyResolution();
            ApplyPatches();
            InitializeComponents();

            Log.Info($"{PluginName} loaded successfully!");
        }

        private void InitializeLogging()
        {
            Log.Init(Logger);
        }

        private void InitializeAssemblyResolution()
        {
            ForceLoadNewtonsoftJson();
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            CheckSystemNumericsAvailability();
        }

        private void ForceLoadNewtonsoftJson()
        {
            try
            {
                var jsonType = typeof(Newtonsoft.Json.JsonConvert);
                Log.Message($"Loaded Newtonsoft.Json version: {jsonType.Assembly.GetName().Version}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to preload Newtonsoft.Json: {ex.Message}");
            }
        }

        private void CheckSystemNumericsAvailability()
        {
            try
            {
                var bigIntType = Type.GetType("System.Numerics.BigInteger, System.Numerics");
                Logger.LogInfo($"BigInteger available: {bigIntType != null}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"BigInteger check failed: {ex.Message}");
            }
        }

        private void InitializeComponents()
        {
            fileWriter = gameObject.AddComponent<FileWriter>();
            _archipelagoSprite = LoadEmbeddedSprite("SkulAPMod.Resources.Sprites.archipelago_logo.png");

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
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
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
        }

        /// <summary>
        /// Queues an action to be run on the Unity main thread on the next Update().
        /// Safe to call from background threads (AP socket callbacks).
        /// </summary>
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
                action();
            }

            if (APClient == null || !APClient.HasPendingNotifications()) return;
            string notification = APClient.DequeuePendingNotification();
            // Add notification things here
        }

        private void OnArchipelagoConnected()
        {
            Log.Message("Connected to Archipelago - loading items");
            uiObject.GetComponent<ConnectionUI>().ToggleUI();
            APSessionManager.OnConnected();
        }

        private void OnArchipelagoDisconnected()
        {
            Log.Message("Disconnected from Archipelago");
            ArchipelagoItemTracker.Clear();
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            if (assemblyName.Name != "Newtonsoft.Json") return null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "Newtonsoft.Json") continue;
                Log.Message($"Resolved Newtonsoft.Json to version {assembly.GetName().Version}");
                return assembly;
            }

            return null;
        }

        public static void CreateUI()
        {
            if (uiCreated) return;

            Log.Message("Creating Archipelago UI...");
            uiObject = new GameObject("ArchipelagoUI");
            DontDestroyOnLoad(uiObject);

            var ui = uiObject.AddComponent<ConnectionUI>();
            ui.Initialize(APClient);

            // notificationDisplay = uiObject.AddComponent<NotificationDisplay>();
            // notificationDisplay.Initialize();

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

namespace SkulAPMod.Patches
{
    [HarmonyPatch(typeof(Character), "DoMotion")]
    public class Character_DoMotion_Patch
    {
        static void Prefix()
        {
            
        }
    }

    [HarmonyPatch(typeof(EnemyWave), "Clear")]
    public class EnemyWave_Clear_Patch
    {
        static void Prefix()
        {
            // This is where we should send locations for clearing waves
            Scenes.GameBase.instance.uiManager.unlockNotice.Show(SkulAPMod._archipelagoSprite, "Sent [item] to [player]!");
        }
    }
    
    [HarmonyPatch(typeof(Data.GameData.Currency), "Earn", typeof(int))]
    public class Currency_Earn_Patch
    {
        static void Prefix(ref int amount)
        {
            amount = 0;
        }
    }
}