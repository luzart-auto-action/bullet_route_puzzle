using System.Collections.Generic;
using UnityEngine;
using BulletRoute.Core;
using BulletRoute.Tile;

namespace BulletRoute.Grid
{
    /// <summary>
    /// Lightweight data capturing the state of ONE tile at snapshot time.
    /// Used to detect and restore tiles that were destroyed/modified during simulation.
    /// </summary>
    [System.Serializable]
    public class TileSnapshot
    {
        public Vector2Int Position;
        public TileType Type;
        public int Rotation;
        public int ExtraData;   // PortalId for portals, mirror type for mirrors
        public bool IsLocked;
    }

    /// <summary>
    /// Captures and restores the entire grid state.
    /// Taken before each simulation so we can revert on miss.
    /// </summary>
    public class GridSnapshot
    {
        private readonly List<TileSnapshot> _tileSnapshots = new List<TileSnapshot>();

        public IReadOnlyList<TileSnapshot> Tiles => _tileSnapshots;

        /// <summary>
        /// Capture the current grid state: every non-null tile's type, rotation, and extra data.
        /// </summary>
        public void Capture(GridManager gridManager)
        {
            _tileSnapshots.Clear();

            foreach (var cell in gridManager.GetAllCells())
            {
                if (cell.TileInstance == null) continue;

                var tile = cell.TileInstance;
                var snap = new TileSnapshot
                {
                    Position = cell.Position,
                    Type = tile.TileType,
                    Rotation = tile.RotationState,
                    IsLocked = tile.IsLocked,
                    ExtraData = 0
                };

                // Capture extra data for special tile types
                if (tile is PortalTile portal)
                    snap.ExtraData = portal.PortalId;
                else if (tile is MirrorTile)
                    snap.ExtraData = 0; // Currently only forward slash is used

                _tileSnapshots.Add(snap);
            }
        }

        /// <summary>
        /// Compare snapshot vs current grid. Returns list of tiles that were
        /// destroyed during simulation and need to be re-created.
        /// </summary>
        public List<TileSnapshot> GetDestroyedTiles(GridManager gridManager)
        {
            var destroyed = new List<TileSnapshot>();

            foreach (var snap in _tileSnapshots)
            {
                var currentTile = gridManager.GetTile(snap.Position);
                if (currentTile == null)
                {
                    // Tile existed in snapshot but is gone now → destroyed
                    destroyed.Add(snap);
                }
            }

            return destroyed;
        }

        public void Clear()
        {
            _tileSnapshots.Clear();
        }
    }
}
