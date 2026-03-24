using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class MirrorTile : TileBase
    {
        [Header("Mirror Settings")]
        [SerializeField] private bool _isForwardSlash = true; // / vs \

        [Header("Mirror FX")]
        [SerializeField] private float _flashDuration = 0.2f;

        public void SetMirrorType(bool isForwardSlash)
        {
            _isForwardSlash = isForwardSlash;
        }

        // Mirror: reflects bullet
        // Forward slash /: Up->Right, Right->Up, Down->Left, Left->Down
        // Back slash \: Up->Left, Left->Up, Down->Right, Right->Down
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            Direction localEntry = ReverseRotation(entryDirection);
            var exits = new List<Direction>();

//            if (_isForwardSlash)
            {
                switch (localEntry)
                {
                    case Direction.Up: exits.Add(ApplyRotation(Direction.Right)); break;
                    case Direction.Right: exits.Add(ApplyRotation(Direction.Up)); break;
                    case Direction.Down: exits.Add(ApplyRotation(Direction.Left)); break;
                    case Direction.Left: exits.Add(ApplyRotation(Direction.Down)); break;
                }
            }
            //else
            //{
            //    switch (localEntry)
            //    {
            //        case Direction.Up: exits.Add(ApplyRotation(Direction.Left)); break;
            //        case Direction.Left: exits.Add(ApplyRotation(Direction.Up)); break;
            //        case Direction.Down: exits.Add(ApplyRotation(Direction.Right)); break;
            //        case Direction.Right: exits.Add(ApplyRotation(Direction.Down)); break;
            //    }
            //}

            return exits;
        }

        public override void AnimateBulletPass(Direction entryDir, Direction exitDir)
        {
            base.AnimateBulletPass(entryDir, exitDir);
            var target = _visualRoot != null ? _visualRoot : transform;

            // Flash effect for mirror reflection
            DOTween.Sequence()
                .Append(target.DOScale(Vector3.one * 1.2f, _flashDuration * 0.5f).SetEase(Ease.OutFlash))
                .Append(target.DOScale(Vector3.one, _flashDuration * 0.5f).SetEase(Ease.InFlash));

            EventBus.Publish(new FXRequestEvent
            {
                FXName = "MirrorReflect",
                Position = _fxSpawnCenter.position,
                Rotation = Quaternion.identity
            });
        }
    }
}
