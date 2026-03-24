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
    /// Main menu with level selection grid. Each level shows star progress.
    /// Levels are built dynamically in Show() from PlayerProgressData.
    /// </summary>
    public class UIMainMenu : UIPanel
    {
        [Header("Main Menu")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _playButtonText;

        [Header("Level Grid")]
        [SerializeField] private Transform _levelGridContainer;
        [SerializeField] private ScrollRect _levelScrollRect;
        [SerializeField] private int _totalLevels = 30;

        [Header("Level Cell Colors")]
        [SerializeField] private Color _unlockedColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color _completedColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color _lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _starActiveColor = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private Color _starInactiveColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        private List<LevelCellUI> _cells = new List<LevelCellUI>();
        private int _selectedLevel;
        private Tween _titlePulse;

        private class LevelCellUI
        {
            public GameObject Root;
            public Button Button;
            public Image Background;
            public TextMeshProUGUI NumberText;
            public Image[] Stars = new Image[3];
            public GameObject LockIcon;
            public int LevelIndex;
        }

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
            _titlePulse?.Kill();
        }

        // ════════════════════════════════════════
        //  SHOW / HIDE
        // ════════════════════════════════════════

        public override void Show()
        {
            base.Show();

            _selectedLevel = PlayerProgressData.GetCurrentLevel();
            BuildLevelGrid();
            UpdatePlayButton();

            // Title pulse
            if (_titleText != null)
            {
                _titlePulse?.Kill();
                _titlePulse = _titleText.transform
                    .DOScale(1.05f, 1.2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            // Play button bounce in
            if (_playButton != null)
            {
                _playButton.transform.localScale = Vector3.zero;
                _playButton.transform.DOScale(1f, 0.5f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.3f)
                    .SetUpdate(true);
            }
        }

        public override void Hide()
        {
            _titlePulse?.Kill();
            base.Hide();
        }

        // ════════════════════════════════════════
        //  LEVEL GRID
        // ════════════════════════════════════════

        private void BuildLevelGrid()
        {
            if (_levelGridContainer == null) return;

            // Clear old cells
            foreach (var cell in _cells)
                if (cell.Root != null) Destroy(cell.Root);
            _cells.Clear();

            int currentLevel = PlayerProgressData.GetCurrentLevel();

            for (int i = 0; i < _totalLevels; i++)
            {
                var cell = CreateLevelCell(i, currentLevel);
                _cells.Add(cell);

                // Staggered appear animation
                int index = i;
                cell.Root.transform.localScale = Vector3.zero;
                cell.Root.transform.DOScale(1f, 0.3f)
                    .SetDelay(index * 0.03f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            HighlightSelected();
        }

        private LevelCellUI CreateLevelCell(int levelIndex, int currentLevel)
        {
            var cell = new LevelCellUI { LevelIndex = levelIndex };
            var progress = PlayerProgressData.GetLevelProgress(levelIndex);
            bool isUnlocked = levelIndex <= currentLevel;
            bool isCompleted = progress.IsCompleted;

            // Root
            cell.Root = new GameObject($"LevelCell_{levelIndex + 1}");
            cell.Root.transform.SetParent(_levelGridContainer, false);
            var rt = cell.Root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 140);

            // Background
            cell.Background = cell.Root.AddComponent<Image>();
            cell.Background.color = isCompleted ? _completedColor : isUnlocked ? _unlockedColor : _lockedColor;

            // Round corners effect (slight transparency on edges)
            cell.Background.type = Image.Type.Sliced;
            cell.Background.pixelsPerUnitMultiplier = 2f;

            // Button
            cell.Button = cell.Root.AddComponent<Button>();
            cell.Button.targetGraphic = cell.Background;
            if (!isUnlocked) cell.Button.interactable = false;

            int capturedIndex = levelIndex;
            cell.Button.onClick.AddListener(() => OnLevelCellClicked(capturedIndex));

            // Level number
            var numberGo = new GameObject("Number");
            numberGo.transform.SetParent(cell.Root.transform, false);
            var numberRt = numberGo.AddComponent<RectTransform>();
            numberRt.anchorMin = new Vector2(0, 0.4f);
            numberRt.anchorMax = new Vector2(1, 1f);
            numberRt.offsetMin = Vector2.zero;
            numberRt.offsetMax = Vector2.zero;
            cell.NumberText = numberGo.AddComponent<TextMeshProUGUI>();
            cell.NumberText.text = (levelIndex + 1).ToString();
            cell.NumberText.fontSize = 36;
            cell.NumberText.fontStyle = FontStyles.Bold;
            cell.NumberText.alignment = TextAlignmentOptions.Center;
            cell.NumberText.color = isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f);

            // Stars container
            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(cell.Root.transform, false);
            var starsRt = starsGo.AddComponent<RectTransform>();
            starsRt.anchorMin = new Vector2(0, 0);
            starsRt.anchorMax = new Vector2(1, 0.35f);
            starsRt.offsetMin = new Vector2(5, 5);
            starsRt.offsetMax = new Vector2(-5, -2);
            var starsLayout = starsGo.AddComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 2;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childForceExpandWidth = true;
            starsLayout.childForceExpandHeight = true;

            // 3 stars
            for (int s = 0; s < 3; s++)
            {
                var starGo = new GameObject($"Star_{s}");
                starGo.transform.SetParent(starsGo.transform, false);
                var starImg = starGo.AddComponent<Image>();
                bool earned = isCompleted && s < progress.BestStars;
                starImg.color = earned ? _starActiveColor : _starInactiveColor;
                // Simple star representation using Unicode
                var starText = starGo.AddComponent<TextMeshProUGUI>();
                starText.text = "\u2605"; // ★
                starText.fontSize = 20;
                starText.alignment = TextAlignmentOptions.Center;
                starText.color = earned ? _starActiveColor : _starInactiveColor;
                // Hide the image, use text for star
                starImg.color = Color.clear;
                cell.Stars[s] = starImg;
            }

            // Lock icon (for locked levels)
            if (!isUnlocked)
            {
                var lockGo = new GameObject("Lock");
                lockGo.transform.SetParent(cell.Root.transform, false);
                var lockRt = lockGo.AddComponent<RectTransform>();
                lockRt.anchorMin = new Vector2(0.25f, 0.3f);
                lockRt.anchorMax = new Vector2(0.75f, 0.8f);
                lockRt.offsetMin = Vector2.zero;
                lockRt.offsetMax = Vector2.zero;
                var lockText = lockGo.AddComponent<TextMeshProUGUI>();
                lockText.text = "\U0001F512"; // 🔒
                lockText.fontSize = 28;
                lockText.alignment = TextAlignmentOptions.Center;
                lockText.color = new Color(1, 1, 1, 0.4f);
                cell.LockIcon = lockGo;
                cell.NumberText.gameObject.SetActive(false);
            }

            return cell;
        }

        private void HighlightSelected()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.Background == null) continue;

                if (i == _selectedLevel)
                {
                    cell.Background.color = _selectedColor;
                    cell.Root.transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
                }
                else
                {
                    var progress = PlayerProgressData.GetLevelProgress(i);
                    bool isUnlocked = i <= PlayerProgressData.GetCurrentLevel();
                    cell.Background.color = progress.IsCompleted ? _completedColor : isUnlocked ? _unlockedColor : _lockedColor;
                    cell.Root.transform.DOScale(1f, 0.15f).SetUpdate(true);
                }
            }
        }

        private void UpdatePlayButton()
        {
            if (_playButtonText != null)
                _playButtonText.text = $"PLAY LEVEL {_selectedLevel + 1}";
        }

        // ════════════════════════════════════════
        //  INTERACTIONS
        // ════════════════════════════════════════

        private void OnLevelCellClicked(int levelIndex)
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _selectedLevel = levelIndex;
            PlayerProgressData.SetCurrentLevel(levelIndex);
            HighlightSelected();
            UpdatePlayButton();
        }

        private void OnPlayClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _playButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    var gm = ServiceLocator.Get<GameManager>();
                    gm?.StartCurrentLevel();
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
