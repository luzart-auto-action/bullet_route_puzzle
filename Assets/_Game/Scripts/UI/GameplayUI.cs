using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.UI
{
    public class GameplayUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _pauseButton;

        [Header("Info Display")]
        [SerializeField] private TMPro.TextMeshProUGUI _levelText;
        [SerializeField] private TMPro.TextMeshProUGUI _moveCountText;
        [SerializeField] private Transform[] _starSlots;

        [Header("Animation")]
        [SerializeField] private float _buttonPunchScale = 0.2f;
        [SerializeField] private float _buttonPunchDuration = 0.2f;
        [SerializeField] private float _counterPunchScale = 0.3f;

        private void OnEnable()
        {
            _playButton?.onClick.AddListener(OnPlayClicked);
            _resetButton?.onClick.AddListener(OnResetClicked);
            _undoButton?.onClick.AddListener(OnUndoClicked);
            _pauseButton?.onClick.AddListener(OnPauseClicked);

            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<PlayerMoveEvent>(OnPlayerMove);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            _playButton?.onClick.RemoveListener(OnPlayClicked);
            _resetButton?.onClick.RemoveListener(OnResetClicked);
            _undoButton?.onClick.RemoveListener(OnUndoClicked);
            _pauseButton?.onClick.RemoveListener(OnPauseClicked);

            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<PlayerMoveEvent>(OnPlayerMove);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnPlayClicked()
        {
            AnimateButton(_playButton.transform);
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
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level {evt.LevelIndex + 1}";
                _levelText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
            }
            UpdateMoveCount(0);
        }

        private void OnPlayerMove(PlayerMoveEvent evt)
        {
            var levelManager = ServiceLocator.Get<Level.LevelManager>();
            if (levelManager != null)
                UpdateMoveCount(levelManager.MoveCount);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            bool isSetup = evt.NewState == GameStateType.Setup;
            if (_playButton != null) _playButton.interactable = isSetup;
            if (_resetButton != null) _resetButton.interactable = isSetup;
            if (_undoButton != null) _undoButton.interactable = isSetup;
        }

        private void UpdateMoveCount(int count)
        {
            if (_moveCountText != null)
            {
                _moveCountText.text = count.ToString();
                _moveCountText.transform.DOPunchScale(Vector3.one * _counterPunchScale, 0.2f, 1, 0.5f);
            }
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
