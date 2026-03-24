using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Grid;
using BulletRoute.Tile;
using BulletRoute.Bullet;
using BulletRoute.Turret;
using BulletRoute.Target;
using BulletRoute.Command;

namespace BulletRoute.Level
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Database")]
        [SerializeField] private List<LevelData> _levels = new List<LevelData>();

        [Header("References")]
        [SerializeField] private Transform _tileParent;

        private GridManager _gridManager;
        private TileFactory _tileFactory;
        private BulletSimulator _bulletSimulator;
        private BulletManager _bulletManager;
        private CommandManager _commandManager;
        private LevelData _currentLevel;
        private int _currentLevelIndex;

        private List<TurretController> _turrets = new List<TurretController>();
        private List<TargetController> _targets = new List<TargetController>();

        public LevelData CurrentLevel => _currentLevel;
        public int CurrentLevelIndex => _currentLevelIndex;
        public CommandManager CommandManager => _commandManager;
        public int MoveCount => _commandManager.MoveCount;

        private void Awake()
        {
            _commandManager = new CommandManager();
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _tileFactory = ServiceLocator.Get<TileFactory>();
            _bulletSimulator = ServiceLocator.Get<BulletSimulator>();
            _bulletManager = ServiceLocator.Get<BulletManager>();
        }

        // LevelManager is a WORKER - GameManager is the orchestrator.
        // No direct event subscriptions here to avoid double-handler bugs.

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= _levels.Count)
            {
                Debug.LogError($"[LevelManager] Invalid level index: {index}");
                //return;
                index = Random.Range(3, _levels.Count);
            }

            _currentLevelIndex = index;
            _currentLevel = _levels[index];
            _commandManager.Clear();
            _turrets.Clear();
            _targets.Clear();

            BuildLevel(_currentLevel);

            EventBus.Publish(new LevelStartedEvent { LevelIndex = index });
        }

        private void BuildLevel(LevelData data)
        {
            _gridManager.InitializeGrid(data.GridWidth, data.GridHeight);

            // ClearLevel() already destroyed _tileParent, create fresh one
            if (_tileParent != null)
                DestroyImmediate(_tileParent.gameObject);
            _tileParent = new GameObject("[Tiles]").transform;

            // Place tiles
            foreach (var placement in data.Tiles)
            {
                var tile = _tileFactory.CreateTile(placement.Type, placement.Position, placement.Rotation, _tileParent);
                if (tile != null)
                {
                    _gridManager.SetTile(placement.Position, tile);
                    if (placement.IsLocked)
                    {
                        var cell = _gridManager.GetCell(placement.Position);
                        if (cell != null) cell.IsLocked = true;
                    }

                    // Handle extra data
                    if (placement.Type == TileType.Portal && tile is PortalTile portal)
                    {
                        portal.SetPortalId(placement.ExtraData);
                    }
                    if (placement.Type == TileType.Mirror && tile is MirrorTile mirror)
                    {
                        // ExtraData: 0 = forward slash (/), 1 = back slash (\)
                        mirror.SetMirrorType(placement.ExtraData == 0);
                    }
                }
            }

            // Place turrets
            foreach (var turretData in data.Turrets)
            {
                var tile = _tileFactory.CreateTile(TileType.Turret, turretData.Position, 0, _tileParent);
                if (tile is TurretController turret)
                {
                    turret.SetFireDirection(turretData.FireDirection);
                    _gridManager.SetTile(turretData.Position, turret);
                    _turrets.Add(turret);
                }
            }

            // Place targets
            foreach (var targetData in data.Targets)
            {
                var tile = _tileFactory.CreateTile(TileType.Target, targetData.Position, 0, _tileParent);
                if (tile is TargetController target)
                {
                    _gridManager.SetTile(targetData.Position, target);
                    _targets.Add(target);
                }
            }

            // Animate grid appear
            _gridManager.AnimateGridAppear();
        }

        public void FireBullets()
        {
            if (_bulletSimulator.IsSimulating) return;

            // Reset targets before each fire (for re-fire support)
            foreach (var target in _targets)
            {
                target.ResetTarget();
            }

            // Return any leftover bullets
            _bulletManager.ReturnAllBullets();

            // Build turret data list
            var turretDataList = new List<BulletRoute.Bullet.TurretData>();
            foreach (var turret in _turrets)
            {
                turretDataList.Add(new BulletRoute.Bullet.TurretData
                {
                    Position = turret.GridPosition,
                    FireDirection = turret.FireDirection
                });

                // Animate turret firing
                turret.AnimateChargeUp(() => turret.AnimateFire());
            }

            // Start simulation with delay for charge animation
            DOVirtual.DelayedCall(0.4f, () =>
            {
                _bulletSimulator.StartSimulation(turretDataList, _targets.Count);
            });
        }

        public void ResetLevel()
        {
            _bulletSimulator.StopSimulation();
            _bulletManager.ReturnAllBullets();

            foreach (var target in _targets)
            {
                target.ResetTarget();
            }

            _commandManager.Clear();

            EventBus.Publish(new LevelResetEvent { LevelIndex = _currentLevelIndex });
        }

        public void StopAllBullets()
        {
            _bulletSimulator.StopSimulation();
            _bulletManager.ReturnAllBullets();
        }

        public void LoadNextLevel()
        {
            ClearLevel();
            LoadLevel(_currentLevelIndex + 1);
        }

        public void ClearLevel()
        {
            DOTween.KillAll(false); // Kill ALL tweens to prevent any stale references

            _bulletSimulator.StopSimulation();
            _bulletManager.ReturnAllBullets();
            _gridManager.ClearGrid();

            // Destroy tile parent immediately
            if (_tileParent != null)
            {
                // DestroyImmediate ensures tiles are gone before new level builds
                DestroyImmediate(_tileParent.gameObject);
                _tileParent = null;
            }

            _turrets.Clear();
            _targets.Clear();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<LevelManager>();
        }
    }
}
