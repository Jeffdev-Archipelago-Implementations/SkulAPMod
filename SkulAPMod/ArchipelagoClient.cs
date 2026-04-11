using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;
using Singletons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services;
using UnityEngine;

// ReSharper disable All

namespace SkulAPMod
{
    public class ArchipelagoClient
    {
        private ArchipelagoSession session { get; set; }
        private int _itemsToSkip;
        private DeathLinkService _deathLinkService;
        private bool _receivingDeathLink;

        public bool DeathLinkEnabled { get; private set; }

        public bool IsConnected => session?.Socket.Connected ?? false;

        public string SlotName { get; private set; }
        public string Seed { get; private set; }
        public string ServerVersion { get; private set; }
        public string ApworldVersion { get; private set; }

        public event Action OnConnected;
        public event Action<string> OnConnectionFailed;
        public event Action OnDisconnected;
        public event Action OnRestartRequired;

        public void Connect(string hostname, int port, string slotName, string password = "")
        {
            try
            {
                Log.Message($"Attempting to connect to {hostname}:{port} as {slotName}");

                SlotName = slotName;
                session = ArchipelagoSessionFactory.CreateSession(hostname, port);
                session.Socket.ErrorReceived += OnError;
                session.Socket.SocketClosed += OnSocketClosed;

                LoginResult result = session.TryConnectAndLogin(
                    "Skul: The Hero Slayer",
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(0, 6, 6),
                    password: string.IsNullOrEmpty(password) ? null : password
                );

                if (result.Successful)
                {
                    LoginSuccessful loginSuccess = (LoginSuccessful)result;
                    SkulAPMod.sessionSlotData = loginSuccess.SlotData;
                    Log.Message($"Connected successfully! Slot: {loginSuccess.Slot}");
                    foreach (var data in loginSuccess.SlotData)
                        Log.Message($"Slot Data: {data.Key} = {data.Value}");

                    SlotName = slotName;
                    Seed = session.RoomState.Seed;

                    if (Patches.Application_persistentDataPath_Patch.SetSlot(SlotName, Seed))
                    {
                        Log.Message("[AP] New slot detected — please relaunch the game to load save data.");
                        OnRestartRequired?.Invoke();
                        return;
                    }

                    APSaveManager.Load(SlotName, Seed);
                    ArchipelagoItemHandler.LoadOptions();
                    InitDeathLink();
                    session.Items.ItemReceived += OnItemReceived;
                    session.MessageLog.OnMessageReceived += OnMessageReceived;
                    _itemsToSkip = ArchipelagoItemTracker.LoadFromServer();

                    OnConnected?.Invoke();
                    ArchipelagoItemTracker.LogAllReceivedItems();
                    ArchipelagoItemTracker.LogAllCheckedLocations();
                }
                else
                {
                    LoginFailure failure = (LoginFailure)result;
                    string errorMsg = string.Join(", ", failure.Errors);
                    Log.Error($"Connection failed: {errorMsg}");
                    OnConnectionFailed?.Invoke(errorMsg);
                    session = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Connection exception: {ex}");
                OnConnectionFailed?.Invoke(ex.Message);
                session = null;
            }
        }

        public ArchipelagoSession GetSession() => session;

        public void Disconnect()
        {
            if (session == null) return;
            session.Socket.DisconnectAsync();
            session = null;
            _deathLinkService = null;
            Log.Message("Disconnected from Archipelago");
        }

        private void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (_itemsToSkip > 0)
            {
                _itemsToSkip--;
                helper.DequeueItem();
            }

            var item = helper.PeekItem();
            string itemName = session.Items.GetItemName(item.ItemId);
            string playerName = session.Players.GetPlayerName(item.Player);
            ItemFlags flags = item.Flags;
            long itemId = item.ItemId;

            Log.Message($"Item Received: {itemName} from {playerName}");

            string color = Utils.GetItemColor(flags);
            string notification = $"Received <color=#{color}>{itemName}</color>\nfrom {playerName}!";
            SkulAPMod.QueueMainThreadAction(() =>
            {
                ArchipelagoItemTracker.AddReceivedItem(itemId);
                ArchipelagoItemHandler.GrantItem(itemId);
                Scenes.GameBase.instance?.uiManager?.unlockNotice?.Show(SkulAPMod._archipelagoSprite, notification);
            });

            helper.DequeueItem();
        }

        private void OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var part in message.Parts)
            {
                var c = part.Color;
                string hex = $"{c.R:X2}{c.G:X2}{c.B:X2}";
                sb.Append($"<color=#{hex}>{part.Text}</color>");
            }
            string line = sb.ToString();
            SkulAPMod.QueueMainThreadAction(() => MessageLogUI.AddMessage(line));
        }

        private void OnError(Exception ex, string message)
        {
            Log.Error($"Socket error: {message} - {ex.Message}");
        }

        private void OnSocketClosed(string reason)
        {
            Log.Warning($"Socket closed: {reason}");
            OnDisconnected?.Invoke();
        }

        public void SendLocation(long locationId)
        {
            if (!IsConnected) return;
            ArchipelagoItemTracker.AddCheckedLocation(locationId);
            session.Locations.CompleteLocationChecks(locationId);
            Log.Message($"Sent location check: {locationId}");
            ShowLocationSentFloatingText(locationId);
        }

        private void ShowLocationSentFloatingText(long locationId)
        {
            var capturedSession = session;
            Task.Run(() =>
            {
                try
                {
                    var info = capturedSession?.Locations.ScoutLocationsAsync(false, locationId)?.Result?.Values.FirstOrDefault();
                    string itemName = info?.ItemName ?? "Item";
                    string playerName = info != null ? capturedSession.Players.GetPlayerName(info.Player) : "";
                    string color = info != null ? Utils.GetItemColor(info.Flags) : "ffffff";
                    string displayText = $"Sent: {itemName} to {info.Player.Name}";

                    SkulAPMod.QueueMainThreadAction(() =>
                    {
                        try
                        {
                            var spawner = Singleton<Service>.Instance?.floatingTextSpawner;
                            var player = Singleton<Service>.Instance?.levelManager?.player;
                            if (spawner == null || player == null) return;
                            spawner.SpawnBuff(displayText, player.transform.position, color);
                        }
                        catch (Exception ex) { Log.Error($"[AP] Floating Text spawn error: {ex.Message}"); }
                    });
                }
                catch (Exception ex) { Log.Error($"[AP] Location scout error: {ex.Message}"); }
            });
        }

        private void InitDeathLink()
        {
            _deathLinkService = session.CreateDeathLinkService();
            _deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;

            string val = null;
            try { val = GetSlotDataValue(ArchipelagoConstants.DeathLinkOption); } catch { }
            if (val == "1" || val == "true")
            {
                DeathLinkEnabled = true;
                _deathLinkService.EnableDeathLink();
                Log.Message("[AP] DeathLink enabled");
            }
        }

        public void SetDeathLinkEnabled(bool enabled)
        {
            if (_deathLinkService == null) return;
            DeathLinkEnabled = enabled;
            if (enabled)
                _deathLinkService.EnableDeathLink();
            else
                _deathLinkService.DisableDeathLink();
            Log.Message($"[AP] DeathLink toggled {(enabled ? "on" : "off")}");
        }

        public void SendDeathLink()
        {
            if (_deathLinkService == null || !DeathLinkEnabled) return;
            if (_receivingDeathLink)
            {
                _receivingDeathLink = false;
                return;
            }
            _deathLinkService.SendDeathLink(new DeathLink(SlotName, $"{SlotName} was defeated in Skul: The Hero Slayer"));
            Log.Message("[AP] Sent DeathLink");
        }

        private void OnDeathLinkReceived(DeathLink deathLink)
        {
            string source = deathLink.Source ?? "Unknown";
            string cause = deathLink.Cause ?? $"{source} died";
            Log.Message($"[AP] DeathLink received from {source}: {cause}");

            _receivingDeathLink = true;
            SkulAPMod.QueueMainThreadAction(() =>
            {
                Singleton<Service>.Instance.levelManager.player?.health.Kill();
            });
        }

        public string GetSlotDataValue(string key)
        {
            if (session == null || SkulAPMod.sessionSlotData == null) return null;

            if (SkulAPMod.sessionSlotData.TryGetValue("options", out object optionsObj))
            {
                Dictionary<string, object> options = optionsObj switch
                {
                    Dictionary<string, object> d => d,
                    JObject j => j.ToObject<Dictionary<string, object>>(),
                    _ => null
                };
                if (options != null && options.TryGetValue(key, out object optValue))
                    return optValue.ToString();
            }

            if (SkulAPMod.sessionSlotData.TryGetValue(key, out object value))
                return value.ToString();

            throw new SlotDataException($"Invalid option requested from apworld: {key}. Did you generate on the wrong version?");
        }

        public string GetSeed() => session?.RoomState?.Seed;
        
        public ScoutedItemInfo TryScoutLocation(long locationId, bool createHint = false)
        {
            return session.Locations.ScoutLocationsAsync(createHint, locationId)?.Result?.Values.First();
        }

        public Dictionary<long, ScoutedItemInfo> BulkScoutLocations(long[] locationIds)
        {
            if (!IsConnected || locationIds.Length == 0) return new Dictionary<long, ScoutedItemInfo>();
            return session.Locations.ScoutLocationsAsync(false, locationIds)?.Result
                   ?? new Dictionary<long, ScoutedItemInfo>();
        }
    }
}
