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
        [SerializeField] private float _stepDelay = 0.05f; // Extra delay between steps
        [SerializeField] private int _maxSteps = 100; // Prevent infinite loops

        private GridManager _gridManager;
        private BulletManager _bulletManager;
        private bool _isSimulating;
        private int _targetsRemaining;
        private int _totalTargets;
        private List<BulletController> _activeBullets = new List<BulletController>();

        public bool IsSimulating => _isSimulating;
        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _bulletManager = ServiceLocator.Get<BulletManager>();
        }

        public void StartSimulation(List<TurretData> turrets, int targetCount)
        {
            if (_isSimulating) return;
            _isSimulating = true;
            _targetsRemaining = targetCount;
            _totalTargets = targetCount;
            _activeBullets.Clear();

            Sequence masterSequence = DOTween.Sequence();

            for (int i = 0; i < turrets.Count; i++)
            {
                var turret = turrets[i];
                float startDelay = i * 0.3f; // Stagger turret firing
                masterSequence.InsertCallback(startDelay, () => SimulateBullet(turret));
            }
        }

        private void SimulateBullet(TurretData turretData)
        {
            var bullet = _bulletManager.SpawnBullet();
            Vector3 startPos = _gridManager.GridToWorldPosition(turretData.Position);
            bullet.Initialize(startPos, turretData.FireDirection);
            _activeBullets.Add(bullet);

            EventBus.Publish(new BulletFiredEvent
            {
                StartPosition = startPos,
                GridPos = turretData.Position
            });

            // Build path sequence
            SimulatePath(bullet, turretData.Position, turretData.FireDirection);
        }

        private void SimulatePath(BulletController bullet, Vector2Int startPos, Direction startDir)
        {
            Sequence pathSequence = DOTween.Sequence();
            var visited = new HashSet<(Vector2Int, Direction)>();

            Vector2Int currentPos = startPos;
            Direction currentDir = startDir;
            int step = 0;

            // Move to first tile in fire direction
            Vector2Int nextPos = currentPos + DirectionHelper.ToVector(currentDir);

            while (step < _maxSteps)
            {
                if (!_gridManager.IsValidPosition(nextPos))
                {
                    // Out of grid
                    float delay = step * (bullet.MoveSpeed + _stepDelay);
                    Vector3 outPos = _gridManager.GridToWorldPosition(currentPos) +
                                     (Vector3)(Vector2)DirectionHelper.ToVector(currentDir) * _gridManager.TotalCellSize;
                    // Convert Vector2Int direction to Vector3
                    var dirVec = DirectionHelper.ToVector(currentDir);
                    Vector3 dir3 = new Vector3(dirVec.x, 0, dirVec.y) * _gridManager.TotalCellSize;
                    outPos = _gridManager.GridToWorldPosition(currentPos) + dir3;

                    var capturedDir = currentDir;
                    var capturedPos = nextPos;
                    var capturedOutPos = outPos;
                    pathSequence.Append(bullet.MoveTo(capturedOutPos, capturedDir));
                    pathSequence.AppendCallback(() =>
                    {
                        bullet.AnimateStop();
                        OnBulletStopped(capturedPos, BulletStopReason.OutOfGrid);
                    });
                    break;
                }

                // Check for loops
                var state = (nextPos, currentDir);
                if (visited.Contains(state))
                {
                    var capturedPos2 = nextPos;
                    pathSequence.AppendCallback(() =>
                    {
                        bullet.AnimateStop();
                        OnBulletStopped(capturedPos2, BulletStopReason.NoPath);
                    });
                    break;
                }
                visited.Add(state);

                var tile = _gridManager.GetTile(nextPos);
                Vector3 worldPos = _gridManager.GridToWorldPosition(nextPos);

                if (tile == null)
                {
                    // Empty cell - bullet stops
                    var capturedDir3 = currentDir;
                    var capturedPos3 = nextPos;
                    pathSequence.Append(bullet.MoveTo(worldPos, capturedDir3));
                    pathSequence.AppendCallback(() =>
                    {
                        bullet.AnimateStop();
                        OnBulletStopped(capturedPos3, BulletStopReason.NoPath);
                    });
                    break;
                }

                // Check if target
                if (tile.TileType == TileType.Target)
                {
                    var capturedDir4 = currentDir;
                    var capturedPos4 = nextPos;
                    var capturedWorldPos = worldPos;
                    pathSequence.Append(bullet.MoveTo(capturedWorldPos, capturedDir4));
                    pathSequence.AppendCallback(() =>
                    {
                        tile.AnimateBulletPass(capturedDir4, capturedDir4);
                        bullet.AnimateHitTarget();
                        OnTargetHit(capturedPos4);
                    });
                    break;
                }

                // Get exit directions from tile
                Direction entryDir = DirectionHelper.Opposite(currentDir);
                var exits = tile.GetExitDirections(entryDir);

                if (exits.Count == 0)
                {
                    // Blocked or absorbed
                    var capturedDir5 = currentDir;
                    var capturedEntryDir = entryDir;
                    var capturedPos5 = nextPos;
                    var isAbsorb = tile.TileType == TileType.Absorb;
                    pathSequence.Append(bullet.MoveTo(worldPos, capturedDir5));
                    pathSequence.AppendCallback(() =>
                    {
                        tile.AnimateBulletPass(capturedEntryDir, capturedDir5);
                        bullet.AnimateStop();
                        OnBulletStopped(capturedPos5, isAbsorb ? BulletStopReason.Absorbed : BulletStopReason.HitBlock);
                    });
                    break;
                }

                // Handle portal teleport
                if (tile.TileType == TileType.Portal)
                {
                    var portal = tile as Tile.PortalTile;
                    if (portal != null)
                    {
                        var pairedPortal = FindPairedPortal(portal);
                        if (pairedPortal != null)
                        {
                            var capturedDir6 = currentDir;
                            var capturedWorldPos2 = worldPos;
                            Vector3 portalOutPos = _gridManager.GridToWorldPosition(pairedPortal.GridPosition);
                            var capturedPairedPos = pairedPortal.GridPosition;

                            pathSequence.Append(bullet.MoveTo(capturedWorldPos2, capturedDir6));
                            pathSequence.AppendCallback(() =>
                            {
                                portal.AnimateTeleportIn();
                                EventBus.Publish(new BulletTeleportedEvent
                                {
                                    FromPortal = portal.GridPosition,
                                    ToPortal = capturedPairedPos
                                });
                            });
                            pathSequence.AppendCallback(() =>
                            {
                                bullet.AnimateTeleport(portalOutPos, null);
                                pairedPortal.AnimateTeleportOut();
                            });
                            pathSequence.AppendInterval(0.4f); // Wait for teleport animation

                            currentPos = pairedPortal.GridPosition;
                            // Continue in same direction
                            nextPos = currentPos + DirectionHelper.ToVector(currentDir);
                            step++;
                            continue;
                        }
                    }
                }

                // Handle splitter (multiple exits)
                if (exits.Count > 1)
                {
                    var capturedDir7 = currentDir;
                    var capturedEntryDir2 = entryDir;
                    var capturedNextPos = nextPos;
                    pathSequence.Append(bullet.MoveTo(worldPos, capturedDir7));
                    pathSequence.AppendCallback(() =>
                    {
                        tile.AnimateBulletPass(capturedEntryDir2, exits[0]);
                    });

                    // First exit continues with current bullet
                    currentDir = exits[0];
                    currentPos = nextPos;
                    nextPos = currentPos + DirectionHelper.ToVector(currentDir);

                    // Spawn new bullets for additional exits
                    for (int i = 1; i < exits.Count; i++)
                    {
                        Direction splitDir = exits[i];
                        Vector2Int splitStart = capturedNextPos;
                        pathSequence.AppendCallback(() =>
                        {
                            SpawnSplitBullet(splitStart, splitDir);
                        });
                    }

                    step++;
                    continue;
                }

                // Handle bomb
                if (tile.TileType == TileType.Bomb)
                {
                    var bomb = tile as BombTile;
                    var capturedDir8 = currentDir;
                    var capturedEntryDir3 = entryDir;
                    pathSequence.Append(bullet.MoveTo(worldPos, capturedDir8));
                    pathSequence.AppendCallback(() =>
                    {
                        tile.AnimateBulletPass(capturedEntryDir3, exits[0]);
                        DestroyAdjacentBlocks(tile.GridPosition);
                    });

                    currentDir = exits[0];
                    currentPos = nextPos;
                    nextPos = currentPos + DirectionHelper.ToVector(currentDir);
                    step++;
                    continue;
                }

                // Normal movement
                {
                    var capturedDir9 = currentDir;
                    var capturedEntryDir4 = entryDir;
                    var capturedExit = exits[0];
                    pathSequence.Append(bullet.MoveTo(worldPos, capturedDir9));
                    pathSequence.AppendCallback(() =>
                    {
                        tile.AnimateBulletPass(capturedEntryDir4, capturedExit);
                        EventBus.Publish(new BulletMovedEvent
                        {
                            FromPos = currentPos,
                            ToPos = nextPos,
                            WorldPosition = worldPos
                        });
                    });
                    pathSequence.AppendInterval(_stepDelay);

                    currentDir = exits[0];
                    currentPos = nextPos;
                    nextPos = currentPos + DirectionHelper.ToVector(currentDir);
                    step++;
                }
            }
        }

        private void SpawnSplitBullet(Vector2Int fromPos, Direction dir)
        {
            var bullet = _bulletManager.SpawnBullet();
            Vector3 pos = _gridManager.GridToWorldPosition(fromPos);
            bullet.Initialize(pos, dir);
            _activeBullets.Add(bullet);
            SimulatePath(bullet, fromPos, dir);
        }

        private PortalTile FindPairedPortal(PortalTile portal)
        {
            var cells = _gridManager.GetAllCells();
            foreach (var cell in cells)
            {
                if (cell.TileInstance is PortalTile other &&
                    other != portal &&
                    other.PortalId == portal.PortalId)
                {
                    return other;
                }
            }
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
                    // Destroy block after animation
                    var target = tile.VisualRoot != null ? tile.VisualRoot : tile.transform;
                    target.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            _gridManager.SetTile(adj, null);
                            Destroy(tile.gameObject);
                        });
                }
            }
        }

        private void OnTargetHit(Vector2Int pos)
        {
            _targetsRemaining--;
            EventBus.Publish(new BulletHitTargetEvent
            {
                TargetPos = pos,
                TargetIndex = _totalTargets - _targetsRemaining
            });

            if (_targetsRemaining <= 0)
            {
                _isSimulating = false;
                // Small delay before win
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    var levelManager = ServiceLocator.Get<LevelManager>();
                    var timer = ServiceLocator.Get<Timer.LevelTimer>();
                    int moves = levelManager != null ? levelManager.MoveCount : 0;
                    int levelIdx = levelManager != null ? levelManager.CurrentLevelIndex : 0;
                    float timeRemaining = timer != null ? timer.TimeRemaining : 0f;
                    float timeLimit = timer != null ? timer.TimeLimit : 0f;
                    int stars = levelManager?.CurrentLevel != null
                        ? levelManager.CurrentLevel.CalculateStarsByTime(timeRemaining) : 1;
                    EventBus.Publish(new LevelCompletedEvent
                    {
                        LevelIndex = levelIdx,
                        Stars = stars,
                        MoveCount = moves,
                        TimeRemaining = timeRemaining,
                        TimeLimit = timeLimit
                    });
                });
            }
        }

        private void OnBulletStopped(Vector2Int pos, BulletStopReason reason)
        {
            EventBus.Publish(new BulletStoppedEvent { LastPos = pos, Reason = reason });

            // Check if all bullets are done
            bool allDone = true;
            foreach (var b in _activeBullets)
                if (b.IsActive) { allDone = false; break; }

            if (allDone && _targetsRemaining > 0)
            {
                // All bullets stopped but targets remain.
                // Don't fail immediately - let player fire again.
                // Just return to Setup state so they can adjust and re-fire.
                _isSimulating = false;
                DOVirtual.DelayedCall(0.3f, () =>
                {
                    // Return bullets to pool
                    var bulletManager = ServiceLocator.Get<BulletManager>();
                    bulletManager?.ReturnAllBullets();
                    _activeBullets.Clear();

                    // Back to Setup so player can adjust tiles and fire again
                    // Timer keeps running - no restart
                    var stateManager = ServiceLocator.Get<GameStateManager>();
                    if (stateManager != null && stateManager.CurrentStateType == GameStateType.Simulating)
                    {
                        stateManager.ChangeState(GameStateType.Setup);
                    }
                });
            }
        }

        public void StopSimulation()
        {
            _isSimulating = false;
            foreach (var bullet in _activeBullets)
            {
                if (bullet != null)
                    bullet.Deactivate();
            }
            _activeBullets.Clear();
            DOTween.Kill(this);
        }
    }

    [System.Serializable]
    public class TurretData
    {
        public Vector2Int Position;
        public Direction FireDirection;
    }
}
