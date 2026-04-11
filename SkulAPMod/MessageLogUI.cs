using System.Collections.Generic;
using UnityEngine;

namespace SkulAPMod
{
    public class MessageLogUI : MonoBehaviour
    {
        private static readonly List<string> _messages = new List<string>();
        private const int MaxMessages = 50;
        private const KeyCode ToggleKey = KeyCode.F9;

        private bool _visible = true;
        private Vector2 _scrollPos;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;

        public static void AddMessage(string richText)
        {
            if (_messages.Count >= MaxMessages)
                _messages.RemoveAt(0);
            _messages.Add(richText);
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                _visible = !_visible;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            InitStyles();

            float panelWidth = 620f;
            float x = 4f;

            var client = SkulAPMod.APClient;
            string statusText;
            Color statusColor;
            if (client != null && client.IsConnected)
            {
                statusText = $"[F9] Skul {SkulAPMod.Version}  |  Connected as {client.SlotName} {(_visible ? "▼" : "▲")}";
                statusColor = new Color(0.59f, 0.96f, 0.59f, 0.9f);
            }
            else
            {
                statusText = $"[F9] AP  |  Disconnected {(_visible ? "▼" : "▲")}";
                statusColor = new Color(0.96f, 0.4f, 0.4f, 0.9f);
            }

            var statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = statusColor }
            };
            GUI.Label(new Rect(x, 2f, panelWidth, 18f), statusText, statusStyle);

            // DeathLink toggle directly after the status text
            if (client != null && client.IsConnected)
            {
                var toggleStyle = new GUIStyle(GUI.skin.toggle)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    onNormal = { textColor = new Color(0.59f, 0.96f, 0.59f) }
                };
                float textWidth = statusStyle.CalcSize(new GUIContent(statusText)).x;
                float toggleX = x + textWidth + 6f;
                bool newVal = GUI.Toggle(new Rect(toggleX, 2f, 110f, 18f), client.DeathLinkEnabled, " DeathLink", toggleStyle);
                if (newVal != client.DeathLinkEnabled)
                    client.SetDeathLinkEnabled(newVal);
            }

            if (!_visible) return;

            float panelHeight = 300f;
            float y = 20f;

            var panelRect = new Rect(x, y, panelWidth, panelHeight);

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.85f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.backgroundColor = prev;

            var scrollRect = new Rect(x + 4, y + 4, panelWidth - 8, panelHeight - 8);
            var contentHeight = Mathf.Max(_messages.Count * 20f, scrollRect.height);
            var viewRect = new Rect(0, 0, scrollRect.width - 16, contentHeight);

            _scrollPos = GUI.BeginScrollView(scrollRect, _scrollPos, viewRect);

            float lineY = contentHeight - (_messages.Count * 20f);
            foreach (var msg in _messages)
            {
                GUI.Label(new Rect(2, lineY, viewRect.width - 4, 20), msg, _labelStyle);
                lineY += 20f;
            }

            GUI.EndScrollView();

            if (Event.current.type == EventType.Layout)
                _scrollPos.y = Mathf.Infinity;
        }
    }
}
