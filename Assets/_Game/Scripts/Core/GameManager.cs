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
    /// <summary>
    /// Central orchestrator. ONLY GameManager subscribes to user-action events.
    /// Controls panel visibility: GameplayUI shown during gameplay, hidden at menu/win/fail.
    /// Stores LevelCompletedEvent data for WinPanel to read in Show().
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("DOTween Settings")]
        [SerializeField] private int _tweenCapacity = 500;
        [SerializeField] private int _sequenceCapacity = 100;

        private GameStateManager _stateManager;
        private LevelManager _levelManager;

        /// <summary>
        /// Last level completion data. WinPanel reads this in Show().
        /// </summary>
        public LevelCompletedEvent LastCompletedEvent { get; private set; }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            // recycleAllByDefault must be FALSE — recycling causes Sequences to stall
            // because nested tweens get recycled prematurely, making the Sequence lose track of them.
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly)
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
            EventBus.Subscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Subscribe<ResetButtonPressedEvent>(OnResetPressed);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<LevelFailedEvent>(OnLevelFailed);
            EventBus.Subscribe<GoToMainMenuEvent>(OnGoToMainMenu);
            EventBus.Subscribe<TimerExpiredEvent>(OnTimerExpired);

            // Start at Main Menu
            _stateManager.ChangeState(GameStateType.MainMenu);
        }

        // ==================== PUBLIC API ====================

        public void StartCurrentLevel()
        {
            int currentLevel = PlayerProgressData.GetCurrentLevel();
            LoadLevel(currentLevel);
            EventBus.Publish(new PlayMusicEvent { TrackName = "GameplayMusic" });
        }

        public void LoadLevel(int index)
        {
            _levelManager.StopAllBullets();
            var timer = ServiceLocator.Get<LevelTimer>();
            timer?.StopTimer();

            // Kill all panels (MainMenu, Win, Fail, GameplayUI, popups)
            ForceHideAllPanels();

            _levelManager.ClearLevel();
            _stateManager.ChangeState(GameStateType.Loading);

            // Show GameplayUI BEFORE LoadLevel so it receives LevelStartedEvent
            ShowPanel("GameplayUI");

            _levelManager.LoadLevel(index);
            _stateManager.ChangeState(GameStateType.Setup);

            if (timer != null && _levelManager.CurrentLevel != null)
                timer.StartTimer(_levelManager.CurrentLevel.TimeLimit, index);
        }

        public void NextLevel()
        {
            int next = _levelManager.CurrentLevelIndex + 1;
            PlayerProgressData.SetCurrentLevel(next);
            LoadLevel(next);
        }

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

        // ==================== EVENT HANDLERS ====================

        private void OnPlayPressed(PlayButtonPressedEvent evt)
        {
            Debug.Log($"[GameManager] OnPlayPressed: CurrentState={_stateManager.CurrentStateType}");
            if (_stateManager.CurrentStateType != GameStateType.Setup)
            {
                Debug.Log("[GameManager] BLOCKED: not in Setup state");
                return;
            }
            _stateManager.ChangeState(GameStateType.Simulating);
            _levelManager.FireBullets();
        }

        private void OnResetPressed(ResetButtonPressedEvent evt)
        {
            LoadLevel(_levelManager.CurrentLevelIndex);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Store data for WinPanel to read in Show()
            LastCompletedEvent = evt;
            PlayerProgressData.SaveLevelProgress(evt.LevelIndex, evt.Stars, evt.MoveCount);

            // Hide gameplay HUD, then show Win
            HidePanel("GameplayUI");
            _stateManager.ChangeState(GameStateType.Win);
        }

        private void OnLevelFailed(LevelFailedEvent evt)
        {
            _levelManager.StopAllBullets();

            // Hide gameplay HUD, then show Fail
            HidePanel("GameplayUI");
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

        // ==================== HELPERS ====================

        private void ShowPanel(string name)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            uiManager?.ShowPanel(name);
        }

        private void HidePanel(string name)
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            uiManager?.HidePanel(name);
        }

        private void ForceHideAllPanels()
        {
            var uiManager = ServiceLocator.Get<UIManager>();
            uiManager?.ForceHideAll();
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
