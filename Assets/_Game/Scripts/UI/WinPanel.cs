using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.UI
{
    /// <summary>
    /// Win screen. No Awake subscriptions — GameManager stores completion data,
    /// WinPanel reads it in Show(). Fully managed by UIPanel lifecycle.
    /// </summary>
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

        public override void Show()
        {
            base.Show();

            // Read completion data from GameManager (the orchestrator holds the state)
            var gm = ServiceLocator.Get<GameManager>();
            if (gm != null && gm.LastCompletedEvent.Stars > 0)
            {
                DisplayResults(gm.LastCompletedEvent);
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

                if (i < count)
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
            ServiceLocator.Get<GameManager>()?.NextLevel();
        }

        private void OnRetryClicked()
        {
            var gm = ServiceLocator.Get<GameManager>();
            var lm = ServiceLocator.Get<Level.LevelManager>();
            if (gm != null && lm != null)
                gm.LoadLevel(lm.CurrentLevelIndex);
        }

        private void OnHomeClicked()
        {
            EventBus.Publish(new GoToMainMenuEvent());
        }
    }
}
