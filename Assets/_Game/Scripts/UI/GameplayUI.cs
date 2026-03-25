using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Data;

namespace BulletRoute.UI
{
    /// <summary>
    /// Gameplay HUD — extends UIPanel so it's managed by UIManager like every other panel.
    /// Starts inactive. GameManager calls ShowPanel("GameplayUI") when entering gameplay.
    /// All subscriptions in OnEnable/OnDisable: panel active = subscribed, inactive = silent.
    /// No CanvasGroup — uses simple SetActive for show/hide.
    /// </summary>
    public class GameplayUI : UIPanel
    {
        [Header("Buttons")]
        [SerializeField] private Button _fireButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _hintButton;

        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _moveCountText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Transform[] _starSlots;

        [Header("Button Animation")]
        [SerializeField] private float _buttonPunchScale = 0.2f;
        [SerializeField] private float _buttonPunchDuration = 0.2f;
        [SerializeField] private float _counterPunchScale = 0.3f;

        private Tween _timerWarningTween;

        // ════════════════════════════════════════
        //  LIFECYCLE — subscribe only while active
        // ════════════════════════════════════════

        private void OnEnable()
        {
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<PlayerMoveEvent>(OnPlayerMove);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<TimerTickEvent>(OnTimerTick);

            _fireButton?.onClick.AddListener(OnFireClicked);
            _resetButton?.onClick.AddListener(OnResetClicked);
            _undoButton?.onClick.AddListener(OnUndoClicked);
            _pauseButton?.onClick.AddListener(OnPauseClicked);
            _hintButton?.onClick.AddListener(OnHintClicked);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<PlayerMoveEvent>(OnPlayerMove);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<TimerTickEvent>(OnTimerTick);

            _fireButton?.onClick.RemoveListener(OnFireClicked);
            _resetButton?.onClick.RemoveListener(OnResetClicked);
            _undoButton?.onClick.RemoveListener(OnUndoClicked);
            _pauseButton?.onClick.RemoveListener(OnPauseClicked);
            _hintButton?.onClick.RemoveListener(OnHintClicked);

            _timerWarningTween?.Kill();
            _timerWarningTween = null;
        }

        // ════════════════════════════════════════
        //  SHOW / HIDE — simple SetActive, no CanvasGroup
        // ════════════════════════════════════════

        public override void Show()
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
        }

        public override void Hide()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
        }

        // ════════════════════════════════════════
        //  EVENT HANDLERS
        // ════════════════════════════════════════

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (_levelText != null)
            {
                int level = PlayerProgressData.GetCurrentLevel() + 1;
                _levelText.text = $"Level {level}";
                _levelText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
            }
            UpdateMoveCount(0);

            if (_timerText != null)
            {
                _timerText.color = Color.white;
                _timerText.transform.localScale = Vector3.one;
                _timerWarningTween?.Kill();
                _timerWarningTween = null;
            }
        }

        private void OnTimerTick(TimerTickEvent evt)
        {
            if (_timerText == null) return;

            int minutes = Mathf.FloorToInt(evt.TimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(evt.TimeRemaining % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";

            if (ServiceLocator.TryGet<GameConfig>(out var config))
            {
                float threshold = config.TimerWarningThreshold;
                if (evt.TimeRemaining <= threshold && _timerWarningTween == null)
                {
                    _timerText.color = config.TimerWarningColor;
                    _timerWarningTween = _timerText.transform
                        .DOScale(1.15f, 0.5f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            }
        }

        private void OnPlayerMove(PlayerMoveEvent evt)
        {
            var lm = ServiceLocator.Get<Level.LevelManager>();
            if (lm != null) UpdateMoveCount(lm.MoveCount);
        }

        /// <summary>
        /// Only toggles button interactability. Show/Hide is orchestrated by GameManager.
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            bool isSetup = evt.NewState == GameStateType.Setup;
            if (_fireButton != null) _fireButton.interactable = isSetup;
            if (_resetButton != null) _resetButton.interactable = isSetup;
            if (_undoButton != null) _undoButton.interactable = isSetup;
            if (_hintButton != null) _hintButton.interactable = isSetup;
        }

        // ════════════════════════════════════════
        //  BUTTON HANDLERS
        // ════════════════════════════════════════

        private void OnFireClicked()
        {
            AnimateButton(_fireButton.transform);
            EventBus.Publish(new PlayButtonPressedEvent());
        }
        private void OnResetClicked()
        {
            AnimateButton(_resetButton.transform);
            EventBus.Publish(new ResetButtonPressedEvent());
        }
        private void OnUndoClicked()
        {
            AnimateButton(_undoButton.transform);
            ServiceLocator.Get<Level.LevelManager>()?.CommandManager.Undo();
        }
        private void OnPauseClicked()
        {
            AnimateButton(_pauseButton.transform);
            ServiceLocator.Get<GameState.GameStateManager>()?.ChangeState(GameStateType.Paused);
            EventBus.Publish(new ShowPanelEvent { PanelName = "PopupPause" });
        }
        private void OnHintClicked()
        {
            AnimateButton(_hintButton.transform);
            EventBus.Publish(new HintRequestedEvent());
        }

        // ════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════

        private void UpdateMoveCount(int count)
        {
            if (_moveCountText == null) return;
            _moveCountText.text = count.ToString();
            _moveCountText.transform.DOPunchScale(Vector3.one * _counterPunchScale, 0.2f, 1, 0.5f);
        }

        private void AnimateButton(Transform btn)
        {
            btn.DOKill();
            btn.localScale = Vector3.one;
            btn.DOPunchScale(Vector3.one * _buttonPunchScale, _buttonPunchDuration, 2, 0.5f);
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
        }
    }
}
