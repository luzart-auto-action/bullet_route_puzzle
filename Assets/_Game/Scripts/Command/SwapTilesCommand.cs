using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;
using BulletRoute.Grid;
using BulletRoute.Tile;

namespace BulletRoute.Command
{
    public class SwapTilesCommand : ICommand
    {
        private readonly GridManager _gridManager;
        private readonly Vector2Int _posA;
        private readonly Vector2Int _posB;

        public SwapTilesCommand(GridManager gridManager, Vector2Int posA, Vector2Int posB)
        {
            _gridManager = gridManager;
            _posA = posA;
            _posB = posB;
        }

        public void Execute()
        {
            PerformSwap(_posA, _posB);
        }

        public void Undo()
        {
            PerformSwap(_posA, _posB); // Swap back
        }

        private void PerformSwap(Vector2Int a, Vector2Int b)
        {
            var tileA = _gridManager.GetTile(a);
            var tileB = _gridManager.GetTile(b);

            _gridManager.SwapTiles(a, b);

            // Animate swap with DOTween
            Vector3 posAWorld = _gridManager.GridToWorldPosition(a);
            Vector3 posBWorld = _gridManager.GridToWorldPosition(b);

            if (tileA != null) tileA.AnimateMoveTo(posBWorld);
            if (tileB != null) tileB.AnimateMoveTo(posAWorld);

            EventBus.Publish(new TileSwappedEvent { FromPos = a, ToPos = b });
        }
    }
}
