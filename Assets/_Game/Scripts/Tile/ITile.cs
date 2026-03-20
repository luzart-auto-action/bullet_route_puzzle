using BulletRoute.Core;
using System.Collections.Generic;
using UnityEngine;

namespace BulletRoute.Tile
{
    public interface IBulletRouter
    {
        List<Direction> GetExitDirections(Direction entryDirection);
    }

    public interface IRotatable
    {
        int RotationState { get; }
        void Rotate(int steps = 1);
        bool CanRotate { get; }
    }

    public interface IDraggable
    {
        bool CanDrag { get; }
    }

    public interface IDestructible
    {
        void OnBulletHit();
    }
}
