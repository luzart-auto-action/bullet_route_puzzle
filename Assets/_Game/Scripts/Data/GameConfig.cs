using UnityEngine;
using DG.Tweening;

namespace BulletRoute.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BulletRoute/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        public float CellSize = 1.2f;
        public float CellSpacing = 0.1f;

        [Header("Bullet")]
        public float BulletSpeedPerTile = 0.5f;
        public float BulletSpawnDelay = 0.3f;
        public int MaxBulletSteps = 100;

        [Header("Animation Timing")]
        public float TileRotateDuration = 0.25f;
        public float TileSwapDuration = 0.3f;
        public float GridAppearDelay = 0.03f;
        public float LevelTransitionDuration = 0.5f;

        [Header("Animation Easing")]
        public Ease TileRotateEase = Ease.OutBack;
        public Ease TileSwapEase = Ease.OutQuad;
        public Ease GridAppearEase = Ease.OutBack;

        [Header("Gameplay")]
        public float AutoResetDelay = 1.5f;
        public float WinPanelDelay = 0.5f;

        [Header("Camera")]
        public float CameraHeight = 10f;
        public float CameraAngle = 60f;
        public float CameraPadding = 2f;
    }
}
