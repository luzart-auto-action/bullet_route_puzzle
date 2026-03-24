using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Bullet
{
    public class BulletController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private TrailRenderer _trail;

        [Header("FX Points")]
        [SerializeField] private Transform _fxFront;
        [SerializeField] private Transform _fxCenter;
        [SerializeField] private Transform _fxTrail;

        [Header("Animation")]
        [SerializeField] private float _moveSpeed = 0.5f;
        [SerializeField] private Ease _moveEase = Ease.InOutQuad;
        [SerializeField] private float _spawnScaleTime = 0.2f;
        [SerializeField] private float _despawnScaleTime = 0.15f;
        [SerializeField] private float _pulseScale = 0.1f;
        [SerializeField] private float _pulseDuration = 0.3f;
        [SerializeField] private float _rotationSmooth = 0.3f;

        private bool _isActive;

        public float MoveSpeed => _moveSpeed;
        public bool IsActive => _isActive;

        // ════════════════════════════════════════
        //  INIT
        // ════════════════════════════════════════

        public void Initialize(Vector3 startPos, Direction startDir)
        {
            DOTween.Kill(transform); // kill everything from previous use
            transform.position = startPos;
            transform.eulerAngles = new Vector3(0, DirectionHelper.ToAngle(startDir), 0);
            if (_visualRoot != null) _visualRoot.localPosition = Vector3.zero;
            if (_trail != null) _trail.Clear();

            _isActive = true;
            AnimateSpawn();
        }

        private void AnimateSpawn()
        {
            if (_visualRoot == null) return;
            _visualRoot.localScale = Vector3.zero;
            DOTween.Sequence()
                .SetTarget(transform)
                .Append(_visualRoot.DOScale(Vector3.one * 1.2f, _spawnScaleTime * 0.6f).SetEase(Ease.OutBack))
                .Append(_visualRoot.DOScale(Vector3.one, _spawnScaleTime * 0.4f).SetEase(Ease.InOutQuad))
                .Append(_visualRoot.DOScale(Vector3.one * (1f + _pulseScale), _pulseDuration)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        }

        // ════════════════════════════════════════
        //  MOVEMENT — pure position tween, no side effects
        // ════════════════════════════════════════

        /// <summary>
        /// Returns a DOMove tween. Does NOT change direction.
        /// Direction must be set separately via SetDirection callback in the sequence.
        /// </summary>
        public Tween MoveTo(Vector3 targetPos)
        {
            return transform.DOMove(targetPos, _moveSpeed).SetEase(_moveEase);
        }

        /// <summary>
        /// Rotate bullet to face direction. Call from AppendCallback in the sequence
        /// so it fires at the right time, not during BUILD phase.
        /// </summary>
        public void SetDirection(Direction dir)
        {
            float angle = DirectionHelper.ToAngle(dir);
            transform.DORotate(new Vector3(0, angle, 0), _rotationSmooth)
                .SetTarget(transform)
                .SetEase(Ease.OutQuad);
        }

        // ════════════════════════════════════════
        //  END ANIMATIONS
        // ════════════════════════════════════════

        public void AnimateHitTarget()
        {
            _isActive = false;
            if (_visualRoot == null) return;

            DOTween.Sequence()
                .SetTarget(transform)
                .Append(_visualRoot.DOScale(Vector3.one * 1.5f, 0.15f).SetEase(Ease.OutQuad))
                .Append(_visualRoot.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    EventBus.Publish(new FXRequestEvent
                    {
                        FXName = "BulletHitTarget",
                        Position = transform.position,
                        Rotation = Quaternion.identity
                    });
                });
        }

        public void AnimateStop()
        {
            _isActive = false;
            if (_visualRoot == null) return;

            DOTween.Sequence()
                .SetTarget(transform)
                .Append(_visualRoot.DOShakePosition(0.3f, 0.1f, 10, 90f))
                .Append(_visualRoot.DOScale(Vector3.zero, _despawnScaleTime).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    EventBus.Publish(new FXRequestEvent
                    {
                        FXName = "BulletStop",
                        Position = transform.position,
                        Rotation = Quaternion.identity
                    });
                });
        }

        public void AnimateTeleport(Vector3 toPos, System.Action onComplete)
        {
            if (_visualRoot == null) return;

            DOTween.Sequence()
                .SetTarget(transform)
                .Append(_visualRoot.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                .AppendCallback(() =>
                {
                    transform.position = toPos;
                    if (_trail != null) _trail.Clear();
                })
                .Append(_visualRoot.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack))
                .OnComplete(() => onComplete?.Invoke());
        }

        // ════════════════════════════════════════
        //  CLEANUP — single point: DOTween.Kill(transform)
        // ════════════════════════════════════════

        public void Deactivate()
        {
            _isActive = false;
            DOTween.Kill(transform);
            if (_trail != null) _trail.Clear();
        }

        private void OnDisable() => DOTween.Kill(transform);
        private void OnDestroy() => DOTween.Kill(transform);
    }
}
