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

        private void OnEnable()
        {
            EventBus.Subscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Subscribe<ResetButtonPressedEvent>(OnResetPressed);
            EventBus.Subscribe<LevelFailedEvent>(OnLevelFailed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayButtonPressedEvent>(OnPlayPressed);
            EventBus.Unsubscribe<ResetButtonPressedEvent>(OnResetPressed);
            EventBus.Unsubscribe<LevelFailedEvent>(OnLevelFailed);
        }

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= _levels.Count)
            {
                Debug.LogError($"[LevelManager] Invalid level index: {index}");
                return;
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

            if (_tileParent == null)
            {
                _tileParent = new GameObject("[Tiles]").transform;
            }

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

        private void OnPlayPressed(PlayButtonPressedEvent evt)
        {
            if (_bulletSimulator.IsSimulating) return;

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

        private void OnResetPressed(ResetButtonPressedEvent evt)
        {
            ResetLevel();
        }

        public void ResetLevel()
        {
            _bulletSimulator.StopSimulation();
            _bulletManager.ReturnAllBullets();

            // Reset targets
            foreach (var target in _targets)
            {
                target.ResetTarget();
            }

            _commandManager.Clear();

            EventBus.Publish(new LevelResetEvent { LevelIndex = _currentLevelIndex });
        }

        public int CalculateCurrentStars()
        {
            return _currentLevel.CalculateStars(_commandManager.MoveCount);
        }

        private void OnLevelFailed(LevelFailedEvent evt)
        {
            // Auto-reset after delay
            DOVirtual.DelayedCall(1.5f, () => ResetLevel());
        }

        public void LoadNextLevel()
        {
            ClearLevel();
            LoadLevel(_currentLevelIndex + 1);
        }

        private void ClearLevel()
        {
            _bulletSimulator.StopSimulation();
            _bulletManager.ReturnAllBullets();
            _gridManager.ClearGrid();

            if (_tileParent != null)
            {
                foreach (Transform child in _tileParent)
                    Destroy(child.gameObject);
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
