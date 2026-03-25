using System.Collections.Generic;
using UnityEngine;

namespace BulletRoute
{
    /// <summary>
    /// Displays Unity console logs on screen. Attach to any GameObject.
    /// Tap top-left corner 3 times to toggle visibility.
    /// Runs before all other scripts to catch early errors.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ScreenLogger : MonoBehaviour
    {
        [SerializeField] private int _maxLines = 30;
        [SerializeField] private int _fontSize = 22;

        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private Vector2 _scrollPos;
        private bool _visible = true;
        private bool _collapsed;
        private int _tapCount;
        private float _lastTapTime;

        private struct LogEntry
        {
            public string Message;
            public LogType Type;
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            _logs.Add(new LogEntry { Message = message, Type = type });
            if (_logs.Count > _maxLines * 2)
                _logs.RemoveRange(0, _logs.Count - _maxLines);
            // Auto-scroll to bottom
            _scrollPos.y = float.MaxValue;
        }

        private void OnGUI()
        {
            // Tap top-left corner (100x100) 3 times to toggle
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.TouchDown)
            {
                var pos = Event.current.mousePosition;
                if (pos.x < 150 && pos.y < 150)
                {
                    if (Time.unscaledTime - _lastTapTime > 1f) _tapCount = 0;
                    _tapCount++;
                    _lastTapTime = Time.unscaledTime;
                    if (_tapCount >= 3) { _visible = !_visible; _tapCount = 0; }
                }
            }

            if (!_visible) return;

            float w = Screen.width;
            float h = Screen.height * 0.45f;

            GUI.skin.label.fontSize = _fontSize;
            GUI.skin.button.fontSize = _fontSize;
            GUI.skin.verticalScrollbar.fixedWidth = 40;
            GUI.skin.verticalScrollbarThumb.fixedWidth = 40;

            // Background
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, w, h + 40), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Toolbar
            if (GUI.Button(new Rect(10, 5, 120, 35), "Clear"))
                _logs.Clear();
            if (GUI.Button(new Rect(140, 5, 180, 35), _collapsed ? "Expand" : "Collapse"))
                _collapsed = !_collapsed;
            GUI.Label(new Rect(340, 5, 300, 35), $"Logs: {_logs.Count}");

            // Log area
            _scrollPos = GUI.BeginScrollView(new Rect(0, 40, w, h), _scrollPos, new Rect(0, 0, w - 50, _logs.Count * (_fontSize + 4)));
            int start = _collapsed ? Mathf.Max(0, _logs.Count - 8) : 0;
            for (int i = start; i < _logs.Count; i++)
            {
                var entry = _logs[i];
                switch (entry.Type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        GUI.color = Color.red;
                        break;
                    case LogType.Warning:
                        GUI.color = Color.yellow;
                        break;
                    default:
                        GUI.color = Color.white;
                        break;
                }
                GUI.Label(new Rect(5, (i - start) * (_fontSize + 4), w - 55, _fontSize + 4), entry.Message);
            }
            GUI.color = Color.white;
            GUI.EndScrollView();
        }
    }
}
