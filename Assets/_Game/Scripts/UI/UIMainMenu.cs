using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Data;

namespace BulletRoute.UI
{
    /// <summary>
    /// Vertical path level map. Spawns LevelNodeUI prefab for each level.
    /// Edit the LevelNode prefab to customize the look — UIMainMenu only handles layout + data.
    /// </summary>
    public class UIMainMenu : UIPanel
    {
        [Header("Main Menu")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TextMeshProUGUI _playButtonText;

        [Header("Level Path")]
        [SerializeField] private LevelNodeUI _levelNodePrefab;
        [SerializeField] private Transform _levelPathContainer;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private int _totalLevels = 30;

        [Header("Path Layout")]
        [SerializeField] private float _nodeSpacing = 140f;
        [SerializeField] private float _zigzagOffset = 70f;
        [SerializeField] private float _startY = 100f;

        [Header("Road Prefab — Image prefab spawned between nodes")]
        [SerializeField] private RectTransform _roadPrefab;

        private List<LevelNodeUI> _spawnedNodes = new List<LevelNodeUI>();
        private int _currentLevel;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void OnEnable()
        {
            _playButton?.onClick.AddListener(OnPlayClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDisable()
        {
            _playButton?.onClick.RemoveListener(OnPlayClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsClicked);
        }

        // ════════════════════════════════════════
        //  SHOW
        // ════════════════════════════════════════

        public override void Show()
        {
            base.Show();
            _currentLevel = PlayerProgressData.GetCurrentLevel();
            BuildPath();
            UpdatePlayButton();

            DOVirtual.DelayedCall(0.15f, ScrollToCurrentLevel).SetUpdate(true);

            if (_playButton != null)
            {
                _playButton.transform.localScale = Vector3.zero;
                _playButton.transform.DOScale(1f, 0.5f)
                    .SetEase(Ease.OutBack).SetDelay(0.3f).SetUpdate(true);
            }
        }

        // ════════════════════════════════════════
        //  BUILD PATH — Instantiate prefabs + road
        // ════════════════════════════════════════

        private void BuildPath()
        {
            if (_levelPathContainer == null || _levelNodePrefab == null) return;

            // Clear old
            foreach (var node in _spawnedNodes)
                if (node != null) Destroy(node.gameObject);
            _spawnedNodes.Clear();

            // Clear road segments
            foreach (Transform child in _levelPathContainer)
                Destroy(child.gameObject);

            // Set content height
            float totalHeight = _startY + _totalLevels * _nodeSpacing + 100f;
            var contentRt = _levelPathContainer.GetComponent<RectTransform>();
            if (contentRt != null)
                contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, totalHeight);

            // Spawn nodes bottom to top
            for (int i = 0; i < _totalLevels; i++)
            {
                float yPos = _startY + i * _nodeSpacing;
                float xPos = (i % 2 == 0) ? -_zigzagOffset : _zigzagOffset;

                // Road segment
                if (i > 0)
                {
                    float prevY = _startY + (i - 1) * _nodeSpacing;
                    float prevX = ((i - 1) % 2 == 0) ? -_zigzagOffset : _zigzagOffset;
                    SpawnRoad(prevX, prevY, xPos, yPos);
                }

                // Node
                var node = SpawnNode(i, xPos, yPos);
                _spawnedNodes.Add(node);

                // Stagger animation
                int idx = i;
                node.transform.localScale = Vector3.zero;
                node.transform.DOScale(1f, 0.25f)
                    .SetDelay(idx * 0.02f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private LevelNodeUI SpawnNode(int levelIndex, float xPos, float yPos)
        {
            var node = Instantiate(_levelNodePrefab, _levelPathContainer);
            node.name = $"Level_{levelIndex + 1}";

            var rt = node.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xPos, yPos);

            // Get progress data and setup visual state
            var progress = PlayerProgressData.GetLevelProgress(levelIndex);
            bool isCurrent = levelIndex == _currentLevel;
            bool isCompleted = progress.IsCompleted;
            bool isLocked = levelIndex > _currentLevel;

            node.Setup(levelIndex, progress.BestStars, isCompleted, isCurrent, isLocked);

            // Wire button — only current level plays
            if (isCurrent)
                node.Button.onClick.AddListener(OnPlayClicked);

            return node;
        }

        private void SpawnRoad(float x1, float y1, float x2, float y2)
        {
            if (_roadPrefab == null) return;

            var road = Instantiate(_roadPrefab, _levelPathContainer);
            road.name = "Road";
            road.gameObject.SetActive(true);

            road.anchorMin = new Vector2(0.5f, 0);
            road.anchorMax = new Vector2(0.5f, 0);
            road.pivot = new Vector2(0.5f, 0.5f);
            road.anchoredPosition = new Vector2((x1 + x2) * 0.5f, (y1 + y2) * 0.5f);

            float dist = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
            road.sizeDelta = new Vector2(road.sizeDelta.x, dist);

            float angle = Mathf.Atan2(x2 - x1, y2 - y1) * Mathf.Rad2Deg;
            road.localRotation = Quaternion.Euler(0, 0, -angle);

            // Road behind nodes
            road.SetAsFirstSibling();
        }

        // ════════════════════════════════════════
        //  SCROLL TO CURRENT
        // ════════════════════════════════════════

        private void ScrollToCurrentLevel()
        {
            if (_scrollRect == null || _levelPathContainer == null) return;

            var contentRt = _levelPathContainer.GetComponent<RectTransform>();
            if (contentRt == null) return;

            float contentH = contentRt.sizeDelta.y;
            var viewportRt = _scrollRect.viewport != null
                ? _scrollRect.viewport : _scrollRect.GetComponent<RectTransform>();
            float viewH = viewportRt.rect.height;
            if (contentH <= viewH) return;

            // Target: center the current level node in the viewport
            float nodeY = _startY + _currentLevel * _nodeSpacing;
            float scrollableRange = contentH - viewH;

            // Offset so node sits at center of viewport (subtract half viewport height)
            float targetScroll = nodeY - viewH * 0.5f;
            float norm = Mathf.Clamp01(targetScroll / scrollableRange);

            DOTween.To(() => _scrollRect.verticalNormalizedPosition,
                v => _scrollRect.verticalNormalizedPosition = v,
                norm, 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private void UpdatePlayButton()
        {
            if (_playButtonText != null)
                _playButtonText.text = $"Play Level {_currentLevel + 1}";
        }

        // ════════════════════════════════════════
        //  INTERACTIONS
        // ════════════════════════════════════════

        private void OnPlayClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _playButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    ServiceLocator.Get<GameManager>()?.StartCurrentLevel();
                });
        }

        private void OnSettingsClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _settingsButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f).SetUpdate(true);
            EventBus.Publish(new ShowPanelEvent { PanelName = "PopupSettings" });
        }
    }
}
