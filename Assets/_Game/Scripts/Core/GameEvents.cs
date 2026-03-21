using UnityEngine;

namespace BulletRoute.Core
{
    // === Grid Events ===
    public struct GridReadyEvent : IGameEvent
    {
        public int Width;
        public int Height;
    }

    // === Tile Events ===
    public struct TileRotatedEvent : IGameEvent
    {
        public Vector2Int GridPos;
        public int NewRotation;
    }

    public struct TileSwappedEvent : IGameEvent
    {
        public Vector2Int FromPos;
        public Vector2Int ToPos;
    }

    public struct TilePlacedEvent : IGameEvent
    {
        public Vector2Int GridPos;
    }

    // === Bullet Events ===
    public struct BulletFiredEvent : IGameEvent
    {
        public Vector3 StartPosition;
        public Vector2Int GridPos;
    }

    public struct BulletMovedEvent : IGameEvent
    {
        public Vector2Int FromPos;
        public Vector2Int ToPos;
        public Vector3 WorldPosition;
    }

    public struct BulletHitTargetEvent : IGameEvent
    {
        public Vector2Int TargetPos;
        public int TargetIndex;
    }

    public struct BulletStoppedEvent : IGameEvent
    {
        public Vector2Int LastPos;
        public BulletStopReason Reason;
    }

    public struct BulletSplitEvent : IGameEvent
    {
        public Vector2Int SplitPos;
    }

    public struct BulletTeleportedEvent : IGameEvent
    {
        public Vector2Int FromPortal;
        public Vector2Int ToPortal;
    }

    public struct BombExplodedEvent : IGameEvent
    {
        public Vector2Int BombPos;
    }

    public struct BulletAbsorbedEvent : IGameEvent
    {
        public Vector2Int AbsorbPos;
    }

    public enum BulletStopReason
    {
        OutOfGrid,
        HitBlock,
        HitWall,
        NoPath,
        Absorbed
    }

    // === Game State Events ===
    public struct GameStateChangedEvent : IGameEvent
    {
        public GameStateType PreviousState;
        public GameStateType NewState;
    }

    public struct LevelStartedEvent : IGameEvent
    {
        public int LevelIndex;
    }

    public struct LevelCompletedEvent : IGameEvent
    {
        public int LevelIndex;
        public int Stars;
        public int MoveCount;
        public float TimeRemaining;
        public float TimeLimit;
    }

    public struct LevelFailedEvent : IGameEvent
    {
        public int LevelIndex;
    }

    public struct LevelResetEvent : IGameEvent
    {
        public int LevelIndex;
    }

    // === Player Input Events ===
    public struct PlayerMoveEvent : IGameEvent
    {
        public MoveType Type;
        public Vector2Int Position;
    }

    public struct PlayButtonPressedEvent : IGameEvent { }
    public struct ResetButtonPressedEvent : IGameEvent { }
    public struct GoToMainMenuEvent : IGameEvent { }
    public struct HintRequestedEvent : IGameEvent { }

    // === Timer Events ===
    public struct TimerStartedEvent : IGameEvent
    {
        public float TimeLimit;
    }

    public struct TimerTickEvent : IGameEvent
    {
        public float TimeRemaining;
        public float TimeLimit;
    }

    public struct TimerExpiredEvent : IGameEvent
    {
        public int LevelIndex;
    }

    public enum MoveType
    {
        Rotate,
        Swap
    }

    public enum GameStateType
    {
        MainMenu,
        Loading,
        Setup,
        Playing,
        Simulating,
        Win,
        Fail,
        Paused
    }

    // === FX Events ===
    public struct FXRequestEvent : IGameEvent
    {
        public string FXName;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    // === UI Events ===
    public struct ShowPanelEvent : IGameEvent
    {
        public string PanelName;
    }

    public struct HidePanelEvent : IGameEvent
    {
        public string PanelName;
    }

    // === Audio Events ===
    public struct PlaySFXEvent : IGameEvent
    {
        public string ClipName;
    }

    public struct PlayMusicEvent : IGameEvent
    {
        public string TrackName;
    }

    // === Camera Events ===
    public struct CameraShakeEvent : IGameEvent
    {
        public float Intensity;
        public float Duration;
    }

    public struct CameraFocusEvent : IGameEvent
    {
        public Vector3 TargetPosition;
        public float Duration;
    }
}
