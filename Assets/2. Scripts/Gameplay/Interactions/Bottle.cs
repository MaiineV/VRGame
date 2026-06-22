using Data.SO;
using Gameplay.Systems;
using Services;
using Services.Economy;
using UnityEngine;

namespace Gameplay.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GrabBridge))]
    public sealed class Bottle : MonoBehaviour
    {
        [SerializeField] private BottleSO _so;
        [Tooltip("Unique id for THIS physical bottle in the scene (0 = none/free). Purchasable bottles " +
                 "need a distinct value so two bottles of the same ingredient are bought independently. " +
                 "BottleRespawner re-applies it when it recreates an owned bottle.")]
        [SerializeField] private int _instanceId;
        [SerializeField] private Transform _neck;
        [Tooltip("This bottle's own prefab. Set each bottle prefab to reference itself so BottleRespawner " +
                 "can destroy and recreate it at its origin when a night ends.")]
        [SerializeField] private GameObject _sourcePrefab;
        [Tooltip("Optional. If assigned, registers BottleSO.RepairCost as an expense when broken.")]
        [SerializeField] private Breakable _breakable;

        [Header("Physics (applied on Awake)")]
        [Tooltip("Mass in kg. A full glass bottle is roughly 1 kg.")]
        [SerializeField] private float _mass = 1.2f;
        [Tooltip("Linear damping (air resistance). Low so a thrown bottle flies instead of floating.")]
        [SerializeField] private float _linearDamping = 0.05f;
        [SerializeField] private float _angularDamping = 0.05f;

        private float _remainingMl;
        private bool _filled;

        public BottleSO SO => _so;
        /// <summary>Unique scene-instance id for per-bottle ownership (0 = none/free bottle).</summary>
        public int InstanceId => _instanceId;
        /// <summary>Re-apply the instance id after BottleRespawner recreates this bottle from its prefab.</summary>
        public void SetInstanceId(int id) => _instanceId = id;
        public GameObject SourcePrefab => _sourcePrefab;
        public Transform Neck => _neck != null ? _neck : transform;
        public float RemainingMl { get { EnsureFilled(); return _remainingMl; } }
        public bool IsEmpty { get { EnsureFilled(); return _remainingMl <= 0f; } }
        public GrabBridge Grab { get; private set; }
        public Rigidbody Body { get; private set; }

        void Awake()
        {
            Grab = GetComponent<GrabBridge>();
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.mass = _mass;
            Body.linearDamping = _linearDamping;
            Body.angularDamping = _angularDamping;
            // Keep the bottle upright when set down: a low centre of mass plus the flat-bottomed
            // BoxCollider on the prefab make it self-right instead of tipping (a CapsuleCollider's
            // rounded base balances on a point and falls over). Same trick as Glass.
            Body.centerOfMass = new Vector3(0f, -0.04f, 0f);

            if (_breakable == null) _breakable = GetComponent<Breakable>();
            if (_breakable != null) _breakable.Broken += HandleBroken;

            EnsureFilled();
        }

        void OnDestroy()
        {
            if (_breakable != null) _breakable.Broken -= HandleBroken;
        }

        private void HandleBroken(Breakable _)
        {
            if (_so == null || _so.RepairCost <= 0) return;
            if (!ServiceLocator.TryGet<IEconomyService>(out var economy)) return;
            economy.RegisterExpense(_so.RepairCost, $"Broken {_so.Ingredient?.DisplayName ?? "bottle"}");
        }

        public float Consume(float volumeMl)
        {
            EnsureFilled();
            if (volumeMl <= 0f || _remainingMl <= 0f) return 0f;
            float actual = volumeMl > _remainingMl ? _remainingMl : volumeMl;
            _remainingMl -= actual;
            return actual;
        }

        public void Refill()
        {
            if (_so != null) { _remainingMl = _so.CapacityMl; _filled = true; }
        }

        /// <summary>
        /// Set the remaining liquid directly, clamped to [0, CapacityMl]. Marks the bottle as filled
        /// so the lazy <see cref="EnsureFilled"/> can't override it. Used by NightService to fill the
        /// bottle from the stock the player bought in the day shop (instead of a free refill).
        /// </summary>
        public void SetRemaining(float ml)
        {
            float cap = _so != null ? _so.CapacityMl : ml;
            _remainingMl = Mathf.Clamp(ml, 0f, cap);
            _filled = true;
        }

        /// <summary>
        /// Fills the bottle from its BottleSO the first time it's needed. Guards the case where
        /// Awake ran before the SO reference was ready (or didn't run) — which left some bottles
        /// reading empty and refusing to pour while others worked.
        /// </summary>
        private void EnsureFilled()
        {
            if (_filled || _so == null) return;
            _remainingMl = _so.CapacityMl;
            _filled = true;
        }
    }
}
