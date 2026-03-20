using UnityEngine;
using DG.Tweening;

namespace BulletRoute.UI
{
    public class UIPanel : MonoBehaviour
    {
        [Header("Panel Settings")]
        [SerializeField] private string _panelName;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Show Animation")]
        [SerializeField] private float _showDuration = 0.4f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Vector3 _showFromScale = Vector3.one * 0.5f;
        [SerializeField] private float _showFromAlpha = 0f;

        [Header("Hide Animation")]
        [SerializeField] private float _hideDuration = 0.25f;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private Sequence _currentAnimation;

        public string PanelName => _panelName;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            _currentAnimation?.Kill();
            gameObject.SetActive(true);

            transform.localScale = _showFromScale;
            _canvasGroup.alpha = _showFromAlpha;
            _canvasGroup.interactable = false;

            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one, _showDuration).SetEase(_showEase))
                .Join(_canvasGroup.DOFade(1f, _showDuration * 0.7f))
                .OnComplete(() => _canvasGroup.interactable = true)
                .SetUpdate(true);
        }

        public virtual void Hide()
        {
            _currentAnimation?.Kill();
            _canvasGroup.interactable = false;

            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScale(_showFromScale, _hideDuration).SetEase(_hideEase))
                .Join(_canvasGroup.DOFade(0f, _hideDuration))
                .OnComplete(() => gameObject.SetActive(false))
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            _currentAnimation?.Kill();
        }
    }
}
