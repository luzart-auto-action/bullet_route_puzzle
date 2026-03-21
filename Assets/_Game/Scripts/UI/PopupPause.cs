using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.GameState;

namespace BulletRoute.UI
{
    public class PopupPause : UIPanel
    {
        [Header("Pause Menu")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _settingsButton;

        private void OnEnable()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _homeButton?.onClick.AddListener(OnHomeClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDisable()
        {
            _resumeButton?.onClick.RemoveListener(OnResumeClicked);
            _homeButton?.onClick.RemoveListener(OnHomeClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsClicked);
        }

        public override void Show()
        {
            base.Show();
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
        }

        private void OnResumeClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _resumeButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f).SetUpdate(true);

            Hide();

            // Resume game - exit paused state back to setup
            var stateManager = ServiceLocator.Get<GameStateManager>();
            stateManager?.ChangeState(GameStateType.Setup);
        }

        private void OnHomeClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });

            Hide();

            // GoToMainMenu already restores timeScale
            EventBus.Publish(new GoToMainMenuEvent());
        }

        private void OnSettingsClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            _settingsButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f).SetUpdate(true);
            EventBus.Publish(new ShowPanelEvent { PanelName = "PopupSettings" });
        }
    }
}
