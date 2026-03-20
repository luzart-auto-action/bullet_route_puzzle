using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class BombTile : TileBase, IDestructible
    {
        [Header("Bomb Settings")]
        [SerializeField] private float _explosionDelay = 0.2f;
        [SerializeField] private float _explosionScale = 2f;

        private bool _hasExploded;

        // Bomb: bullet passes through and destroys adjacent blocks
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            var exits = new List<Direction>();
            exits.Add(DirectionHelper.Opposite(entryDirection));
            return exits;
        }

        public void OnBulletHit()
        {
            if (_hasExploded) return;
            _hasExploded = true;
            AnimateExplosion();
        }

        public override void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            OnBulletHit();
        }

        private void AnimateExplosion()
        {
            var target = _visualRoot != null ? _visualRoot : transform;

            DOTween.Sequence()
                .AppendInterval(_explosionDelay)
                .Append(target.DOScale(Vector3.one * _explosionScale, 0.2f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    EventBus.Publish(new BombExplodedEvent { BombPos = GridPosition });
                    EventBus.Publish(new FXRequestEvent
                    {
                        FXName = "Explosion",
                        Position = _fxSpawnCenter.position,
                        Rotation = Quaternion.identity
                    });
                });
        }
    }
}
