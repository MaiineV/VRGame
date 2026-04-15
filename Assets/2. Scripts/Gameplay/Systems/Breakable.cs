using Data.Enums;
using Services;
using Services.Audio;
using Services.UpdateService;
using UnityEngine;

namespace Gameplay.Systems
{
    /// <summary>
    /// Swaps this object with a pre-fractured prefab when impact impulse exceeds a threshold.
    /// Fractured instances come from BreakablePoolService (no runtime Instantiate).
    /// The original object is disabled (returned to its own pool by whoever spawned it, if any)
    /// or destroyed as fallback.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Breakable : MonoBehaviour
    {
        [SerializeField] private GameObject _shardsPrefab;
        [Tooltip("Minimum collision impulse magnitude to trigger a break.")]
        [SerializeField] private float _impulseThreshold = 2.5f;
        [Tooltip("Seconds before shards are returned to the pool.")]
        [SerializeField] private float _shardLifetime = 4f;
        [Tooltip("Optional extra outward velocity applied to shard rigidbodies on spawn.")]
        [SerializeField] private float _shardExplosionForce = 0.5f;
        [SerializeField] private SfxId _breakSfx = SfxId.GlassBreak;

        public event System.Action<Breakable> Broken;

        private bool _broken;

        void OnCollisionEnter(Collision col)
        {
            if (_broken || _shardsPrefab == null) return;
            if (col.impulse.magnitude < _impulseThreshold) return;
            Break();
        }

        public void Break()
        {
            if (_broken) return;
            _broken = true;

            if (!ServiceLocator.TryGet<IBreakablePoolService>(out var pool))
            {
                Destroy(gameObject);
                return;
            }

            var shards = pool.Spawn(_shardsPrefab, transform.position, transform.rotation);
            if (shards != null && _shardExplosionForce > 0f)
                ApplyExplosion(shards.transform, _shardExplosionForce);

            Broken?.Invoke(this);

            if (_breakSfx != SfxId.None && ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.PlayOneShot(_breakSfx, transform.position);

            gameObject.SetActive(false);
            ReturnShardsLater.Schedule(_shardsPrefab, shards, _shardLifetime);
        }

        private static void ApplyExplosion(Transform shardsRoot, float force)
        {
            var bodies = shardsRoot.GetComponentsInChildren<Rigidbody>();
            var center = shardsRoot.position;
            for (int i = 0; i < bodies.Length; i++)
            {
                var dir = bodies[i].worldCenterOfMass - center;
                if (dir.sqrMagnitude < 0.0001f) dir = Random.onUnitSphere;
                bodies[i].AddForce(dir.normalized * force, ForceMode.VelocityChange);
            }
        }
    }

    /// <summary>
    /// Lightweight timer that returns a pooled shard instance after N seconds.
    /// Registers with IUpdateService while waiting; self-destructs on completion.
    /// </summary>
    internal sealed class ReturnShardsLater : MonoBehaviour, IUpdateListener
    {
        private GameObject _prefabKey;
        private GameObject _instance;
        private float _timer;
        private IUpdateService _updates;
        private IBreakablePoolService _pool;

        public static void Schedule(GameObject prefabKey, GameObject instance, float seconds)
        {
            if (instance == null || prefabKey == null) return;
            var go = new GameObject("[ShardReturn]");
            Object.DontDestroyOnLoad(go);
            var t = go.AddComponent<ReturnShardsLater>();
            t._prefabKey = prefabKey;
            t._instance = instance;
            t._timer = seconds;
        }

        void OnEnable()
        {
            ServiceLocator.TryGet<IBreakablePoolService>(out _pool);
            if (ServiceLocator.TryGet<IUpdateService>(out _updates))
                _updates.AddUpdateListener(this);
        }

        void OnDisable()
        {
            if (_updates != null) _updates.RemoveUpdateListener(this);
            _updates = null;
        }

        public void MyUpdate()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _pool?.Return(_prefabKey, _instance);
            Destroy(gameObject);
        }
    }
}
