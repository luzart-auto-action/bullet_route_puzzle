using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float _defaultHeight = 10f;
        [SerializeField] private float _defaultAngle = 60f; // top-down angle
        [SerializeField] private float _padding = 2f;

        [Header("Animation")]
        [SerializeField] private float _focusDuration = 0.5f;
        [SerializeField] private Ease _focusEase = Ease.InOutQuad;
        [SerializeField] private float _zoomDuration = 0.3f;

        [Header("Shake")]
        [SerializeField] private float _defaultShakeIntensity = 0.3f;
        [SerializeField] private float _defaultShakeDuration = 0.2f;

        private Tween _moveTween;
        private Tween _shakeTween;
        private Vector3 _originalPosition;

        private void Awake()
        {
            ServiceLocator.Register(this);
            _originalPosition = transform.position;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CameraShakeEvent>(OnShakeRequested);
            EventBus.Subscribe<CameraFocusEvent>(OnFocusRequested);
            EventBus.Subscribe<GridReadyEvent>(OnGridReady);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CameraShakeEvent>(OnShakeRequested);
            EventBus.Unsubscribe<CameraFocusEvent>(OnFocusRequested);
            EventBus.Unsubscribe<GridReadyEvent>(OnGridReady);
        }

        private void OnGridReady(GridReadyEvent evt)
        {
            FitGridInView(evt.Width, evt.Height);
        }

        public void FitGridInView(int gridWidth, int gridHeight)
        {
            var gridManager = ServiceLocator.Get<Grid.GridManager>();
            if (gridManager == null) return;

            float totalSize = Mathf.Max(gridWidth, gridHeight) * gridManager.TotalCellSize + _padding;

            // Calculate height needed for orthographic or perspective
            float height = totalSize * 0.8f + _defaultHeight * 0.5f;
            Vector3 targetPos = new Vector3(0, height, -height * 0.5f);

            _moveTween?.Kill();
            _moveTween = transform.DOMove(targetPos, _focusDuration).SetEase(_focusEase);
            transform.DORotate(new Vector3(_defaultAngle, 0, 0), _focusDuration).SetEase(_focusEase);

            _originalPosition = targetPos;
        }

        public void FocusOn(Vector3 position, float duration = -1f)
        {
            if (duration < 0) duration = _focusDuration;

            _moveTween?.Kill();
            Vector3 targetPos = new Vector3(position.x, _originalPosition.y, position.z - _originalPosition.y * 0.5f);
            _moveTween = transform.DOMove(targetPos, duration).SetEase(_focusEase);
        }

        public void Shake(float intensity = -1f, float duration = -1f)
        {
            if (intensity < 0) intensity = _defaultShakeIntensity;
            if (duration < 0) duration = _defaultShakeDuration;

            _shakeTween?.Kill();
            _shakeTween = transform.DOShakePosition(duration, intensity, 10, 90f, false, true)
                .OnComplete(() => transform.position = _originalPosition);
        }

        public void ZoomIn(float amount, float duration = -1f)
        {
            if (duration < 0) duration = _zoomDuration;
            _moveTween?.Kill();
            Vector3 targetPos = _originalPosition + transform.forward * amount;
            _moveTween = transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad);
        }

        public void ZoomOut(float duration = -1f)
        {
            if (duration < 0) duration = _zoomDuration;
            _moveTween?.Kill();
            _moveTween = transform.DOMove(_originalPosition, duration).SetEase(Ease.OutQuad);
        }

        private void OnShakeRequested(CameraShakeEvent evt)
        {
            Shake(evt.Intensity, evt.Duration);
        }

        private void OnFocusRequested(CameraFocusEvent evt)
        {
            FocusOn(evt.TargetPosition, evt.Duration);
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _shakeTween?.Kill();
            ServiceLocator.Unregister<CameraController>();
        }
    }
}
