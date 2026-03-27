using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace BulletRoute.UI
{
    /// <summary>
    /// Single level node on the path map. PREFAB — edit in Inspector.
    /// Uses 3 separate GameObjects for visual states: completed, current, locked.
    /// Only one is active at a time.
    /// </summary>
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("State Visuals — assign 3 child GameObjects")]
        [SerializeField] private GameObject _completedVisual;
        [SerializeField] private GameObject _currentVisual;
        [SerializeField] private GameObject _lockedVisual;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private GameObject _starsContainer;
        [SerializeField] private Image _star1;
        [SerializeField] private Image _star2;
        [SerializeField] private Image _star3;
        [SerializeField] private Button _button;

        [Header("Star Colors")]
        [SerializeField] private Color _starEarned = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private Color _starEmpty = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        private int _levelIndex;
        private Tween _pulseTween;

        public int LevelIndex => _levelIndex;
        public Button Button => _button;

        /// <summary>
        /// Called by UIMainMenu after Instantiate. Activates the correct visual GameObject.
        /// </summary>
        public void Setup(int levelIndex, int stars, bool isCompleted, bool isCurrent, bool isLocked)
        {
            _levelIndex = levelIndex;

            // ── State visuals: show exactly one ──
            if (_completedVisual != null) _completedVisual.SetActive(isCompleted && !isCurrent);
            if (_currentVisual != null)   _currentVisual.SetActive(isCurrent);
            if (_lockedVisual != null)    _lockedVisual.SetActive(isLocked && !isCurrent && !isCompleted);

            // ── Number ──
            if (_numberText != null)
            {
                _numberText.text = (levelIndex + 1).ToString();
                _numberText.gameObject.SetActive(!isLocked);
            }

            // ── Stars (only visible when completed) ──
            if (_starsContainer != null)
                _starsContainer.SetActive(isCompleted);

            SetStar(_star1, isCompleted && stars >= 1);
            SetStar(_star2, isCompleted && stars >= 2);
            SetStar(_star3, isCompleted && stars >= 3);

            // ── Button interactable ──
            if (_button != null)
                _button.interactable = isCurrent;

            // ── Current level pulse ──
            _pulseTween?.Kill();
            if (isCurrent)
            {
                transform.localScale = Vector3.one;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        private void SetStar(Image star, bool earned)
        {
            if (star == null) return;
            star.color = earned ? _starEarned : _starEmpty;
        }

        private void OnDestroy()
        {
            _pulseTween?.Kill();
        }
    }
}
