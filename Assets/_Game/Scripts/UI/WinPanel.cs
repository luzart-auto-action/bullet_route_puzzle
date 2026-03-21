using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.UI
{
    public class WinPanel : UIPanel
    {
        [Header("Win Panel")]
        [SerializeField] private TMPro.TextMeshProUGUI _levelCompleteText;
        [SerializeField] private TMPro.TextMeshProUGUI _moveCountText;
        [SerializeField] private TMPro.TextMeshProUGUI _timeRemainingText;
        [SerializeField] private Transform[] _stars;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        [Header("Star Animation")]
        [SerializeField] private float _starDelay = 0.3f;
        [SerializeField] private float _starScaleDuration = 0.4f;
        [SerializeField] private Ease _starEase = Ease.OutBack;

        // Store last event data so we can display it when panel shows
        private LevelCompletedEvent _lastCompletedEvent;
        private bool _hasData;

        private void Awake()
        {
            // Subscribe in Awake so we always receive the event,
            // even when gameObject is inactive
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnEnable()
        {
            _nextButton?.onClick.AddListener(OnNextClicked);
            _retryButton?.onClick.AddListener(OnRetryClicked);
            _homeButton?.onClick.AddListener(OnHomeClicked);
        }

        private void OnDisable()
        {
            _nextButton?.onClick.RemoveListener(OnNextClicked);
            _retryButton?.onClick.RemoveListener(OnRetryClicked);
            _homeButton?.onClick.RemoveListener(OnHomeClicked);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Store data - panel might not be visible yet
            _lastCompletedEvent = evt;
            _hasData = true;
        }

        public override void Show()
        {
            base.Show();

            // Display stored data
            if (_hasData)
            {
                DisplayResults(_lastCompletedEvent);
                _hasData = false;
            }

            EventBus.Publish(new PlaySFXEvent { ClipName = "LevelComplete" });
            EventBus.Publish(new FXRequestEvent
            {
                FXName = "Confetti",
                Position = Vector3.up * 5f,
                Rotation = Quaternion.identity
            });
        }

        private void DisplayResults(LevelCompletedEvent evt)
        {
            if (_moveCountText != null)
                _moveCountText.text = $"Moves: {evt.MoveCount}";

            if (_timeRemainingText != null)
            {
                int minutes = Mathf.FloorToInt(evt.TimeRemaining / 60f);
                int seconds = Mathf.FloorToInt(evt.TimeRemaining % 60f);
                _timeRemainingText.text = $"Time: {minutes:00}:{seconds:00}";
                _timeRemainingText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f).SetUpdate(true);
            }

            AnimateStars(evt.Stars);
        }

        private void AnimateStars(int count)
        {
            if (_stars == null) return;

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].localScale = Vector3.zero;
                bool earned = i < count;

                if (earned)
                {
                    int index = i;
                    DOVirtual.DelayedCall(_starDelay * (i + 1), () =>
                    {
                        _stars[index].DOScale(Vector3.one, _starScaleDuration).SetEase(_starEase);
                        _stars[index].DOPunchRotation(new Vector3(0, 0, 30f), _starScaleDuration, 3, 0.5f);
                        EventBus.Publish(new PlaySFXEvent { ClipName = "StarEarned" });
                        EventBus.Publish(new FXRequestEvent
                        {
                            FXName = "StarBurst",
                            Position = _stars[index].position,
                            Rotation = Quaternion.identity
                        });
                    }).SetUpdate(true);
                }
            }
        }

        private void OnNextClicked()
        {
            // State transition (Win -> Loading) will hide this panel via WinState.Exit()
            var gm = ServiceLocator.Get<GameManager>();
            gm?.NextLevel();
        }

        private void OnRetryClicked()
        {
            var gm = ServiceLocator.Get<GameManager>();
            var lm = ServiceLocator.Get<Level.LevelManager>();
            if (gm != null && lm != null)
            {
                gm.LoadLevel(lm.CurrentLevelIndex);
            }
        }

        private void OnHomeClicked()
        {
            EventBus.Publish(new GoToMainMenuEvent());
        }
    }
}
