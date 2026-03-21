using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.UI
{
    public class FailPanel : UIPanel
    {
        [Header("Fail Panel")]
        [SerializeField] private TMPro.TextMeshProUGUI _failText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Transform _failIcon;

        private void OnEnable()
        {
            _retryButton?.onClick.AddListener(OnRetryClicked);
            _homeButton?.onClick.AddListener(OnHomeClicked);
        }

        private void OnDisable()
        {
            _retryButton?.onClick.RemoveListener(OnRetryClicked);
            _homeButton?.onClick.RemoveListener(OnHomeClicked);
        }

        public override void Show()
        {
            base.Show();

            if (_failText != null)
                _failText.text = "Time's Up!";

            if (_failIcon != null)
            {
                _failIcon.DOShakeRotation(0.5f, 30f, 10, 90f).SetUpdate(true);
            }

            EventBus.Publish(new PlaySFXEvent { ClipName = "LevelFail" });
            EventBus.Publish(new CameraShakeEvent { Intensity = 0.4f, Duration = 0.3f });
        }

        private void OnRetryClicked()
        {
            // State transition (Fail -> Loading) will hide this panel via FailState.Exit()
            var gm = ServiceLocator.Get<GameManager>();
            var lm = ServiceLocator.Get<Level.LevelManager>();
            if (gm != null && lm != null)
            {
                gm.LoadLevel(lm.CurrentLevelIndex);
            }
        }

        private void OnHomeClicked()
        {
            // State transition handles hiding
            EventBus.Publish(new GoToMainMenuEvent());
        }
    }
}
