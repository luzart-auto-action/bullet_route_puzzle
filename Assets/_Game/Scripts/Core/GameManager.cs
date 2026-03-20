using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Level;
using BulletRoute.GameState;
using BulletRoute.Data;

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
            // Initialize DOTween
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

            // Subscribe to game events
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<LevelFailedEvent>(OnLevelFailed);
            EventBus.Subscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Subscribe<ResetButtonPressedEvent>(OnResetPressed);

            // Start first level
            StartGame();
        }

        private void StartGame()
        {
            _stateManager.ChangeState(GameStateType.Loading);

            int currentLevel = PlayerProgressData.GetCurrentLevel();
            _levelManager.LoadLevel(currentLevel);

            _stateManager.ChangeState(GameStateType.Setup);

            EventBus.Publish(new PlayMusicEvent { TrackName = "GameplayMusic" });
        }

        private void OnPlayPressed(PlayButtonPressedEvent evt)
        {
            _stateManager.ChangeState(GameStateType.Simulating);
        }

        private void OnResetPressed(ResetButtonPressedEvent evt)
        {
            _stateManager.ChangeState(GameStateType.Setup);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            PlayerProgressData.SaveLevelProgress(evt.LevelIndex, evt.Stars, evt.MoveCount);
            _stateManager.ChangeState(GameStateType.Win);
        }

        private void OnLevelFailed(LevelFailedEvent evt)
        {
            _stateManager.ChangeState(GameStateType.Fail);
        }

        public void LoadLevel(int index)
        {
            _stateManager.ChangeState(GameStateType.Loading);
            _levelManager.LoadLevel(index);
            _stateManager.ChangeState(GameStateType.Setup);
        }

        public void NextLevel()
        {
            int next = _levelManager.CurrentLevelIndex + 1;
            PlayerProgressData.SetCurrentLevel(next);
            LoadLevel(next);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<LevelFailedEvent>(OnLevelFailed);
            EventBus.Unsubscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Unsubscribe<ResetButtonPressedEvent>(OnResetPressed);

            ServiceLocator.Clear();
            EventBus.Clear();
        }
    }
}
