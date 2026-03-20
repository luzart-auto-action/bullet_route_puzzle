using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class SplitterTile : TileBase
    {
        // Splitter: bullet splits into 2 perpendicular directions
        // Entry from any direction -> exits to the 2 perpendicular directions
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            var exits = new List<Direction>();
            // Split to perpendicular directions
            Direction left = DirectionHelper.RotateCCW(entryDirection);
            Direction right = DirectionHelper.RotateCW(entryDirection);
            exits.Add(left);
            exits.Add(right);
            return exits;
        }

        public override void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            var target = _visualRoot != null ? _visualRoot : transform;
            DOTween.Sequence()
                .Append(target.DOPunchScale(Vector3.one * 0.2f, 0.4f, 3, 0.5f))
                .Join(target.DOPunchRotation(new Vector3(0, 0, 10f), 0.4f, 5, 0.5f));

            EventBus.Publish(new BulletSplitEvent { SplitPos = GridPosition });

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "BulletSplit",
                Position = _fxSpawnCenter.position,
                Rotation = Quaternion.identity
            });
        }
    }
}
