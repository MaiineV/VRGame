using Data.Enums;
using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Base class for anything that holds liquid (Glass, Shaker). Owns the LiquidMix and
    /// notifies a renderer only when the mix actually changes.
    /// </summary>
    public abstract class LiquidContainer : MonoBehaviour
    {
        [SerializeField] protected float _capacityMl = 300f;
        [SerializeField] protected LiquidRenderer _renderer;
        [Tooltip("Minimum fill-ratio delta before the shader is updated (throttle). 0.01 = 1%.")]
        [SerializeField] protected float _refreshEpsilon = 0.01f;

        protected LiquidMix _mix;
        private float _lastRefreshedFill = -1f;

        public LiquidMix Mix => _mix;
        public float CapacityMl => _capacityMl;
        public float FillRatio => _capacityMl > 0f ? Mathf.Clamp01(_mix.TotalMl / _capacityMl) : 0f;
        public bool IsFull => _mix != null && _mix.TotalMl >= _capacityMl;

        protected virtual void Awake()
        {
            _mix = new LiquidMix();
        }

        public void Receive(IngredientId id, float volumeMl)
        {
            if (_mix == null || volumeMl <= 0f) return;
            float remaining = _capacityMl - _mix.TotalMl;
            if (remaining <= 0f) return;
            if (volumeMl > remaining) volumeMl = remaining;

            _mix.Add(id, volumeMl);
            MaybeRefresh();
        }

        public void Empty()
        {
            if (_mix == null) return;
            _mix.Clear();
            _lastRefreshedFill = -1f;
            if (_renderer != null) _renderer.Refresh(this);
        }

        private void MaybeRefresh()
        {
            if (_renderer == null) return;
            float fill = FillRatio;
            if (Mathf.Abs(fill - _lastRefreshedFill) < _refreshEpsilon && !IsFull) return;
            _lastRefreshedFill = fill;
            _renderer.Refresh(this);
        }
    }
}
