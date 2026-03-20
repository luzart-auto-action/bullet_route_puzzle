using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Tile;

namespace BulletRoute.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float _cellSize = 1.2f;
        [SerializeField] private float _cellSpacing = 0.1f;
        [SerializeField] private Transform _gridParent;

        [Header("Animation")]
        [SerializeField] private float _cellAppearDuration = 0.3f;
        [SerializeField] private float _cellAppearDelay = 0.03f;
        [SerializeField] private Ease _cellAppearEase = Ease.OutBack;

        private GridCell[,] _cells;
        private int _width;
        private int _height;
        private Vector3 _gridOrigin;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public float TotalCellSize => _cellSize + _cellSpacing;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void InitializeGrid(int width, int height)
        {
            ClearGrid();
            _width = width;
            _height = height;
            _cells = new GridCell[width, height];

            // Center the grid
            float totalWidth = width * TotalCellSize - _cellSpacing;
            float totalHeight = height * TotalCellSize - _cellSpacing;
            _gridOrigin = new Vector3(-totalWidth / 2f + _cellSize / 2f, 0f, -totalHeight / 2f + _cellSize / 2f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = new GridCell(new Vector2Int(x, y), GridToWorldPosition(x, y));
                    _cells[x, y] = cell;
                }
            }

            EventBus.Publish(new GridReadyEvent { Width = width, Height = height });
        }

        public void AnimateGridAppear(System.Action onComplete = null)
        {
            Sequence seq = DOTween.Sequence();
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    if (cell.TileInstance != null)
                    {
                        var t = cell.TileInstance.transform;
                        t.localScale = Vector3.zero;
                        float delay = (x + y) * _cellAppearDelay;
                        seq.Insert(delay, t.DOScale(Vector3.one, _cellAppearDuration).SetEase(_cellAppearEase));
                    }
                }
            }
            seq.OnComplete(() => onComplete?.Invoke());
        }

        public Vector3 GridToWorldPosition(int x, int y)
        {
            return _gridOrigin + new Vector3(x * TotalCellSize, 0f, y * TotalCellSize);
        }

        public Vector3 GridToWorldPosition(Vector2Int pos)
        {
            return GridToWorldPosition(pos.x, pos.y);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 local = worldPos - _gridOrigin;
            int x = Mathf.RoundToInt(local.x / TotalCellSize);
            int y = Mathf.RoundToInt(local.z / TotalCellSize);
            return new Vector2Int(
                Mathf.Clamp(x, 0, _width - 1),
                Mathf.Clamp(y, 0, _height - 1)
            );
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _width && pos.y >= 0 && pos.y < _height;
        }

        public GridCell GetCell(Vector2Int pos)
        {
            if (!IsValidPosition(pos)) return null;
            return _cells[pos.x, pos.y];
        }

        public GridCell GetCell(int x, int y)
        {
            return GetCell(new Vector2Int(x, y));
        }

        public void SetTile(Vector2Int pos, TileBase tile)
        {
            var cell = GetCell(pos);
            if (cell == null) return;
            cell.SetTile(tile);
            if (tile != null)
            {
                tile.transform.position = GridToWorldPosition(pos);
                tile.GridPosition = pos;
            }
        }

        public TileBase GetTile(Vector2Int pos)
        {
            var cell = GetCell(pos);
            return cell?.TileInstance;
        }

        public void SwapTiles(Vector2Int posA, Vector2Int posB)
        {
            var cellA = GetCell(posA);
            var cellB = GetCell(posB);
            if (cellA == null || cellB == null) return;

            var tileA = cellA.TileInstance;
            var tileB = cellB.TileInstance;

            cellA.SetTile(tileB);
            cellB.SetTile(tileA);

            if (tileA != null) tileA.GridPosition = posB;
            if (tileB != null) tileB.GridPosition = posA;
        }

        public List<GridCell> GetAllCells()
        {
            var cells = new List<GridCell>();
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    cells.Add(_cells[x, y]);
            return cells;
        }

        public void ClearGrid()
        {
            if (_cells == null) return;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    if (cell?.TileInstance != null)
                        Destroy(cell.TileInstance.gameObject);
                }
            }
            _cells = null;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GridManager>();
        }
    }
}
