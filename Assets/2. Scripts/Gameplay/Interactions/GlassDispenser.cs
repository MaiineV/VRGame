using Gameplay.Customer;
using Gameplay.Systems;
using Services;
using UI.Diegetic;
using Utilities;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// On-demand glass dispenser with a limited budget. A physical poke button (with a controller
    /// fallback) spawns one pooled glass per press, up to a cap of <c>seats + buffer</c>. A glass
    /// frees its budget slot only when it leaves play — served and carried off by a customer
    /// (LeavingState) or dropped in the trash bin (GlassTrashBin). While at the cap the button
    /// goes non-interactable, so you must recycle a glass before spawning another.
    /// </summary>
    public sealed class GlassDispenser : MonoBehaviour
    {
        [Header("Glass")]
        [Tooltip("Glass prefab to dispense. Must have a Glass + GrabBridge.")]
        [SerializeField] private GameObject _glassPrefab;
        [Tooltip("Where each glass appears. Defaults to this transform if null.")]
        [SerializeField] private Transform _spawnPoint;

        [Header("Budget")]
        [Tooltip("Spare glasses on top of the seat count (re-pour mistakes / leftovers). Cap = seats + buffer.")]
        [SerializeField] private int _buffer = 2;
        [Tooltip("Override the auto seat count. 0 = count CustomerSeatPoints in the scene.")]
        [SerializeField] private int _seatCountOverride = 0;
        [Tooltip("Hard ceiling on glasses live in the world at once, regardless of seats + buffer.")]
        [SerializeField] private int _maxGlasses = 12;
        [Tooltip("Dispense one glass at scene start so the bar isn't empty.")]
        [SerializeField] private bool _spawnOneOnStart = true;
        [Tooltip("Auto-dispense a replacement whenever a glass leaves play (a customer carries it " +
                 "off, or it's trashed), so the bar is never left empty. Still respects the budget cap.")]
        [SerializeField] private bool _autoRefillOnReturn = true;

        [Header("Spawn button (poke)")]
        [SerializeField] private PokeButton _spawnButton;

        [Header("Controller fallback")]
        [Tooltip("Also spawn on a controller button, in case the poke button is unreliable in-headset.")]
        [SerializeField] private bool _enableControllerFallback = true;
        [SerializeField] private OVRInput.Button _fallbackButton = OVRInput.Button.Three;      // X
        [SerializeField] private OVRInput.Controller _fallbackController = OVRInput.Controller.LTouch;

        private IGlassPoolService _pool;
        private bool _disabled;
        private bool _subscribed;

        void Start()
        {
            if (_glassPrefab == null || _glassPrefab.GetComponent<Glass>() == null)
            {
                MyLogger.LogWarning($"[GlassDispenser:{name}] Glass prefab missing or has no Glass component — dispenser disabled.");
                _disabled = true;
                return;
            }

            EnsurePool();

            if (_spawnButton != null) _spawnButton.Pressed += TrySpawn;

            if (_spawnOneOnStart) TrySpawn();
        }

        void OnEnable()
        {
            // Pool may already be resolved from a previous enable cycle; re-hook if so.
            SubscribePool();
        }

        void OnDisable()
        {
            if (_spawnButton != null) _spawnButton.Pressed -= TrySpawn;
            UnsubscribePool();
        }

        void Update()
        {
            if (_disabled) return;

            EnsurePool();

            // Gray out the physical button while the budget is exhausted (PokeButton stays silent
            // when not interactable). With no pool service (no bootstrap) it's always interactable.
            // Interactable when a fresh glass can be spawned OR there's a live glass we could recycle to
            // make room (TrySpawn evicts the oldest free one at the cap). Only goes dead with no pool.
            if (_spawnButton != null) _spawnButton.Interactable = _pool == null || _pool.CanSpawn || _pool.LiveCount > 0;

            if (_enableControllerFallback && OVRInput.GetDown(_fallbackButton, _fallbackController))
                TrySpawn();
        }

        /// <summary>Lazily resolve the pool service and set its budget the first time it appears.</summary>
        private void EnsurePool()
        {
            if (_pool != null) return;
            if (!ServiceLocator.TryGet<IGlassPoolService>(out _pool)) return;

            int seats = _seatCountOverride > 0
                ? _seatCountOverride
                : FindObjectsByType<CustomerSeatPoint>(FindObjectsSortMode.None).Length;
            int budget = Mathf.Max(1, seats + _buffer);
            if (_maxGlasses > 0) budget = Mathf.Min(budget, _maxGlasses);
            _pool.Capacity = budget;
            MyLogger.LogInfo($"[GlassDispenser:{name}] Glass budget = min({seats} seats + {_buffer}, cap {_maxGlasses}) = {_pool.Capacity}.");

            SubscribePool();
        }

        private void SubscribePool()
        {
            if (_subscribed || _pool == null) return;
            _pool.Returned += OnGlassReturned;
            _subscribed = true;
        }

        private void UnsubscribePool()
        {
            if (!_subscribed || _pool == null) return;
            _pool.Returned -= OnGlassReturned;
            _subscribed = false;
        }

        /// <summary>
        /// A glass just left play and freed its budget slot. Replace it at the dispenser so the
        /// player always has a fresh glass ready. <see cref="TrySpawn"/> respects the budget cap.
        /// </summary>
        private void OnGlassReturned(Glass _)
        {
            if (_disabled || !_autoRefillOnReturn) return;
            TrySpawn();
        }

        /// <summary>
        /// Spawn one glass. Uses the pooled budget when the service is available; otherwise falls
        /// back to a plain Instantiate so the scene still works when played without the bootstrap.
        /// </summary>
        public void TrySpawn()
        {
            if (_disabled) return;
            EnsurePool();

            var t = _spawnPoint != null ? _spawnPoint : transform;
            if (_pool != null)
            {
                // At the cap, recycle the oldest free glass so the player can always get a fresh one
                // instead of the dispenser going dead with stale glasses cluttering the bar.
                if (!_pool.CanSpawn) _pool.RecycleOldestUnheld();
                if (!_pool.CanSpawn) return;
                _pool.Spawn(_glassPrefab, t.position, t.rotation);
            }
            else
            {
                Instantiate(_glassPrefab, t.position, t.rotation);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var t = _spawnPoint != null ? _spawnPoint : transform;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireSphere(t.position, 0.05f);
        }
#endif
    }
}
