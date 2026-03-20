using UnityEngine;
using BulletRoute.Tile;

namespace BulletRoute.Grid
{
    public class GridCell
    {
        public Vector2Int Position { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public TileBase TileInstance { get; private set; }
        public bool IsOccupied => TileInstance != null;
        public bool IsLocked { get; set; }

        public GridCell(Vector2Int position, Vector3 worldPosition)
        {
            Position = position;
            WorldPosition = worldPosition;
        }

        public void SetTile(TileBase tile)
        {
            TileInstance = tile;
        }

        public void ClearTile()
        {
            TileInstance = null;
        }
    }
}
