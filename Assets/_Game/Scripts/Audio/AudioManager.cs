using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Audio
{
    [System.Serializable]
    public class AudioEntry
    {
        public string Name;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0.5f, 1.5f)] public float Pitch = 1f;
        public bool RandomizePitch;
        [Range(0f, 0.3f)] public float PitchVariance = 0.1f;
    }

    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("SFX Library")]
        [SerializeField] private List<AudioEntry> _sfxLibrary = new List<AudioEntry>();

        [Header("Music Library")]
        [SerializeField] private List<AudioEntry> _musicLibrary = new List<AudioEntry>();

        [Header("Settings")]
        [SerializeField] private float _musicFadeDuration = 1f;
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.5f;

        private Dictionary<string, AudioEntry> _sfxMap = new Dictionary<string, AudioEntry>();
        private Dictionary<string, AudioEntry> _musicMap = new Dictionary<string, AudioEntry>();
        private Tween _musicFade;

        private void Awake()
        {
            foreach (var entry in _sfxLibrary)
                _sfxMap[entry.Name] = entry;
            foreach (var entry in _musicLibrary)
                _musicMap[entry.Name] = entry;

            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlaySFXEvent>(OnPlaySFX);
            EventBus.Subscribe<PlayMusicEvent>(OnPlayMusic);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlaySFXEvent>(OnPlaySFX);
            EventBus.Unsubscribe<PlayMusicEvent>(OnPlayMusic);
        }

        private void OnPlaySFX(PlaySFXEvent evt)
        {
            PlaySFX(evt.ClipName);
        }

        private void OnPlayMusic(PlayMusicEvent evt)
        {
            PlayMusic(evt.TrackName);
        }

        public void PlaySFX(string name)
        {
            if (!_sfxMap.TryGetValue(name, out var entry))
            {
                Debug.LogWarning($"[AudioManager] SFX not found: {name}. Add it to the SFX Library.");
                return;
            }
            if (entry.Clip == null) return;

            float pitch = entry.Pitch;
            if (entry.RandomizePitch)
                pitch += Random.Range(-entry.PitchVariance, entry.PitchVariance);

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(entry.Clip, entry.Volume * _sfxVolume * _masterVolume);
        }

        public void PlayMusic(string name)
        {
            if (!_musicMap.TryGetValue(name, out var entry))
            {
                Debug.LogWarning($"[AudioManager] Music not found: {name}. Add it to the Music Library.");
                return;
            }
            if (entry.Clip == null) return;

            _musicFade?.Kill();

            if (_musicSource.isPlaying)
            {
                _musicFade = _musicSource.DOFade(0f, _musicFadeDuration * 0.5f)
                    .OnComplete(() =>
                    {
                        _musicSource.clip = entry.Clip;
                        _musicSource.volume = 0f;
                        _musicSource.Play();
                        _musicFade = _musicSource.DOFade(entry.Volume * _musicVolume * _masterVolume, _musicFadeDuration * 0.5f);
                    });
            }
            else
            {
                _musicSource.clip = entry.Clip;
                _musicSource.volume = 0f;
                _musicSource.Play();
                _musicFade = _musicSource.DOFade(entry.Volume * _musicVolume * _masterVolume, _musicFadeDuration);
            }
        }

        public void StopMusic(bool fade = true)
        {
            _musicFade?.Kill();
            if (fade)
                _musicFade = _musicSource.DOFade(0f, _musicFadeDuration).OnComplete(() => _musicSource.Stop());
            else
                _musicSource.Stop();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicVolume * _masterVolume;
        }

        private void OnDestroy()
        {
            _musicFade?.Kill();
            ServiceLocator.Unregister<AudioManager>();
        }
    }
}
