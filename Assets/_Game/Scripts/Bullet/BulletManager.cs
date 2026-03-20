using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.Bullet
{
    public class BulletManager : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private BulletController _bulletPrefab;
        [SerializeField] private int _poolSize = 10;
        [SerializeField] private Transform _bulletContainer;

        private ObjectPool<BulletController> _pool;

        private void Awake()
        {
            if (_bulletContainer == null)
            {
                _bulletContainer = new GameObject("[BulletPool]").transform;
                _bulletContainer.SetParent(transform);
            }

            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (_bulletPrefab != null)
                _pool = new ObjectPool<BulletController>(_bulletPrefab, _bulletContainer, _poolSize);
        }

        public BulletController SpawnBullet()
        {
            if (_pool == null)
            {
                Debug.LogError("[BulletManager] Pool not initialized. Assign bullet prefab!");
                return null;
            }
            return _pool.Get();
        }

        public void ReturnBullet(BulletController bullet)
        {
            if (bullet == null) return;
            bullet.Deactivate();
            _pool.Return(bullet);
        }

        public void ReturnAllBullets()
        {
            // Find all active bullets and return them
            var bullets = _bulletContainer.GetComponentsInChildren<BulletController>();
            foreach (var b in bullets)
            {
                if (b.gameObject.activeSelf)
                {
                    b.Deactivate();
                    _pool.Return(b);
                }
            }
        }

        private void OnDestroy()
        {
            _pool?.Clear();
            ServiceLocator.Unregister<BulletManager>();
        }
    }
}
