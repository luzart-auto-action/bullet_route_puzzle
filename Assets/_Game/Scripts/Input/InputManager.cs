using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Grid;
using BulletRoute.Tile;
using BulletRoute.Command;
using BulletRoute.Level;

namespace BulletRoute.Input
{
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float _dragThreshold = 0.3f;
        [SerializeField] private LayerMask _gridLayer;
        [SerializeField] private Camera _mainCamera;

        [Header("Drag Visual")]
        [SerializeField] private float _dragLiftHeight = 0.5f;
        [SerializeField] private float _dragScale = 1.1f;

        private GridManager _gridManager;
        private LevelManager _levelManager;
        private bool _isEnabled = true;
        private bool _isDragging;
        private Vector3 _dragStartPos;
        private Vector2Int _selectedGridPos;
        private TileBase _selectedTile;
        private Vector3 _selectedTileOriginalPos;

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _levelManager = ServiceLocator.Get<LevelManager>();
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            _isEnabled = evt.NewState == GameStateType.Setup;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        private void Update()
        {
            if (!_isEnabled) return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnPointerDown();
            }
            else if (UnityEngine.Input.GetMouseButton(0) && _selectedTile != null)
            {
                OnPointerDrag();
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _selectedTile != null)
            {
                OnPointerUp();
            }
        }

        private void OnPointerDown()
        {
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, _gridLayer)) return;

            Vector2Int gridPos = _gridManager.WorldToGridPosition(hit.point);
            var tile = _gridManager.GetTile(gridPos);

            if (tile == null || tile.IsLocked) return;

            _selectedGridPos = gridPos;
            _selectedTile = tile;
            _dragStartPos = hit.point;
            _selectedTileOriginalPos = tile.transform.position;
            _isDragging = false;

            tile.AnimateSelect();
        }

        private void OnPointerDrag()
        {
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, _gridLayer)) return;

            float dist = Vector3.Distance(_dragStartPos, hit.point);

            if (!_isDragging && dist > _dragThreshold)
            {
                _isDragging = true;
                // Lift tile
                _selectedTile.transform.DOMove(
                    new Vector3(hit.point.x, _dragLiftHeight, hit.point.z),
                    0.15f).SetEase(Ease.OutQuad);
                _selectedTile.transform.DOScale(Vector3.one * _dragScale, 0.15f);
            }

            if (_isDragging)
            {
                _selectedTile.transform.position = new Vector3(hit.point.x, _dragLiftHeight, hit.point.z);
            }
        }

        private void OnPointerUp()
        {
            if (_isDragging)
            {
                HandleDragEnd();
            }
            else
            {
                HandleTap();
            }

            if (_selectedTile != null)
                _selectedTile.AnimateDeselect();

            _selectedTile = null;
            _isDragging = false;
        }

        private void HandleTap()
        {
            if (_selectedTile == null || !_selectedTile.CanRotate) return;

            var command = new RotateTileCommand(_selectedTile);
            _levelManager.CommandManager.ExecuteCommand(command);

            EventBus.Publish(new PlaySFXEvent { ClipName = "TileRotate" });
        }

        private void HandleDragEnd()
        {
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _gridLayer))
            {
                Vector2Int targetPos = _gridManager.WorldToGridPosition(hit.point);

                if (targetPos != _selectedGridPos && _gridManager.IsValidPosition(targetPos))
                {
                    var targetTile = _gridManager.GetTile(targetPos);
                    // Allow drop if target is empty OR target tile is not locked
                    if (targetTile == null || !targetTile.IsLocked)
                    {
                        // Execute swap (works for both swap-with-tile and move-to-empty)
                        var command = new SwapTilesCommand(_gridManager, _selectedGridPos, targetPos);
                        _levelManager.CommandManager.ExecuteCommand(command);

                        EventBus.Publish(new PlaySFXEvent { ClipName = "TileSwap" });
                        return;
                    }
                }
            }

            // Return to original position
            _selectedTile.transform.DOMove(_selectedTileOriginalPos, 0.3f).SetEase(Ease.OutBack);
            _selectedTile.transform.DOScale(Vector3.one, 0.2f);
        }
    }
}
