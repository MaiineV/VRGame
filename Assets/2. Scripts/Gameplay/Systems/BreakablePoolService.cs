using System.Collections.Generic;
using Services;
using UnityEngine;
using Utilities.Pool;

namespace Gameplay.Systems
{
    /// <summary>
    /// Registry of per-prefab pools for pre-fractured "shard" instances.
    /// Avoids Instantiate/Destroy on every break — critical for VR framerate.
    /// </summary>
    public interface IBreakablePoolService : IGameService
    {
        GameObject Spawn(GameObject shardsPrefab, Vector3 position, Quaternion rotation);
        void Return(GameObject shardsPrefab, GameObject instance);
    }

    public sealed class BreakablePoolService : IBreakablePoolService
    {
        private readonly Dictionary<GameObject, PoolGeneric<GameObject>> _pools = new();
        private Transform _root;

        public void Initialize()
        {
            var go = new GameObject("[BreakablePool]");
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
        }

        public GameObject Spawn(GameObject shardsPrefab, Vector3 position, Quaternion rotation)
        {
            if (shardsPrefab == null) return null;
            var pool = GetPool(shardsPrefab);
            var inst = pool.GetOrCreate();
            inst.transform.SetPositionAndRotation(position, rotation);
            inst.SetActive(true);
            return inst;
        }

        public void Return(GameObject shardsPrefab, GameObject instance)
        {
            if (shardsPrefab == null || instance == null) return;
            GetPool(shardsPrefab).ReturnToPool(instance);
        }

        private PoolGeneric<GameObject> GetPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new PoolGeneric<GameObject>(prefab, _root);
                _pools[prefab] = pool;
            }
            return pool;
        }
    }
}
