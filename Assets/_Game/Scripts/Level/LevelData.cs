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

        [Header("Timer")]
        public float TimeLimit = 60f;

        [Header("Star Thresholds (time remaining)")]
        public float ThreeStarTime = 40f;
        public float TwoStarTime = 20f;

        [Header("Star Thresholds (moves - legacy)")]
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
            return 1;
        }

        public int CalculateStarsByTime(float timeRemaining)
        {
            if (timeRemaining >= ThreeStarTime) return 3;
            if (timeRemaining >= TwoStarTime) return 2;
            return 1;
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
