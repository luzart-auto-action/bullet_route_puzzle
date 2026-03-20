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

            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnDisable()
        {
            _nextButton?.onClick.RemoveListener(OnNextClicked);
            _retryButton?.onClick.RemoveListener(OnRetryClicked);
            _homeButton?.onClick.RemoveListener(OnHomeClicked);

            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            if (_moveCountText != null)
                _moveCountText.text = $"Moves: {evt.MoveCount}";

            AnimateStars(evt.Stars);
        }

        public override void Show()
        {
            base.Show();
            EventBus.Publish(new PlaySFXEvent { ClipName = "LevelComplete" });
            EventBus.Publish(new FXRequestEvent
            {
                FXName = "Confetti",
                Position = Vector3.up * 5f,
                Rotation = Quaternion.identity
            });
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
            Hide();
            var levelManager = ServiceLocator.Get<Level.LevelManager>();
            levelManager?.LoadNextLevel();
        }

        private void OnRetryClicked()
        {
            Hide();
            var levelManager = ServiceLocator.Get<Level.LevelManager>();
            levelManager?.ResetLevel();
        }

        private void OnHomeClicked()
        {
            Hide();
            // Navigate to main menu
            EventBus.Publish(new ShowPanelEvent { PanelName = "MainMenu" });
        }
    }
}
