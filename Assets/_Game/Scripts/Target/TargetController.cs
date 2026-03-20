using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Tile;
using System.Collections.Generic;

namespace BulletRoute.Target
{
    public class TargetController : TileBase
    {
        [Header("Target Settings")]
        [SerializeField] private int _targetIndex;
        [SerializeField] private bool _isHit;

        [Header("Target Visual")]
        [SerializeField] private Transform _targetRing;
        [SerializeField] private Transform _targetCenter;

        [Header("Target Animation")]
        [SerializeField] private float _ringRotateSpeed = 45f;
        [SerializeField] private float _hitExplosionScale = 2f;
        [SerializeField] private float _hitDuration = 0.5f;
        [SerializeField] private float _pulseFrequency = 1.5f;

        private Tween _ringRotation;
        private Tween _beaconPulse;

        public int TargetIndex => _targetIndex;
        public bool IsHit => _isHit;

        protected override void Start()
        {
            base.Start();
            _tileType = TileType.Target;
            _isFixed = true; // Targets cannot be moved

            StartTargetAnimations();
        }

        private void StartTargetAnimations()
        {
            // Rotate ring
            if (_targetRing != null)
            {
                _ringRotation = _targetRing.DOLocalRotate(new Vector3(0, 360, 0), 360f / _ringRotateSpeed, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1);
            }

            // Pulse beacon
            if (_targetCenter != null)
            {
                _beaconPulse = _targetCenter.DOScale(Vector3.one * 1.2f, _pulseFrequency)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            // Target accepts bullets from any direction
            return new List<Direction>();
        }

        public void OnHit()
        {
            if (_isHit) return;
            _isHit = true;

            _ringRotation?.Kill();
            _beaconPulse?.Kill();

            AnimateHit();
        }

        private void AnimateHit()
        {
            var target = _visualRoot != null ? _visualRoot : transform;

            Sequence hitSeq = DOTween.Sequence();

            // Explosion scale
            hitSeq.Append(target.DOScale(Vector3.one * _hitExplosionScale, _hitDuration * 0.3f).SetEase(Ease.OutQuad));

            // Flash (via punch)
            hitSeq.Join(target.DOPunchRotation(new Vector3(0, 0, 30f), _hitDuration * 0.3f, 5, 0.5f));

            // Shrink with bounce
            hitSeq.Append(target.DOScale(Vector3.one * 0.5f, _hitDuration * 0.4f).SetEase(Ease.InBack));
            hitSeq.Append(target.DOScale(Vector3.one * 0.8f, _hitDuration * 0.3f).SetEase(Ease.OutBounce));

            // Particles
            EventBus.Publish(new FXRequestEvent
            {
                FXName = "TargetHit",
                Position = _fxSpawnCenter.position,
                Rotation = Quaternion.identity
            });

            EventBus.Publish(new PlaySFXEvent { ClipName = "TargetHit" });
            EventBus.Publish(new CameraShakeEvent { Intensity = 0.3f, Duration = 0.2f });
        }

        public override void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            OnHit();
        }

        public void ResetTarget()
        {
            _isHit = false;
            var target = _visualRoot != null ? _visualRoot : transform;
            target.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            StartTargetAnimations();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _ringRotation?.Kill();
            _beaconPulse?.Kill();
        }
    }
}
