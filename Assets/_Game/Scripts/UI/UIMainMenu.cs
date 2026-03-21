using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Data;

namespace BulletRoute.UI
{
    public class UIMainMenu : UIPanel
    {
        [Header("Main Menu")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _titleText;

        private Tween _titlePulse;

        private void OnEnable()
        {
            _playButton?.onClick.AddListener(OnPlayClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDisable()
        {
            _playButton?.onClick.RemoveListener(OnPlayClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsClicked);
            _titlePulse?.Kill();
        }

        public override void Show()
        {
            base.Show();

            // Update level text
            int currentLevel = PlayerProgressData.GetCurrentLevel() + 1;
            if (_levelText != null)
                _levelText.text = $"Level {currentLevel}";

            // Title pulse animation
            if (_titleText != null)
            {
                _titlePulse?.Kill();
                _titlePulse = _titleText.transform
                    .DOScale(1.05f, 1.2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            // Play button bounce
            if (_playButton != null)
            {
                _playButton.transform.localScale = Vector3.zero;
                _playButton.transform.DOScale(1f, 0.5f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.3f)
                    .SetUpdate(true);
            }
        }

        public override void Hide()
        {
            _titlePulse?.Kill();
            base.Hide();
        }

        private void OnPlayClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });

            _playButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    var gm = ServiceLocator.Get<GameManager>();
                    gm?.StartCurrentLevel();
                });
        }

        private void OnSettingsClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _settingsButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f).SetUpdate(true);
            EventBus.Publish(new ShowPanelEvent { PanelName = "PopupSettings" });
        }
    }
}
