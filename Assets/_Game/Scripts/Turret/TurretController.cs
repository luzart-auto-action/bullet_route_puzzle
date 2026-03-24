using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Tile;
using System.Collections.Generic;

namespace BulletRoute.Turret
{
    public class TurretController : TileBase
    {
        [Header("Turret Settings")]
        [SerializeField] private Direction _fireDirection = Direction.Right;
        [SerializeField] private Transform _barrel;
        [SerializeField] private Transform _muzzlePoint;

        [Header("Turret FX")]
        [SerializeField] private Transform _fxMuzzleFlash;
        [SerializeField] private Transform _fxRecoil;

        [Header("Turret Animation")]
        [SerializeField] private float _recoilDistance = 0.15f;
        [SerializeField] private float _recoilDuration = 0.15f;
        [SerializeField] private float _chargeUpDuration = 0.3f;
        [SerializeField] private float _chargeScale = 1.2f;

        public Direction FireDirection => _fireDirection;
        public Transform MuzzlePoint => _muzzlePoint;

        public void SetFireDirection(Direction dir)
        {
            _fireDirection = dir;
            var posY = DirectionHelper.ToAngle(dir);
            VisualRoot.eulerAngles = new Vector3(0, posY, 0);
        }

        protected override void Start()
        {
            base.Start();
            _tileType = TileType.Turret;
            _isFixed = true; // Turrets cannot be moved
            //VisualRoot.eulerAngles = new Vector3(0, DirectionHelper.ToAngle(_fireDirection), 0);
        }

        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            // Turret doesn't route bullets through it
            return new List<Direction>();
        }

        public void AnimateChargeUp(System.Action onComplete = null)
        {
            var target = _visualRoot != null ? _visualRoot : transform;

            DOTween.Sequence()
                .Append(target.DOScale(Vector3.one * _chargeScale, _chargeUpDuration).SetEase(Ease.InQuad))
                .Append(target.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack))
                .OnComplete(() => onComplete?.Invoke());

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "TurretCharge",
                Position = _muzzlePoint != null ? _muzzlePoint.position : transform.position,
                Rotation = Quaternion.identity
            });
        }

        public void AnimateFire()
        {
            if (_barrel != null)
            {
                Vector2Int dir = -DirectionHelper.ToVector(_fireDirection);
                Vector3 recoilDir = new Vector3(dir.x, 0, dir.y);
                Vector3 recoil3 = new Vector3(recoilDir.x, 0, recoilDir.y) * _recoilDistance;

                DOTween.Sequence()
                    .Append(_barrel.DOLocalMove(recoil3, _recoilDuration * 0.3f).SetEase(Ease.OutQuad))
                    .Append(_barrel.DOLocalMove(Vector3.zero, _recoilDuration * 0.7f).SetEase(Ease.OutElastic));
            }

            // Muzzle flash punch scale
            var target = _visualRoot != null ? _visualRoot : transform;
            target.DOPunchScale(Vector3.one * 0.15f, 0.2f, 3, 0.5f);

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "MuzzleFlash",
                Position = _muzzlePoint != null ? _muzzlePoint.position : transform.position,
                Rotation = Quaternion.Euler(0, DirectionHelper.ToAngle(_fireDirection), 0)
            });

            EventBus.Publish(new PlaySFXEvent { ClipName = "TurretFire" });
            EventBus.Publish(new CameraShakeEvent { Intensity = 0.2f, Duration = 0.15f });
        }
    }
}
