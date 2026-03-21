using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.Timer
{
    public class LevelTimer : MonoBehaviour
    {
        private float _timeLimit;
        private float _timeRemaining;
        private bool _isRunning;
        private int _currentLevelIndex;

        public float TimeRemaining => _timeRemaining;
        public float TimeLimit => _timeLimit;
        public bool IsRunning => _isRunning;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Update()
        {
            if (!_isRunning) return;

            _timeRemaining -= Time.deltaTime;

            EventBus.Publish(new TimerTickEvent
            {
                TimeRemaining = Mathf.Max(0f, _timeRemaining),
                TimeLimit = _timeLimit
            });

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _isRunning = false;
                EventBus.Publish(new TimerExpiredEvent { LevelIndex = _currentLevelIndex });
            }
        }

        public void StartTimer(float timeLimit, int levelIndex)
        {
            _timeLimit = timeLimit;
            _timeRemaining = timeLimit;
            _currentLevelIndex = levelIndex;
            _isRunning = true;

            EventBus.Publish(new TimerStartedEvent { TimeLimit = timeLimit });
        }

        public void StopTimer()
        {
            _isRunning = false;
        }

        public void PauseTimer()
        {
            _isRunning = false;
        }

        public void ResumeTimer()
        {
            if (_timeRemaining > 0f)
                _isRunning = true;
        }

        public void RestartTimer()
        {
            _timeRemaining = _timeLimit;
            _isRunning = true;
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameStateType.Paused:
                    PauseTimer();
                    break;
                case GameStateType.Setup:
                    if (evt.PreviousState == GameStateType.Paused)
                        ResumeTimer();
                    break;
                case GameStateType.Win:
                case GameStateType.Fail:
                case GameStateType.MainMenu:
                    StopTimer();
                    break;
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<LevelTimer>();
        }
    }
}
