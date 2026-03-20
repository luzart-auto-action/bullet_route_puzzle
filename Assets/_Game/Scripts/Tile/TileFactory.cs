using System.Collections.Generic;
using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    [System.Serializable]
    public class TilePrefabEntry
    {
        public TileType Type;
        public TileBase Prefab;
    }

    public class TileFactory : MonoBehaviour
    {
        [SerializeField] private List<TilePrefabEntry> _tilePrefabs = new List<TilePrefabEntry>();

        private Dictionary<TileType, TileBase> _prefabMap;

        private void Awake()
        {
            _prefabMap = new Dictionary<TileType, TileBase>();
            foreach (var entry in _tilePrefabs)
            {
                if (entry.Prefab != null && !_prefabMap.ContainsKey(entry.Type))
                    _prefabMap[entry.Type] = entry.Prefab;
            }
            ServiceLocator.Register(this);
        }

        public TileBase CreateTile(TileType type, Vector2Int gridPos, Transform parent = null)
        {
            if (!_prefabMap.TryGetValue(type, out var prefab))
            {
                Debug.LogError($"[TileFactory] No prefab for tile type: {type}");
                return null;
            }

            var tile = Instantiate(prefab, parent);
            tile.GridPosition = gridPos;
            tile.name = $"Tile_{type}_{gridPos.x}_{gridPos.y}";
            return tile;
        }

        public TileBase CreateTile(TileType type, Vector2Int gridPos, int rotation, Transform parent = null)
        {
            var tile = CreateTile(type, gridPos, parent);
            if (tile != null)
                tile.SetRotation(rotation);
            return tile;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TileFactory>();
        }
    }
}
