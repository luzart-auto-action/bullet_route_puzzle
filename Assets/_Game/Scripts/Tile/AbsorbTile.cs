using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class AbsorbTile : TileBase
    {
        [Header("Absorb Animation")]
        [SerializeField] private float _absorbDuration = 0.4f;

        // Absorb tile: swallows the bullet (no exits)
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            return new List<Direction>(); // Absorbs - no exits
        }

        public override void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            var target = _visualRoot != null ? _visualRoot : transform;

            DOTween.Sequence()
                .Append(target.DOScale(Vector3.one * 1.3f, _absorbDuration * 0.4f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(Vector3.one * 0.8f, _absorbDuration * 0.3f).SetEase(Ease.InQuad))
                .Append(target.DOScale(Vector3.one, _absorbDuration * 0.3f).SetEase(Ease.OutBounce));

            EventBus.Publish(new BulletAbsorbedEvent { AbsorbPos = GridPosition });

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "BulletAbsorb",
                Position = _fxSpawnCenter.position,
                Rotation = Quaternion.identity
            });
        }
    }
}
