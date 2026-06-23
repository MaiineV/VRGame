using System.Collections.Generic;
using Gameplay.Interactions;
using Services;
using UnityEngine;
using Utilities.Pool;

namespace Gameplay.Systems
{
    /// <summary>
    /// Registry of per-prefab pools for Glass instances.
    /// Avoids Instantiate/Destroy on every dispense/serve — keeps the VR frame budget steady.
    /// Mirrors CustomerPoolService in structure. Each spawned glass remembers its source prefab
    /// so callers can return it without tracking the key themselves.
    /// </summary>
    public interface IGlassPoolService : IGameService
    {
        /// <summary>Max glasses allowed live in the world at once. 0 or less = unlimited.</summary>
        int Capacity { get; set; }

        /// <summary>Glasses currently spawned and not yet returned.</summary>
        int LiveCount { get; }

        /// <summary>True when another glass may be spawned without exceeding <see cref="Capacity"/>.</summary>
        bool CanSpawn { get; }

        /// <summary>
        /// Returns a fresh, reset Glass from the pool for <paramref name="prefab"/> (empty liquid,
        /// zeroed physics, not held), instantiating only on a cache miss. SetActive(true) on return.
        /// Returns null when the live budget is exhausted (<see cref="CanSpawn"/> is false).
        /// </summary>
        Glass Spawn(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>Deactivates the glass, returns it to its bucket, and frees a budget slot.</summary>
        void Return(Glass instance);

        /// <summary>Recycle the OLDEST live glass that isn't currently held by the player (and isn't
        /// being carried by a customer), freeing a budget slot. Lets the dispenser hand out a fresh
        /// glass at the cap instead of going dead. Returns false if every live glass is in use.</summary>
        bool RecycleOldestUnheld();

        /// <summary>
        /// Raised right after a glass leaves play (served-and-carried-off, or trashed) and its
        /// budget slot is freed. Lets the dispenser auto-refill so the bar always has a glass ready.
        /// </summary>
        event System.Action<Glass> Returned;
    }

    public sealed class GlassPoolService : IGlassPoolService
    {
        private readonly Dictionary<GameObject, PoolGeneric<Glass>> _pools = new();
        private readonly List<Glass> _live = new();   // spawn order, oldest first; for cap eviction
        private Transform _root;

        public int Capacity { get; set; }
        public int LiveCount { get; private set; }
        public bool CanSpawn => Capacity <= 0 || LiveCount < Capacity;

        public event System.Action<Glass> Returned;

        public void Initialize()
        {
            var go = new GameObject("[GlassPool]");
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
        }

        public Glass Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null || !CanSpawn) return null;
            var pool = GetPool(prefab);
            var inst = pool.GetOrCreate();
            if (inst == null) return null;

            inst.SourcePrefab = prefab;
            inst.transform.SetPositionAndRotation(position, rotation);
            inst.gameObject.SetActive(true);
            inst.ResetForPool();
            LiveCount++;
            _live.Add(inst);
            return inst;
        }

        public bool RecycleOldestUnheld()
        {
            for (int i = 0; i < _live.Count; i++)
            {
                var g = _live[i];
                if (g == null) continue;
                // Skip a glass the player is holding or a customer is carrying (parented off the pool root).
                var grab = g.GetComponent<Gameplay.Interactions.GrabBridge>();
                if (grab != null && grab.IsHeld) continue;
                if (g.transform.parent != null && g.transform.parent != _root) continue;
                Return(g);
                return true;
            }
            return false;
        }

        public void Return(Glass instance)
        {
            if (instance == null) return;
            _live.Remove(instance);
            if (LiveCount > 0) LiveCount--;
            var prefab = instance.SourcePrefab;
            if (prefab == null) { Object.Destroy(instance.gameObject); Returned?.Invoke(instance); return; }
            GetPool(prefab).ReturnToPool(instance);
            // Slot is already freed (LiveCount--), so a listener may safely Spawn a replacement.
            Returned?.Invoke(instance);
        }

        private PoolGeneric<Glass> GetPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new PoolGeneric<Glass>(prefab.GetComponent<Glass>(), _root);
                _pools[prefab] = pool;
            }
            return pool;
        }
    }
}
