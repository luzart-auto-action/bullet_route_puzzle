using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Grid;
using BulletRoute.Tile;
using BulletRoute.Level;
using BulletRoute.Timer;
using BulletRoute.GameState;

namespace BulletRoute.Bullet
{
    public class BulletSimulator : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] private float _stepDelay = 0.05f;
        [SerializeField] private int _maxSteps = 100;

        private GridManager _gridManager;
        private BulletManager _bulletManager;
        private bool _isSimulating;
        private int _targetsRemaining;
        private int _totalTargets;
        private List<BulletController> _activeBullets = new List<BulletController>();
        private Tween _endDelayTween;

        public bool IsSimulating => _isSimulating;

        // ════════════════════════════════════════
        //  DATA — no visuals, no DOTween
        // ════════════════════════════════════════

        private enum StepType { Move, HitTarget, Stop, Teleport, Split, BombExplode }

        private class PathStep
        {
            public StepType Type;
            public Vector2Int GridPos;
            public Vector3 WorldPos;
            public Direction MoveDir;
            public Direction EntryDir;
            public Direction ExitDir;
            public TileBase Tile;
            public BulletStopReason StopReason;
            public PortalTile Portal;
            public PortalTile PairedPortal;
            public Vector3 PortalExitWorldPos;
            public Direction SplitDir;
            public Vector2Int SplitFromPos;
        }

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Awake() => ServiceLocator.Register(this);

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _bulletManager = ServiceLocator.Get<BulletManager>();
        }

        // ════════════════════════════════════════
        //  PUBLIC API
        // ════════════════════════════════════════

        public void StartSimulation(List<TurretData> turrets, int targetCount)
        {
            if (_isSimulating) return;

            _isSimulating = true;
            _targetsRemaining = targetCount;
            _totalTargets = targetCount;
            _activeBullets.Clear();

            Sequence master = DOTween.Sequence();
            master.SetTarget(this);
            for (int i = 0; i < turrets.Count; i++)
            {
                var t = turrets[i];
                float delay = i * 0.3f;
                master.InsertCallback(delay, () => RunBullet(t.Position, t.FireDirection));
            }
        }

        public void StopSimulation()
        {
            _isSimulating = false;
            DOTween.Kill(this);
            _endDelayTween?.Kill(false);
            _endDelayTween = null;

            foreach (var b in _activeBullets)
                if (b != null) b.Deactivate();
            _activeBullets.Clear();
        }

        // ════════════════════════════════════════
        //  PHASE 1: DATA — pure path computation
        // ════════════════════════════════════════

        private List<PathStep> ComputePath(Vector2Int startPos, Direction startDir)
        {
            var path = new List<PathStep>();
            var visited = new HashSet<(Vector2Int, Direction)>();

            Vector2Int curPos = startPos;
            Direction curDir = startDir;
            Vector2Int nextPos = curPos + DirectionHelper.ToVector(curDir);

            for (int step = 0; step < _maxSteps; step++)
            {
                if (!_gridManager.IsValidPosition(nextPos))
                {
                    var dv = DirectionHelper.ToVector(curDir);
                    Vector3 outPos = _gridManager.GridToWorldPosition(curPos)
                                   + new Vector3(dv.x, 0, dv.y) * _gridManager.TotalCellSize;
                    path.Add(new PathStep { Type = StepType.Stop, GridPos = nextPos, WorldPos = outPos,
                        MoveDir = curDir, StopReason = BulletStopReason.OutOfGrid });
                    return path;
                }

                if (!visited.Add((nextPos, curDir)))
                {
                    path.Add(new PathStep { Type = StepType.Stop, GridPos = nextPos,
                        WorldPos = _gridManager.GridToWorldPosition(nextPos),
                        MoveDir = curDir, StopReason = BulletStopReason.NoPath });
                    return path;
                }

                var tile = _gridManager.GetTile(nextPos);
                Vector3 worldPos = _gridManager.GridToWorldPosition(nextPos);

                if (tile == null)
                {
                    path.Add(new PathStep { Type = StepType.Stop, GridPos = nextPos, WorldPos = worldPos,
                        MoveDir = curDir, StopReason = BulletStopReason.NoPath });
                    return path;
                }

                if (tile.TileType == TileType.Target)
                {
                    path.Add(new PathStep { Type = StepType.HitTarget, GridPos = nextPos, WorldPos = worldPos,
                        MoveDir = curDir, EntryDir = curDir, ExitDir = curDir, Tile = tile });
                    return path;
                }

                Direction entryDir = DirectionHelper.Opposite(curDir);
                var exits = tile.GetExitDirections(entryDir);

                if (exits.Count == 0)
                {
                    path.Add(new PathStep { Type = StepType.Stop, GridPos = nextPos, WorldPos = worldPos,
                        MoveDir = curDir, EntryDir = entryDir, ExitDir = curDir, Tile = tile,
                        StopReason = tile.TileType == TileType.Absorb ? BulletStopReason.Absorbed : BulletStopReason.HitBlock });
                    return path;
                }

                if (tile.TileType == TileType.Portal)
                {
                    var portal = tile as PortalTile;
                    var paired = portal != null ? FindPairedPortal(portal) : null;
                    if (paired != null)
                    {
                        path.Add(new PathStep { Type = StepType.Teleport, GridPos = nextPos, WorldPos = worldPos,
                            MoveDir = curDir, Tile = tile, Portal = portal, PairedPortal = paired,
                            PortalExitWorldPos = _gridManager.GridToWorldPosition(paired.GridPosition) });
                        curPos = paired.GridPosition;
                        nextPos = curPos + DirectionHelper.ToVector(curDir);
                        continue;
                    }
                }

                if (exits.Count > 1)
                {
                    path.Add(new PathStep { Type = StepType.Move, GridPos = nextPos, WorldPos = worldPos,
                        MoveDir = curDir, EntryDir = entryDir, ExitDir = exits[0], Tile = tile });
                    for (int i = 1; i < exits.Count; i++)
                        path.Add(new PathStep { Type = StepType.Split, GridPos = nextPos, WorldPos = worldPos,
                            SplitDir = exits[i], SplitFromPos = nextPos });
                    curDir = exits[0]; curPos = nextPos;
                    nextPos = curPos + DirectionHelper.ToVector(curDir);
                    continue;
                }

                if (tile.TileType == TileType.Bomb)
                {
                    path.Add(new PathStep { Type = StepType.BombExplode, GridPos = nextPos, WorldPos = worldPos,
                        MoveDir = curDir, EntryDir = entryDir, ExitDir = exits[0], Tile = tile });
                    curDir = exits[0]; curPos = nextPos;
                    nextPos = curPos + DirectionHelper.ToVector(curDir);
                    continue;
                }

                path.Add(new PathStep { Type = StepType.Move, GridPos = nextPos, WorldPos = worldPos,
                    MoveDir = curDir, EntryDir = entryDir, ExitDir = exits[0], Tile = tile });
                curDir = exits[0]; curPos = nextPos;
                nextPos = curPos + DirectionHelper.ToVector(curDir);
            }

            path.Add(new PathStep { Type = StepType.Stop, GridPos = curPos,
                WorldPos = _gridManager.GridToWorldPosition(curPos),
                MoveDir = curDir, StopReason = BulletStopReason.NoPath });
            return path;
        }

        // ════════════════════════════════════════
        //  PHASE 2: VISUAL — sequence from final data
        // ════════════════════════════════════════

        private void AnimatePath(BulletController bullet, List<PathStep> path)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetTarget(bullet.transform);

            foreach (var step in path)
            {
                var s = step;

                switch (s.Type)
                {
                    case StepType.Move:
                    case StepType.BombExplode:
                        seq.AppendCallback(() => bullet.SetDirection(s.MoveDir));
                        seq.Append(bullet.MoveTo(s.WorldPos));
                        seq.AppendCallback(() =>
                        {
                            s.Tile.AnimateBulletPass(s.EntryDir, s.ExitDir);
                            if (s.Type == StepType.BombExplode) DestroyAdjacentBlocks(s.GridPos);
                        });
                        seq.AppendInterval(_stepDelay);
                        break;

                    case StepType.HitTarget:
                        seq.AppendCallback(() => bullet.SetDirection(s.MoveDir));
                        seq.Append(bullet.MoveTo(s.WorldPos));
                        seq.AppendCallback(() =>
                        {
                            s.Tile.AnimateBulletPass(s.EntryDir, s.ExitDir);
                            bullet.AnimateHitTarget();
                            OnTargetHit(s.GridPos);
                        });
                        break;

                    case StepType.Stop:
                        seq.AppendCallback(() => bullet.SetDirection(s.MoveDir));
                        seq.Append(bullet.MoveTo(s.WorldPos));
                        seq.AppendCallback(() =>
                        {
                            if (s.Tile != null) s.Tile.AnimateBulletPass(s.EntryDir, s.ExitDir);
                            bullet.AnimateStop();
                            OnBulletStopped(s.GridPos, s.StopReason);
                        });
                        break;

                    case StepType.Teleport:
                        seq.AppendCallback(() => bullet.SetDirection(s.MoveDir));
                        seq.Append(bullet.MoveTo(s.WorldPos));
                        seq.AppendCallback(() =>
                        {
                            s.Portal.AnimateTeleportIn();
                            EventBus.Publish(new BulletTeleportedEvent
                            { FromPortal = s.Portal.GridPosition, ToPortal = s.PairedPortal.GridPosition });
                        });
                        seq.AppendCallback(() =>
                        {
                            bullet.AnimateTeleport(s.PortalExitWorldPos, null);
                            s.PairedPortal.AnimateTeleportOut();
                        });
                        seq.AppendInterval(0.4f);
                        break;

                    case StepType.Split:
                        seq.AppendCallback(() => RunBullet(s.SplitFromPos, s.SplitDir));
                        break;
                }
            }
        }

        // ════════════════════════════════════════
        //  ORCHESTRATION
        // ════════════════════════════════════════

        private void RunBullet(Vector2Int startPos, Direction startDir)
        {
            var bullet = _bulletManager.SpawnBullet();
            bullet.Initialize(_gridManager.GridToWorldPosition(startPos), startDir);
            _activeBullets.Add(bullet);

            EventBus.Publish(new BulletFiredEvent
            { StartPosition = bullet.transform.position, GridPos = startPos });

            var path = ComputePath(startPos, startDir);
            DebugLogPath(startPos, startDir, path);
            AnimatePath(bullet, path);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DebugLogPath(Vector2Int start, Direction dir, List<PathStep> path)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[BulletPath] Start=({start.x},{start.y}) Dir={dir} Steps={path.Count}");
            for (int i = 0; i < path.Count; i++)
            {
                var s = path[i];
                string tileName = s.Tile != null ? $"{s.Tile.TileType}(rot={s.Tile.RotationState})" : "null";
                switch (s.Type)
                {
                    case StepType.Move:
                        sb.AppendLine($"  [{i}] MOVE ({s.GridPos.x},{s.GridPos.y}) {tileName} entry={s.EntryDir} exit={s.ExitDir}");
                        break;
                    case StepType.HitTarget:
                        sb.AppendLine($"  [{i}] HIT TARGET ({s.GridPos.x},{s.GridPos.y})");
                        break;
                    case StepType.Stop:
                        sb.AppendLine($"  [{i}] STOP ({s.GridPos.x},{s.GridPos.y}) reason={s.StopReason} tile={tileName}");
                        break;
                    case StepType.Teleport:
                        sb.AppendLine($"  [{i}] TELEPORT ({s.GridPos.x},{s.GridPos.y}) → ({s.PairedPortal.GridPosition.x},{s.PairedPortal.GridPosition.y})");
                        break;
                    case StepType.Split:
                        sb.AppendLine($"  [{i}] SPLIT from ({s.SplitFromPos.x},{s.SplitFromPos.y}) dir={s.SplitDir}");
                        break;
                    case StepType.BombExplode:
                        sb.AppendLine($"  [{i}] BOMB ({s.GridPos.x},{s.GridPos.y})");
                        break;
                }
            }
            Debug.Log(sb.ToString());
        }

        // ════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════

        private PortalTile FindPairedPortal(PortalTile portal)
        {
            foreach (var cell in _gridManager.GetAllCells())
                if (cell.TileInstance is PortalTile other && other != portal && other.PortalId == portal.PortalId)
                    return other;
            return null;
        }

        private void DestroyAdjacentBlocks(Vector2Int pos)
        {
            Direction[] dirs = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
            foreach (var dir in dirs)
            {
                Vector2Int adj = pos + DirectionHelper.ToVector(dir);
                var tile = _gridManager.GetTile(adj);
                if (tile != null && tile.TileType == TileType.Block)
                {
                    tile.AnimateBulletPass(dir, dir);
                    var vis = tile.VisualRoot != null ? tile.VisualRoot : tile.transform;
                    vis.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                        .OnComplete(() => { _gridManager.SetTile(adj, null); Destroy(tile.gameObject); });
                }
            }
        }

        // ════════════════════════════════════════
        //  SIMULATION END
        // ════════════════════════════════════════

        private void OnTargetHit(Vector2Int pos)
        {
            _targetsRemaining--;
            EventBus.Publish(new BulletHitTargetEvent
            { TargetPos = pos, TargetIndex = _totalTargets - _targetsRemaining });
            CheckSimulationEnd();
        }

        private void OnBulletStopped(Vector2Int pos, BulletStopReason reason)
        {
            EventBus.Publish(new BulletStoppedEvent { LastPos = pos, Reason = reason });
            CheckSimulationEnd();
        }

        private void CheckSimulationEnd()
        {
            if (!_isSimulating) return;

            if (_targetsRemaining <= 0)
            {
                _isSimulating = false;
                _endDelayTween?.Kill(false);
                _endDelayTween = DOVirtual.DelayedCall(0.5f, () =>
                {
                    _endDelayTween = null;
                    var lm = ServiceLocator.Get<LevelManager>();
                    var timer = ServiceLocator.Get<LevelTimer>();
                    int moves = lm != null ? lm.MoveCount : 0;
                    int levelIdx = lm != null ? lm.CurrentLevelIndex : 0;
                    float timeLeft = timer != null ? timer.TimeRemaining : 0f;
                    float timeLimit = timer != null ? timer.TimeLimit : 0f;
                    int stars = lm?.CurrentLevel != null ? lm.CurrentLevel.CalculateStarsByTime(timeLeft) : 1;
                    EventBus.Publish(new LevelCompletedEvent
                    { LevelIndex = levelIdx, Stars = stars, MoveCount = moves,
                      TimeRemaining = timeLeft, TimeLimit = timeLimit });
                });
                return;
            }

            foreach (var b in _activeBullets)
                if (b != null && b.IsActive) return;

            _isSimulating = false;
            _endDelayTween?.Kill(false);
            _endDelayTween = DOVirtual.DelayedCall(0.3f, () =>
            {
                _endDelayTween = null;
                _bulletManager?.ReturnAllBullets();
                _activeBullets.Clear();
                ServiceLocator.Get<LevelManager>()?.ResetAllTargets();
                var sm = ServiceLocator.Get<GameStateManager>();
                if (sm != null && sm.CurrentStateType == GameStateType.Simulating)
                    sm.ChangeState(GameStateType.Setup);
            });
        }
    }

    [System.Serializable]
    public class TurretData
    {
        public Vector2Int Position;
        public Direction FireDirection;
    }
}
