using System.Collections.Generic;
using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.Level
{
    [CreateAssetMenu(fileName = "Level_", menuName = "BulletRoute/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public int LevelIndex;
        public string LevelName;
        public int WorldIndex;

        [Header("Grid")]
        public int GridWidth = 5;
        public int GridHeight = 5;

        [Header("Star Thresholds (moves)")]
        public int ThreeStar = 5;
        public int TwoStar = 8;
        public int OneStar = 12;

        [Header("Tiles")]
        public List<TilePlacement> Tiles = new List<TilePlacement>();

        [Header("Turrets")]
        public List<TurretPlacement> Turrets = new List<TurretPlacement>();

        [Header("Targets")]
        public List<TargetPlacement> Targets = new List<TargetPlacement>();

        public int TargetCount => Targets.Count;

        public int CalculateStars(int moveCount)
        {
            if (moveCount <= ThreeStar) return 3;
            if (moveCount <= TwoStar) return 2;
            if (moveCount <= OneStar) return 1;
            return 1; // minimum 1 star for completion
        }
    }

    [System.Serializable]
    public class TilePlacement
    {
        public Vector2Int Position;
        public TileType Type;
        public int Rotation;
        public bool IsLocked;
        [Tooltip("Portal ID for portal tiles, mirror type for mirror tiles")]
        public int ExtraData;
    }

    [System.Serializable]
    public class TurretPlacement
    {
        public Vector2Int Position;
        public Direction FireDirection;
    }

    [System.Serializable]
    public class TargetPlacement
    {
        public Vector2Int Position;
    }
}
