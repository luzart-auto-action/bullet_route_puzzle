using System.Collections.Generic;
using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.FX
{
    [System.Serializable]
    public class FXEntry
    {
        public string Name;
        public ParticleSystem Prefab;
        public int PoolSize = 5;
        public float LifeTime = 2f;
    }

    public class FXManager : MonoBehaviour
    {
        [Header("FX Library")]
        [SerializeField] private List<FXEntry> _fxLibrary = new List<FXEntry>();

        private Dictionary<string, ObjectPool<ParticleSystem>> _pools = new Dictionary<string, ObjectPool<ParticleSystem>>();
        private Transform _fxContainer;

        private void Awake()
        {
            _fxContainer = new GameObject("[FXPool]").transform;
            _fxContainer.SetParent(transform);

            foreach (var entry in _fxLibrary)
            {
                if (entry.Prefab != null)
                {
                    var pool = new ObjectPool<ParticleSystem>(entry.Prefab, _fxContainer, entry.PoolSize);
                    _pools[entry.Name] = pool;
                }
            }

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<FXRequestEvent>(OnFXRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FXRequestEvent>(OnFXRequested);
        }

        private void OnFXRequested(FXRequestEvent evt)
        {
            SpawnFX(evt.FXName, evt.Position, evt.Rotation);
        }

        public ParticleSystem SpawnFX(string fxName, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(fxName, out var pool))
            {
                Debug.LogWarning($"[FXManager] FX not found: {fxName}. Add it to the FX Library.");
                return null;
            }

            var fx = pool.Get();
            fx.transform.position = position;
            fx.transform.rotation = rotation;
            fx.Play();

            // Auto-return after lifetime
            float lifetime = GetLifetime(fxName);
            StartCoroutine(ReturnAfterDelay(pool, fx, lifetime));

            return fx;
        }

        public ParticleSystem SpawnFX(string fxName, Transform parent)
        {
            var fx = SpawnFX(fxName, parent.position, parent.rotation);
            if (fx != null)
                fx.transform.SetParent(parent);
            return fx;
        }

        private float GetLifetime(string fxName)
        {
            foreach (var entry in _fxLibrary)
            {
                if (entry.Name == fxName)
                    return entry.LifeTime;
            }
            return 2f;
        }

        private System.Collections.IEnumerator ReturnAfterDelay(ObjectPool<ParticleSystem> pool, ParticleSystem fx, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (fx != null)
            {
                fx.Stop();
                fx.transform.SetParent(_fxContainer);
                pool.Return(fx);
            }
        }

        private void OnDestroy()
        {
            foreach (var pool in _pools.Values)
                pool.Clear();
            ServiceLocator.Unregister<FXManager>();
        }
    }
}
