using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Level;
using BulletRoute.GameState;
using BulletRoute.Data;
using BulletRoute.Timer;
using BulletRoute.UI;

namespace BulletRoute.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("DOTween Settings")]
        [SerializeField] private int _tweenCapacity = 500;
        [SerializeField] private int _sequenceCapacity = 100;

        private GameStateManager _stateManager;
        private LevelManager _levelManager;

        private void Awake()
        {
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly)
                .SetCapacity(_tweenCapacity, _sequenceCapacity);
            DOTween.defaultAutoPlay = AutoPlay.All;
            DOTween.defaultUpdateType = UpdateType.Normal;

            ServiceLocator.Register(this);
            if (_gameConfig != null)
                ServiceLocator.Register(_gameConfig);
        }

        private void Start()
        {
            _stateManager = ServiceLocator.Get<GameStateManager>();
            _levelManager = ServiceLocator.Get<LevelManager>();

            // GameManager is the SINGLE orchestrator for all game events.
            // No other system should subscribe to these button/state events.
            EventBus.Subscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Subscribe<ResetButtonPressedEvent>(OnResetPressed);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<LevelFailedEvent>(OnLevelFailed);
            EventBus.Subscribe<GoToMainMenuEvent>(OnGoToMainMenu);
            EventBus.Subscribe<TimerExpiredEvent>(OnTimerExpired);

            // Start at Main Menu
            _stateManager.ChangeState(GameStateType.MainMenu);
        }

        // ==================== PUBLIC API (called by UI panels) ====================

        /// <summary>
        /// Called by UIMainMenu when Play is pressed. Loads current level.
        /// </summary>
        public void StartCurrentLevel()
        {
            int currentLevel = PlayerProgressData.GetCurrentLevel();
            LoadLevel(currentLevel);
            EventBus.Publish(new PlayMusicEvent { TrackName = "GameplayMusic" });
        }

        /// <summary>
        /// Load a specific level. Clears old level, rebuilds, starts timer.
        /// Can be called from any state (Win, Fail, Setup, etc.)
        /// </summary>
        public void LoadLevel(int index)
        {
            // Stop everything from previous level
            _levelManager.StopAllBullets();
            var timer = ServiceLocator.Get<LevelTimer>();
            timer?.StopTimer();

            // Force-hide all popup panels immediately (no animation)
            // This prevents animation conflicts when transitioning from Win/Fail
            ForceHideAllPanels();

            // Clear old level
            _levelManager.ClearLevel();

            // Load new level
            _stateManager.ChangeState(GameStateType.Loading);
            _levelManager.LoadLevel(index);
            _stateManager.ChangeState(GameStateType.Setup);

            // Start timer
            if (timer != null && _levelManager.CurrentLevel != null)
            {
                timer.StartTimer(_levelManager.CurrentLevel.TimeLimit, index);
            }
        }

        /// <summary>
        /// Load next level. Called by WinPanel.
        /// </summary>
        public void NextLevel()
        {
            int next = _levelManager.CurrentLevelIndex + 1;
            PlayerProgressData.SetCurrentLevel(next);
            LoadLevel(next);
        }

        /// <summary>
        /// Return to main menu. Called by any panel Home button.
        /// </summary>
        public void GoToMainMenu()
        {
            _levelManager.StopAllBullets();
            var timer = ServiceLocator.Get<LevelTimer>();
            timer?.StopTimer();

            ForceHideAllPanels();
            _levelManager.ClearLevel();

            Time.timeScale = 1f;

            _stateManager.ChangeState(GameStateType.MainMenu);
        }

        private void ForceHideAllPanels()
        {
            var uiManager = ServiceLocator.Get<UI.UIManager>();
            uiManager?.ForceHideAll();
        }

        // ==================== EVENT HANDLERS (single point of control) ====================

        private void OnPlayPressed(PlayButtonPressedEvent evt)
        {
            if (_stateManager.CurrentStateType != GameStateType.Setup) return;

            _stateManager.ChangeState(GameStateType.Simulating);
            _levelManager.FireBullets();
        }

        private void OnResetPressed(ResetButtonPressedEvent evt)
        {
            // Reload current level from scratch
            LoadLevel(_levelManager.CurrentLevelIndex);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            PlayerProgressData.SaveLevelProgress(evt.LevelIndex, evt.Stars, evt.MoveCount);
            _stateManager.ChangeState(GameStateType.Win);
        }

        private void OnLevelFailed(LevelFailedEvent evt)
        {
            _levelManager.StopAllBullets();
            _stateManager.ChangeState(GameStateType.Fail);
        }

        private void OnGoToMainMenu(GoToMainMenuEvent evt)
        {
            GoToMainMenu();
        }

        private void OnTimerExpired(TimerExpiredEvent evt)
        {
            EventBus.Publish(new LevelFailedEvent { LevelIndex = evt.LevelIndex });
        }

        // ==================== CLEANUP ====================

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Unsubscribe<ResetButtonPressedEvent>(OnResetPressed);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<LevelFailedEvent>(OnLevelFailed);
            EventBus.Unsubscribe<GoToMainMenuEvent>(OnGoToMainMenu);
            EventBus.Unsubscribe<TimerExpiredEvent>(OnTimerExpired);

            ServiceLocator.Clear();
            EventBus.Clear();
        }
    }
}
