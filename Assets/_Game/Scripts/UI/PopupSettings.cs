using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Audio;

namespace BulletRoute.UI
{
    public class PopupSettings : UIPanel
    {
        [Header("Settings")]
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _sfxValueText;
        [SerializeField] private TextMeshProUGUI _musicValueText;

        private const string SFX_VOLUME_KEY = "BulletRoute_SFXVolume";
        private const string MUSIC_VOLUME_KEY = "BulletRoute_MusicVolume";

        private void OnEnable()
        {
            _closeButton?.onClick.AddListener(OnCloseClicked);
            _sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
            _musicSlider?.onValueChanged.AddListener(OnMusicChanged);
        }

        private void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(OnCloseClicked);
            _sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
            _musicSlider?.onValueChanged.RemoveListener(OnMusicChanged);
        }

        public override void Show()
        {
            base.Show();

            // Load saved values
            float sfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            float music = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);

            if (_sfxSlider != null)
            {
                _sfxSlider.minValue = 0f;
                _sfxSlider.maxValue = 1f;
                _sfxSlider.value = sfx;
            }

            if (_musicSlider != null)
            {
                _musicSlider.minValue = 0f;
                _musicSlider.maxValue = 1f;
                _musicSlider.value = music;
            }

            UpdateValueTexts(sfx, music);
        }

        private void OnSFXChanged(float value)
        {
            var audio = ServiceLocator.Get<AudioManager>();
            audio?.SetSFXVolume(value);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);

            if (_sfxValueText != null)
                _sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnMusicChanged(float value)
        {
            var audio = ServiceLocator.Get<AudioManager>();
            audio?.SetMusicVolume(value);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);

            if (_musicValueText != null)
                _musicValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void UpdateValueTexts(float sfx, float music)
        {
            if (_sfxValueText != null)
                _sfxValueText.text = $"{Mathf.RoundToInt(sfx * 100)}%";
            if (_musicValueText != null)
                _musicValueText.text = $"{Mathf.RoundToInt(music * 100)}%";
        }

        private void OnCloseClicked()
        {
            EventBus.Publish(new PlaySFXEvent { ClipName = "ButtonClick" });
            PlayerPrefs.Save();
            Hide();
        }
    }
}
