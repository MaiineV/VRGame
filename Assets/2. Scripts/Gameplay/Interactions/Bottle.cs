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
        [SerializeField] private Transform _neck;
        [Tooltip("Optional. If assigned, registers BottleSO.RepairCost as an expense when broken.")]
        [SerializeField] private Breakable _breakable;

        private float _remainingMl;

        public BottleSO SO => _so;
        public Transform Neck => _neck != null ? _neck : transform;
        public float RemainingMl => _remainingMl;
        public bool IsEmpty => _remainingMl <= 0f;
        public GrabBridge Grab { get; private set; }
        public Rigidbody Body { get; private set; }

        void Awake()
        {
            Grab = GetComponent<GrabBridge>();
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (_breakable == null) _breakable = GetComponent<Breakable>();
            if (_breakable != null) _breakable.Broken += HandleBroken;

            if (_so != null) _remainingMl = _so.CapacityMl;
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
            if (volumeMl <= 0f || _remainingMl <= 0f) return 0f;
            float actual = volumeMl > _remainingMl ? _remainingMl : volumeMl;
            _remainingMl -= actual;
            return actual;
        }

        public void Refill()
        {
            if (_so != null) _remainingMl = _so.CapacityMl;
        }
    }
}
