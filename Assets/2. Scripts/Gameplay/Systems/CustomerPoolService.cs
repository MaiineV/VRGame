using System.Collections.Generic;
using Gameplay.Customer;
using Services;
using UnityEngine;
using Utilities.Pool;

namespace Gameplay.Systems
{
    /// <summary>
    /// Registry of per-prefab pools for CustomerEntity instances.
    /// Avoids Instantiate/Destroy on every customer spawn — critical for VR framerate.
    /// Mirrors BreakablePoolService exactly in structure.
    /// </summary>
    public interface ICustomerPoolService : IGameService
    {
        /// <summary>
        /// Returns an active CustomerEntity from the pool for <paramref name="prefab"/>,
        /// instantiating a new one only on a cache miss.
        /// The GameObject is SetActive(true) before returning.
        /// </summary>
        CustomerEntity Spawn(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Deactivates <paramref name="instance"/> and returns it to the pool bucket
        /// keyed by <paramref name="prefab"/>.
        /// Callers must have already unsubscribed events and called ReturnToPool on
        /// the entity before invoking this.
        /// </summary>
        void Return(GameObject prefab, CustomerEntity instance);
    }

    public sealed class CustomerPoolService : ICustomerPoolService
    {
        private readonly Dictionary<GameObject, PoolGeneric<CustomerEntity>> _pools = new();
        private Transform _root;

        public void Initialize()
        {
            var go = new GameObject("[CustomerPool]");
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
        }

        public CustomerEntity Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            var pool = GetPool(prefab);
            var inst = pool.GetOrCreate();
            if (inst == null) return null;
            inst.transform.SetPositionAndRotation(position, rotation);
            inst.gameObject.SetActive(true);
            return inst;
        }

        public void Return(GameObject prefab, CustomerEntity instance)
        {
            if (prefab == null || instance == null) return;
            GetPool(prefab).ReturnToPool(instance);
        }

        private PoolGeneric<CustomerEntity> GetPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new PoolGeneric<CustomerEntity>(prefab.GetComponent<CustomerEntity>(), _root);
                _pools[prefab] = pool;
            }
            return pool;
        }
    }
}
