using System.Collections.Generic;
using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.Bullet
{
    public class BulletManager : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private BulletController _bulletPrefab;
        [SerializeField] private Transform _bulletContainer;

        private List<BulletController> _activeBullets = new List<BulletController>();

        private void Awake()
        {
            if (_bulletContainer == null)
            {
                _bulletContainer = new GameObject("[Bullets]").transform;
                _bulletContainer.SetParent(transform);
            }

            ServiceLocator.Register(this);
        }

        public BulletController SpawnBullet()
        {
            if (_bulletPrefab == null)
            {
                Debug.LogError("[BulletManager] Bullet prefab not assigned!");
                return null;
            }

            var bullet = Instantiate(_bulletPrefab, _bulletContainer);
            _activeBullets.Add(bullet);
            return bullet;
        }

        public void ReturnBullet(BulletController bullet)
        {
            if (bullet == null) return;
            bullet.Deactivate();
            _activeBullets.Remove(bullet);
            Destroy(bullet.gameObject);
        }

        public void ReturnAllBullets()
        {
            foreach (var b in _activeBullets)
            {
                if (b != null)
                {
                    b.Deactivate();
                    Destroy(b.gameObject);
                }
            }
            _activeBullets.Clear();
        }

        private void OnDestroy()
        {
            ReturnAllBullets();
            ServiceLocator.Unregister<BulletManager>();
        }
    }
}
