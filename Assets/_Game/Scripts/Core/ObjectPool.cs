using System.Collections.Generic;
using UnityEngine;

namespace BulletRoute.Core
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly int _initialSize;

        public ObjectPool(T prefab, Transform parent, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var obj = Object.Instantiate(_prefab, _parent);
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = Object.Instantiate(_prefab, _parent);
            }
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_parent);
            _pool.Enqueue(obj);
        }

        public void ReturnAll()
        {
            // This only returns tracked objects that are in the pool
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
        }
    }
}
