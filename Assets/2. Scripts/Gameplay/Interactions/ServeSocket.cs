using Data.Enums;
using Gameplay.Liquid;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Trigger volume in front of a customer seat. A Glass set down inside (released, at rest,
    /// with a stable fill) is bucketed to a discrete fill level and compared against the
    /// customer's requested level. Raises Served(glass, isCorrect) once per placement.
    ///
    /// Evaluation waits for the glass to settle (not held, not moving, fill stable) so the
    /// player can fill it either before or after placing it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ServeSocket : MonoBehaviour
    {
        [SerializeField] private RecipeId _targetRecipe = RecipeId.None;
        [Tooltip("Max speed (m/s) for the glass to count as 'set down'.")]
        [SerializeField] private float _restVelocity = 0.08f;
        [Tooltip("Seconds the fill must stay unchanged before evaluating (lets you top up in place).")]
        [SerializeField] private float _settleSeconds = 0.5f;

        // Recipe kept for the liquid/economy bookkeeping; correctness is by fill level.
        public RecipeId TargetRecipe { get => _targetRecipe; set => _targetRecipe = value; }
        // Requested fill level (index into FillLevels). Set by NightService on spawn.
        public int TargetLevel { get; set; }

        public Glass CurrentGlass { get; private set; }

        /// <summary>Fired once when a glass is served. bool = the fill level matched the order.</summary>
        public event System.Action<Glass, bool> Served;
        public event System.Action<Glass> GlassRemoved;

        private bool _servedThisPlacement;
        private float _lastFill = -1f;
        private float _settleTimer;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var glass = other.GetComponentInParent<Glass>();
            if (glass == null || glass == CurrentGlass) return;
            CurrentGlass = glass;
            _servedThisPlacement = false;
            _lastFill = -1f;
            _settleTimer = 0f;
        }

        void OnTriggerExit(Collider other)
        {
            var glass = other.GetComponentInParent<Glass>();
            if (glass == null || glass != CurrentGlass) return;
            var leaving = CurrentGlass;
            CurrentGlass = null;
            _servedThisPlacement = false;
            GlassRemoved?.Invoke(leaving);
        }

        void Update()
        {
            if (_servedThisPlacement || CurrentGlass == null) return;

            var rb = CurrentGlass.Body;
            if (rb != null && rb.isKinematic) return;                                              // held
            if (rb != null && rb.linearVelocity.sqrMagnitude > _restVelocity * _restVelocity) return; // moving

            float fill = CurrentGlass.Mix != null ? CurrentGlass.FillRatio : 0f;
            if (fill <= 0f) { _lastFill = fill; _settleTimer = 0f; return; }                       // empty

            // Wait until the fill stops changing, so topping up in place doesn't fire early.
            if (Mathf.Abs(fill - _lastFill) > 0.02f) { _lastFill = fill; _settleTimer = 0f; return; }
            _settleTimer += Time.deltaTime;
            if (_settleTimer < _settleSeconds) return;

            bool correct = FillLevels.BucketOf(fill) == FillLevels.Clamp(TargetLevel);
            _servedThisPlacement = true;
            Served?.Invoke(CurrentGlass, correct);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.3f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider b) Gizmos.DrawCube(transform.position + b.center, b.size);
        }
#endif
    }
}
