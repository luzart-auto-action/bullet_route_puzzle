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
        [SerializeField] private float _moveSpeed = 0.5f; // seconds per tile
        [SerializeField] private Ease _moveEase = Ease.InOutQuad;
        [SerializeField] private float _spawnScaleTime = 0.2f;
        [SerializeField] private float _despawnScaleTime = 0.15f;
        [SerializeField] private float _pulseScale = 0.1f;
        [SerializeField] private float _pulseDuration = 0.3f;
        [SerializeField] private float _rotationSmooth = 0.3f;

        private Tween _moveTween;
        private Tween _pulseTween;
        private Tween _rotationTween;
        private Sequence _spawnSequence;
        private bool _isActive;

        public float MoveSpeed => _moveSpeed;
        public bool IsActive => _isActive;
        public Transform FXFront => _fxFront;
        public Transform FXCenter => _fxCenter;

        public void Initialize(Vector3 startPos, Direction startDir)
        {
            transform.position = startPos;
            SetDirection(startDir, false);

            if (_trail != null) _trail.Clear();

            _isActive = true;
            AnimateSpawn();
            StartPulse();
        }

        private void AnimateSpawn()
        {
            if (_visualRoot == null) return;
            _spawnSequence?.Kill();
            _visualRoot.localScale = Vector3.zero;
            _spawnSequence = DOTween.Sequence()
                .Append(_visualRoot.DOScale(Vector3.one * 1.2f, _spawnScaleTime * 0.6f).SetEase(Ease.OutBack))
                .Append(_visualRoot.DOScale(Vector3.one, _spawnScaleTime * 0.4f).SetEase(Ease.InOutQuad));
        }

        private void StartPulse()
        {
            _pulseTween?.Kill();
            if (_visualRoot == null) return;
            _pulseTween = _visualRoot.DOScale(Vector3.one * (1f + _pulseScale), _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public Tween MoveTo(Vector3 targetPos, Direction dir)
        {
            _moveTween?.Kill();

            SetDirection(dir, true);

            _moveTween = transform.DOMove(targetPos, _moveSpeed)
                .SetEase(_moveEase);

            return _moveTween;
        }

        public void SetDirection(Direction dir, bool animate)
        {
            float angle = DirectionHelper.ToAngle(dir);
            Vector3 targetRot = new Vector3(0, angle, 0);

            if (animate)
            {
                _rotationTween?.Kill();
                _rotationTween = transform.DORotate(targetRot, _rotationSmooth).SetEase(Ease.OutQuad);
            }
            else
            {
                transform.eulerAngles = targetRot;
            }
        }

        public void AnimateHitTarget()
        {
            _pulseTween?.Kill();
            _moveTween?.Kill();

            // Mark inactive IMMEDIATELY
            _isActive = false;

            if (_visualRoot == null) return;

            DOTween.Sequence()
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
            _pulseTween?.Kill();
            _moveTween?.Kill();

            // Mark inactive IMMEDIATELY so BulletSimulator can detect all bullets are done
            _isActive = false;

            if (_visualRoot == null) return;

            DOTween.Sequence()
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
            _moveTween?.Kill();
            if (_visualRoot == null) return;

            DOTween.Sequence()
                .Append(_visualRoot.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                .AppendCallback(() =>
                {
                    transform.position = toPos;
                    if (_trail != null) _trail.Clear();
                })
                .Append(_visualRoot.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void Deactivate()
        {
            _isActive = false;
            KillAllTweens();
            if (_trail != null) _trail.Clear();
        }

        private void KillAllTweens()
        {
            _moveTween?.Kill();
            _pulseTween?.Kill();
            _rotationTween?.Kill();
            _spawnSequence?.Kill();
        }

        private void OnDisable()
        {
            KillAllTweens();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }
    }
}
