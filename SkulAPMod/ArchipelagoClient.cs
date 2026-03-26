using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Helpers;

namespace SkulAPMod
{
    public class ArchipelagoClient
    {
        private ArchipelagoSession session;
        private readonly Queue<string> pendingNotifications = new Queue<string>();

#if DEBUG
        private bool _mockConnected;
        private string _mockSeed;
        public bool IsMockConnected => _mockConnected;
#endif

        public bool IsConnected =>
#if DEBUG
            _mockConnected ||
#endif
            (session?.Socket.Connected ?? false);

        public string SlotName { get; private set; }

        public event Action OnConnected;
        public event Action<string> OnConnectionFailed;
        public event Action OnDisconnected;

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
                    {
                        Log.Message($"Slot Data: {data.Key} = {data.Value}");
                    }

                    // Subscribe to message received
                    session.MessageLog.OnMessageReceived += OnMessageReceived;

                    // Load items we already have
                    ArchipelagoItemTracker.LoadFromServer();

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

        public ArchipelagoSession GetSession()
        {
            return session;
        }

        public void Disconnect()
        {
            if (session == null) return;
            session.Socket.DisconnectAsync();
            session = null;
            Log.Message("Disconnected from Archipelago");
        }

        private void OnMessageReceived(LogMessage message)
        {
            pendingNotifications.Enqueue(message.ToString());
        }

        private void OnItemReceived(ReceivedItemsHelper helper)
        {
            var item = helper.PeekItem();

            string itemName = session.Items.GetItemName(item.ItemId);
            string playerName = session.Players.GetPlayerName(item.Player);

            Log.Message($"Item Received: {itemName} from {playerName}");

            ArchipelagoItemTracker.AddReceivedItem(item.ItemId);
            
            Scenes.GameBase.instance.uiManager.unlockNotice.Show(SkulAPMod._archipelagoSprite, $"Received {itemName} from {playerName}!");

            helper.DequeueItem();
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
        }

        public string GetSlotDataValue(string key)
        {
            if (session == null || SkulAPMod.sessionSlotData == null) return null;

            // Options are nested under "options" key in the new apworld format
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

            // Fall back to top-level
            if (SkulAPMod.sessionSlotData.TryGetValue(key, out object value))
                return value.ToString();

            throw new SlotDataException($"Invalid option requested from apworld: {key}. Did you generate on the wrong version?");
        }

        public string GetSeed()
        {
#if DEBUG
            if (_mockConnected) return _mockSeed;
#endif
            return session?.RoomState?.Seed;
        }

#if DEBUG
        public void MockConnect(string slotName, string seed = "mock_seed_1")
        {
            if (_mockConnected) return;
            SlotName = slotName;
            _mockSeed = seed;
            _mockConnected = true;
            Log.Message($"[MockAP] Mock-connected as slot '{slotName}' seed '{seed}'");
            ArchipelagoItemTracker.LoadFromServer();
            OnConnected?.Invoke();
        }

        public void MockDisconnect()
        {
            if (!_mockConnected) return;
            _mockConnected = false;
            Log.Message("[MockAP] Mock-disconnected");
            OnDisconnected?.Invoke();
        }

        /// <summary>Simulates receiving an item from the AP server.</summary>
        public void MockReceiveItem(long itemId, string itemName = null)
        {
            itemName ??= $"Item#{itemId}";
            ArchipelagoItemTracker.AddReceivedItem(itemId);
            pendingNotifications.Enqueue($"[MockAP] Received: {itemName}");
            Log.Message($"[MockAP] Item received: {itemName} ({itemId})");
        }
#endif

        public bool HasPendingNotifications()
        {
            return pendingNotifications.Count > 0;
        }

        public string DequeuePendingNotification()
        {
            return pendingNotifications.Dequeue();
        }
    }
}