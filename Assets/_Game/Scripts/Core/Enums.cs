using UnityEngine;

namespace BulletRoute.Core
{
    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public enum TileType
    {
        Empty,
        Straight,
        Corner,
        Cross,
        Block,
        Mirror,
        Splitter,
        Portal,
        Bomb,
        Absorb,
        Turret,
        Target
    }

    public static class DirectionHelper
    {
        public static Direction Opposite(Direction dir)
        {
            return (Direction)(((int)dir + 2) % 4);
        }

        public static Direction RotateCW(Direction dir)
        {
            return (Direction)(((int)dir + 1) % 4);
        }

        public static Direction RotateCCW(Direction dir)
        {
            return (Direction)(((int)dir + 3) % 4);
        }

        /// <summary>
        /// Returns the grid-step offset for the direction.
        /// Up = (0,1), Right = (1,0), Down = (0,-1), Left = (-1,0).
        /// Note: In 3D world space, Y maps to Z axis via GridManager.
        /// </summary>
        public static Vector2Int ToVector(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return Vector2Int.up;
                case Direction.Right:  return Vector2Int.right;
                case Direction.Down:   return Vector2Int.down;
                case Direction.Left:   return Vector2Int.left;
                default:               return Vector2Int.zero;
            }
        }

        /// <summary>
        /// Returns a unit Vector2 for the direction.
        /// Useful for smooth position math (e.g. Lerp, velocity).
        /// </summary>
        public static Vector2 ToVector2(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return Vector2.up;
                case Direction.Right:  return Vector2.right;
                case Direction.Down:   return Vector2.down;
                case Direction.Left:   return Vector2.left;
                default:               return Vector2.zero;
            }
        }

        public static Direction FromVector(Vector2Int v)
        {
            if (v == Vector2Int.up)    return Direction.Up;
            if (v == Vector2Int.right) return Direction.Right;
            if (v == Vector2Int.down)  return Direction.Down;
            if (v == Vector2Int.left)  return Direction.Left;
            return Direction.Up;
        }

        /// <summary>
        /// Returns the Y-axis rotation angle for 3D top-down view.
        /// Up = 0, Right = -90, Down = -180, Left = -270.
        /// Apply as: Quaternion.Euler(0, ToAngle(dir), 0)
        /// </summary>
        public static float ToAngle(Direction dir)
        {
            return (int)dir * -90f;
        }
    }
}
