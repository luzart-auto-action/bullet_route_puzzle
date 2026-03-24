using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Data;

namespace BulletRoute.UI
{
    public class GameplayUI : MonoBehaviour
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

        [Header("Animation")]
        [SerializeField] private float _buttonPunchScale = 0.2f;
        [SerializeField] private float _buttonPunchDuration = 0.2f;
        [SerializeField] private float _counterPunchScale = 0.3f;

        private CanvasGroup _canvasGroup;
        private Tween _timerWarningTween;
        private bool _subscribedButtons;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Subscribe events in Awake (NOT OnEnable) so they persist
            // even when UI is hidden via CanvasGroup
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<PlayerMoveEvent>(OnPlayerMove);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<TimerTickEvent>(OnTimerTick);

            // Start hidden (game begins at MainMenu)
            HideUI();
        }

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (_subscribedButtons) return;
            _subscribedButtons = true;
            _fireButton?.onClick.AddListener(OnFireClicked);
            _resetButton?.onClick.AddListener(OnResetClicked);
            _undoButton?.onClick.AddListener(OnUndoClicked);
            _pauseButton?.onClick.AddListener(OnPauseClicked);
            _hintButton?.onClick.AddListener(OnHintClicked);
        }

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
            var levelManager = ServiceLocator.Get<Level.LevelManager>();
            levelManager?.CommandManager.Undo();
        }

        private void OnPauseClicked()
        {
            AnimateButton(_pauseButton.transform);
            var stateManager = ServiceLocator.Get<GameState.GameStateManager>();
            stateManager?.ChangeState(GameStateType.Paused);
            EventBus.Publish(new ShowPanelEvent { PanelName = "PopupPause" });
        }

        private void OnHintClicked()
        {
            AnimateButton(_hintButton.transform);
            EventBus.Publish(new HintRequestedEvent());
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (_levelText != null)
            {
                int level = PlayerProgressData.GetCurrentLevel() + 1;
                _levelText.text = $"Level {level}";
                _levelText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
            }
            UpdateMoveCount(0);

            // Reset timer display
            if (_timerText != null)
            {
                _timerText.color = Color.white;
                _timerWarningTween?.Kill();
            }
        }

        private void OnTimerTick(TimerTickEvent evt)
        {
            if (_timerText == null) return;

            int minutes = Mathf.FloorToInt(evt.TimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(evt.TimeRemaining % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";

            // Warning flash
            var config = ServiceLocator.Get<GameConfig>();
            float threshold = config != null ? config.TimerWarningThreshold : 10f;

            if (evt.TimeRemaining <= threshold && _timerWarningTween == null)
            {
                Color warningColor = config != null ? config.TimerWarningColor : Color.red;
                _timerText.color = warningColor;
                _timerWarningTween = _timerText.transform
                    .DOScale(1.15f, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnPlayerMove(PlayerMoveEvent evt)
        {
            var levelManager = ServiceLocator.Get<Level.LevelManager>();
            if (levelManager != null)
                UpdateMoveCount(levelManager.MoveCount);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // Hide during MainMenu, Win, Fail
            if (evt.NewState == GameStateType.MainMenu ||
                evt.NewState == GameStateType.Win ||
                evt.NewState == GameStateType.Fail)
            {
                HideUI();
                // Also disable all buttons
                if (_fireButton != null) _fireButton.interactable = false;
                if (_resetButton != null) _resetButton.interactable = false;
                if (_undoButton != null) _undoButton.interactable = false;
                if (_hintButton != null) _hintButton.interactable = false;
                return;
            }

            // Show during gameplay states
            if (evt.NewState == GameStateType.Setup ||
                evt.NewState == GameStateType.Simulating ||
                evt.NewState == GameStateType.Loading)
            {
                ShowUI();
            }

            bool isSetup = evt.NewState == GameStateType.Setup;
            if (_fireButton != null) _fireButton.interactable = isSetup;
            if (_resetButton != null) _resetButton.interactable = isSetup;
            if (_undoButton != null) _undoButton.interactable = isSetup;
            if (_hintButton != null) _hintButton.interactable = isSetup;
        }

        private void UpdateMoveCount(int count)
        {
            if (_moveCountText != null)
            {
                _moveCountText.text = count.ToString();
                _moveCountText.transform.DOPunchScale(Vector3.one * _counterPunchScale, 0.2f, 1, 0.5f);
            }
        }

        private void ShowUI()
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void HideUI()
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void AnimateButton(Transform btn)
        {
            btn.DOKill();
            btn.localScale = Vector3.one;
            btn.DOPunchScale(Vector3.one * _buttonPunchScale, _buttonPunchDuration, 2, 0.5f);
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<PlayerMoveEvent>(OnPlayerMove);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<TimerTickEvent>(OnTimerTick);
            _timerWarningTween?.Kill();
        }
    }
}
