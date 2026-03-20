using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class PortalTile : TileBase
    {
        [Header("Portal Settings")]
        [SerializeField] private int _portalId; // Matching portal ID
        [SerializeField] private Color _portalColor = Color.cyan;

        [Header("Portal Animation")]
        [SerializeField] private float _portalSpinSpeed = 90f;
        [SerializeField] private float _teleportDuration = 0.3f;

        private Tween _spinTween;

        public int PortalId => _portalId;
        public Color PortalColor => _portalColor;

        public void SetPortalId(int id)
        {
            _portalId = id;
        }

        protected override void Start()
        {
            base.Start();
            StartPortalSpin();
        }

        private void StartPortalSpin()
        {
            _spinTween?.Kill();
            if (_visualRoot != null)
            {
                _spinTween = _visualRoot.DOLocalRotate(new Vector3(0, 360, 0), 360f / _portalSpinSpeed, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1);
            }
        }

        // Portal: bullet enters and exits from paired portal
        // Returns the same direction (bullet continues in same direction from paired portal)
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            var exits = new List<Direction>();
            exits.Add(entryDirection);
            return exits;
        }

        public void AnimateTeleportIn()
        {
            var target = _visualRoot != null ? _visualRoot : transform;
            DOTween.Sequence()
                .Append(target.DOScale(Vector3.one * 1.5f, _teleportDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(Vector3.one, _teleportDuration * 0.5f).SetEase(Ease.InQuad));

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "PortalIn",
                Position = _fxSpawnCenter.position,
                Rotation = Quaternion.identity
            });
        }

        public void AnimateTeleportOut()
        {
            var target = _visualRoot != null ? _visualRoot : transform;
            DOTween.Sequence()
                .Append(target.DOScale(Vector3.zero, _teleportDuration * 0.3f).SetEase(Ease.InBack))
                .Append(target.DOScale(Vector3.one, _teleportDuration * 0.7f).SetEase(Ease.OutBack));

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "PortalOut",
                Position = _fxSpawnCenter.position,
                Rotation = Quaternion.identity
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _spinTween?.Kill();
        }
    }
}
