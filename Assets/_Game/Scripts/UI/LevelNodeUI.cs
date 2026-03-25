using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace BulletRoute.UI
{
    /// <summary>
    /// Single level node on the path map. PREFAB — edit in Inspector.
    /// State is purely color-based: completed=green, current=blue, locked=grey.
    /// No CanvasGroup manipulation.
    /// </summary>
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private GameObject _starsContainer;
        [SerializeField] private Image _star1;
        [SerializeField] private Image _star2;
        [SerializeField] private Image _star3;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private Button _button;

        [Header("Background Colors")]
        [SerializeField] private Color _completedColor = new Color(0.25f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color _currentColor = new Color(0.3f, 0.65f, 1f, 1f);
        [SerializeField] private Color _lockedColor = new Color(0.45f, 0.55f, 0.7f, 1f);

        [Header("Number Colors")]
        [SerializeField] private Color _numberNormal = Color.white;
        [SerializeField] private Color _numberLocked = new Color(1f, 1f, 1f, 0.4f);

        [Header("Star Colors")]
        [SerializeField] private Color _starEarned = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private Color _starEmpty = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        private int _levelIndex;
        private Tween _pulseTween;

        public int LevelIndex => _levelIndex;
        public Button Button => _button;

        /// <summary>
        /// Called by UIMainMenu after Instantiate. Sets visual state by color only.
        /// </summary>
        public void Setup(int levelIndex, int stars, bool isCompleted, bool isCurrent, bool isLocked)
        {
            _levelIndex = levelIndex;

            // ── Number ──
            if (_numberText != null)
            {
                _numberText.text = (levelIndex + 1).ToString();
                _numberText.gameObject.SetActive(!isLocked);
                _numberText.color = isLocked ? _numberLocked : _numberNormal;
            }

            // ── Stars (only visible when completed) ──
            if (_starsContainer != null)
                _starsContainer.SetActive(isCompleted);

            SetStar(_star1, isCompleted && stars >= 1);
            SetStar(_star2, isCompleted && stars >= 2);
            SetStar(_star3, isCompleted && stars >= 3);

            // ── Lock icon ──
            if (_lockIcon != null)
                _lockIcon.SetActive(isLocked);

            // ── Background color ──
            if (_background != null)
            {
                if (isCurrent) _background.color = _currentColor;
                else if (isCompleted) _background.color = _completedColor;
                else _background.color = _lockedColor;
            }

            // ── Button interactable ──
            if (_button != null)
                _button.interactable = isCurrent;

            // ── Current level pulse ──
            _pulseTween?.Kill();
            if (isCurrent)
            {
                Sequence sq = DOTween.Sequence();
                transform.localScale = Vector3.one * 1f;
                sq.Append(transform.DOScale(1.15f, 0.6f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .Append(transform.DOScale(1f, 0.6f).SetEase(Ease.InOutSine).SetUpdate(true));
                _pulseTween = sq;
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
