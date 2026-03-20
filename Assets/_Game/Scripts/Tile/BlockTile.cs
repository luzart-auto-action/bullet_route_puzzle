using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class BlockTile : TileBase
    {
        [Header("Block FX")]
        [SerializeField] private float _shakeIntensity = 0.3f;
        [SerializeField] private float _shakeDuration = 0.4f;

        // Block tile: stops bullet completely
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            return new List<Direction>(); // No exits
        }

        public override void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            // Block shakes when hit
            var target = _visualRoot != null ? _visualRoot : transform;
            target.DOShakePosition(_shakeDuration, _shakeIntensity, 10, 90f, false, true);
            target.DOShakeRotation(_shakeDuration, _shakeIntensity * 10f, 10, 90f);

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "BlockHit",
                Position = GetFXSpawnPoint(entryDir).position,
                Rotation = UnityEngine.Quaternion.identity
            });
        }
    }
}
