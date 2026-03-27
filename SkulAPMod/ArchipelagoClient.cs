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
        private int _itemsToSkip;

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
                        Log.Message($"Slot Data: {data.Key} = {data.Value}");

                    session.MessageLog.OnMessageReceived += OnMessageReceived;

                    APSaveManager.Load(SlotName);
                    session.Items.ItemReceived += OnItemReceived;
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
            Log.Message("Disconnected from Archipelago");
        }

        private void OnMessageReceived(LogMessage message)
        {
            pendingNotifications.Enqueue(message.ToString());
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

            string color = flags switch
            {
                _ when (flags & ItemFlags.Trap) != 0         => "fa8080",
                _ when (flags & ItemFlags.Advancement) != 0  => "9676f5",
                _ when (flags & ItemFlags.NeverExclude) != 0 => "318ce0",
                _                                             => "ffffff"
            };
            string notification = $"Received <color=#{color}>{itemName}</color>\nfrom {playerName}!";
            SkulAPMod.QueueMainThreadAction(() =>
            {
                ArchipelagoItemTracker.AddReceivedItem(itemId);
                ArchipelagoItemHandler.GrantItem(itemId);
                Scenes.GameBase.instance?.uiManager?.unlockNotice?.Show(SkulAPMod._archipelagoSprite, notification);
            });

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

        public void MockReceiveItem(long itemId, string itemName = null)
        {
            itemName ??= $"Item#{itemId}";
            ArchipelagoItemTracker.AddReceivedItem(itemId);
            pendingNotifications.Enqueue($"[MockAP] Received: {itemName}");
            Log.Message($"[MockAP] Item received: {itemName} ({itemId})");
        }
#endif

        public bool HasPendingNotifications() => pendingNotifications.Count > 0;

        public string DequeuePendingNotification() => pendingNotifications.Dequeue();
    }
}
