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

        // ── Snapshot: save grid state before simulation, restore on miss ──
        private GridSnapshot _gridSnapshot = new GridSnapshot();
        // Track blocks destroyed by bombs during simulation (for visual cleanup)
        private List<DestroyedBlockInfo> _destroyedBlocks = new List<DestroyedBlockInfo>();
        // Track pending splits to avoid premature "allDone" checks
        private int _pendingSplits;

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

        /// <summary>
        /// Tracks a block tile that was destroyed by a bomb during simulation.
        /// Needed to restore on miss.
        /// </summary>
        private class DestroyedBlockInfo
        {
            public Vector2Int GridPos;
            public TileType Type;
            public int Rotation;
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
            _destroyedBlocks.Clear();
            _pendingSplits = 0;

            // ── Snapshot: capture grid state BEFORE any bullets move ──
            _gridSnapshot.Capture(_gridManager);
            Debug.Log($"[Simulation] Snapshot captured: {_gridSnapshot.Tiles.Count} tiles");

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
            _pendingSplits = 0;
            DOTween.Kill(this);
            _endDelayTween?.Kill(false);
            _endDelayTween = null;

            foreach (var b in _activeBullets)
                if (b != null) b.Deactivate();
            _activeBullets.Clear();
            _destroyedBlocks.Clear();
            _gridSnapshot.Clear();
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

            // Count how many splits are in this path so we can track pending
            int splitCount = 0;
            foreach (var step in path)
                if (step.Type == StepType.Split) splitCount++;
            _pendingSplits += splitCount;

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
                            if (s.Tile != null) s.Tile.AnimateBulletPass(s.EntryDir, s.ExitDir);
                            if (s.Type == StepType.BombExplode) DestroyAdjacentBlocks(s.GridPos);
                        });
                        seq.AppendInterval(_stepDelay);
                        break;

                    case StepType.HitTarget:
                        seq.AppendCallback(() => bullet.SetDirection(s.MoveDir));
                        seq.Append(bullet.MoveTo(s.WorldPos));
                        seq.AppendCallback(() =>
                        {
                            if (s.Tile != null) s.Tile.AnimateBulletPass(s.EntryDir, s.ExitDir);
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
                            if (s.Portal != null) s.Portal.AnimateTeleportIn();
                            EventBus.Publish(new BulletTeleportedEvent
                            { FromPortal = s.Portal.GridPosition, ToPortal = s.PairedPortal.GridPosition });
                        });
                        seq.AppendCallback(() =>
                        {
                            bullet.AnimateTeleport(s.PortalExitWorldPos, null);
                            if (s.PairedPortal != null) s.PairedPortal.AnimateTeleportOut();
                        });
                        seq.AppendInterval(0.4f);
                        break;

                    case StepType.Split:
                        seq.AppendCallback(() =>
                        {
                            _pendingSplits--;
                            RunBullet(s.SplitFromPos, s.SplitDir);
                        });
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
            if (bullet == null)
            {
                Debug.LogError("[BulletSimulator] Failed to spawn bullet!");
                return;
            }

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

        /// <summary>
        /// Destroy block tiles adjacent to a bomb.
        /// Tracks destroyed blocks so we can restore them on miss.
        /// </summary>
        private void DestroyAdjacentBlocks(Vector2Int pos)
        {
            Direction[] dirs = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
            foreach (var dir in dirs)
            {
                Vector2Int adj = pos + DirectionHelper.ToVector(dir);
                var tile = _gridManager.GetTile(adj);
                if (tile != null && tile.TileType == TileType.Block)
                {
                    // Track this block for potential restore
                    _destroyedBlocks.Add(new DestroyedBlockInfo
                    {
                        GridPos = adj,
                        Type = tile.TileType,
                        Rotation = tile.RotationState
                    });

                    tile.AnimateBulletPass(dir, dir);
                    var vis = tile.VisualRoot != null ? tile.VisualRoot : tile.transform;
                    // Capture local ref to avoid closure issues
                    var tileRef = tile;
                    var adjPos = adj;
                    vis.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            _gridManager.SetTile(adjPos, null);
                            if (tileRef != null) Destroy(tileRef.gameObject);
                        });
                }
            }
        }

        // ════════════════════════════════════════
        //  GRID RESTORE — revert destructive changes on miss
        // ════════════════════════════════════════

        /// <summary>
        /// Restore grid to pre-simulation state.
        /// Re-creates destroyed blocks, resets bombs, resets targets.
        /// </summary>
        private void RestoreGridFromSnapshot()
        {
            var lm = ServiceLocator.Get<LevelManager>();
            var tileFactory = ServiceLocator.Get<TileFactory>();

            if (lm == null || tileFactory == null)
            {
                Debug.LogError("[BulletSimulator] Cannot restore: LevelManager or TileFactory is null");
                return;
            }

            // 1. Re-create destroyed block tiles
            foreach (var block in _destroyedBlocks)
            {
                // Only re-create if the cell is still empty
                var existing = _gridManager.GetTile(block.GridPos);
                if (existing != null) continue;

                var newTile = tileFactory.CreateTile(block.Type, block.GridPos, block.Rotation, lm.TileParent);
                if (newTile != null)
                {
                    _gridManager.SetTile(block.GridPos, newTile);
                    // Animate block appearing
                    var vis = newTile.VisualRoot != null ? newTile.VisualRoot : newTile.transform;
                    vis.localScale = Vector3.zero;
                    vis.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

                    Debug.Log($"[Restore] Re-created Block at ({block.GridPos.x},{block.GridPos.y})");
                }
            }
            _destroyedBlocks.Clear();

            // 2. Reset all bombs (exploded state + visual)
            lm.ResetAllBombs();

            // 3. Reset all targets (hit state + visual)
            lm.ResetAllTargets();

            // 4. Reset visual scale for all remaining tiles
            // (some tiles may have residual animation states from bullet pass)
            foreach (var cell in _gridManager.GetAllCells())
            {
                if (cell.TileInstance == null) continue;
                // Skip targets and bombs - they have their own reset
                if (cell.TileInstance is BombTile) continue;
                if (cell.TileInstance.TileType == TileType.Target) continue;

                var vis = cell.TileInstance.VisualRoot != null
                    ? cell.TileInstance.VisualRoot
                    : cell.TileInstance.transform;

                // Kill any lingering tweens and ensure scale is correct
                DOTween.Kill(vis);
                vis.localScale = Vector3.one;
            }

            _gridSnapshot.Clear();
            Debug.Log("[Restore] Grid restored from snapshot");
        }

        // ════════════════════════════════════════
        //  SIMULATION END
        // ════════════════════════════════════════

        private void OnTargetHit(Vector2Int pos)
        {
            // Guard against going negative (e.g., two split bullets hitting same target)
            if (_targetsRemaining <= 0) return;

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
            Debug.Log($"[SimEnd] _isSimulating={_isSimulating} _targetsRemaining={_targetsRemaining} " +
                      $"activeBullets={_activeBullets.Count} pendingSplits={_pendingSplits}");

            if (!_isSimulating)
            {
                Debug.Log("[SimEnd] SKIP: _isSimulating is false");
                return;
            }

            // ── WIN: all targets hit ──
            if (_targetsRemaining <= 0)
            {
                Debug.Log("[SimEnd] → WIN path");
                _isSimulating = false;
                _pendingSplits = 0;
                _endDelayTween?.Kill(false);
                _endDelayTween = DOVirtual.DelayedCall(0.5f, () =>
                {
                    _endDelayTween = null;
                    Debug.Log("[SimEnd] WIN delayed callback fired → publishing LevelCompletedEvent");
                    var lm = ServiceLocator.Get<LevelManager>();
                    var timer = ServiceLocator.Get<LevelTimer>();
                    int moves = lm != null ? lm.MoveCount : 0;
                    int levelIdx = lm != null ? lm.CurrentLevelIndex : 0;
                    float timeLeft = timer != null ? timer.TimeRemaining : 0f;
                    float timeLimit = timer != null ? timer.TimeLimit : 0f;
                    int stars = lm?.CurrentLevel != null ? lm.CurrentLevel.CalculateStarsByTime(timeLeft) : 1;
                    Debug.Log($"[SimEnd] LevelCompleted: level={levelIdx} stars={stars} moves={moves}");
                    EventBus.Publish(new LevelCompletedEvent
                    { LevelIndex = levelIdx, Stars = stars, MoveCount = moves,
                      TimeRemaining = timeLeft, TimeLimit = timeLimit });

                    // Clear snapshot on win (no restore needed)
                    _destroyedBlocks.Clear();
                    _gridSnapshot.Clear();
                });
                return;
            }

            // ── Check if all bullets are done ──
            // Copy to temp list to avoid collection-modification issues from splits
            bool allDone = true;
            for (int i = 0; i < _activeBullets.Count; i++)
            {
                var b = _activeBullets[i];
                if (b != null && b.IsActive)
                {
                    allDone = false;
                    break;
                }
            }

            // Also wait for pending splits to spawn their bullets
            if (_pendingSplits > 0) allDone = false;

            Debug.Log($"[SimEnd] allDone={allDone}");

            if (!allDone) return;

            // ── MISS: return to Setup with grid restoration ──
            Debug.Log("[SimEnd] → SETUP path (all bullets done, targets remaining)");
            _isSimulating = false;
            _pendingSplits = 0;
            _endDelayTween?.Kill(false);
            _endDelayTween = DOVirtual.DelayedCall(0.3f, () =>
            {
                _endDelayTween = null;
                Debug.Log("[SimEnd] SETUP delayed callback fired → restore grid + ChangeState(Setup)");

                // Cleanup bullets
                _bulletManager?.ReturnAllBullets();
                _activeBullets.Clear();

                // ── RESTORE GRID: re-create destroyed blocks, reset bombs & targets ──
                RestoreGridFromSnapshot();

                // Transition back to Setup
                var sm = ServiceLocator.Get<GameStateManager>();
                Debug.Log($"[SimEnd] CurrentState={sm?.CurrentStateType}");
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
