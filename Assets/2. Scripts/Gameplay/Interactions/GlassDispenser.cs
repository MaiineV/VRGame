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
        [Tooltip("Dispense one glass at scene start so the bar isn't empty.")]
        [SerializeField] private bool _spawnOneOnStart = true;

        [Header("Spawn button (poke)")]
        [SerializeField] private PokeButton _spawnButton;

        [Header("Controller fallback")]
        [Tooltip("Also spawn on a controller button, in case the poke button is unreliable in-headset.")]
        [SerializeField] private bool _enableControllerFallback = true;
        [SerializeField] private OVRInput.Button _fallbackButton = OVRInput.Button.Three;      // X
        [SerializeField] private OVRInput.Controller _fallbackController = OVRInput.Controller.LTouch;

        private IGlassPoolService _pool;
        private bool _disabled;

        void Start()
        {
            if (!ServiceLocator.TryGet<IGlassPoolService>(out _pool))
            {
                MyLogger.LogWarning($"[GlassDispenser:{name}] No IGlassPoolService registered — dispenser disabled.");
                _disabled = true;
                return;
            }
            if (_glassPrefab == null || _glassPrefab.GetComponent<Glass>() == null)
            {
                MyLogger.LogWarning($"[GlassDispenser:{name}] Glass prefab missing or has no Glass component — dispenser disabled.");
                _disabled = true;
                return;
            }

            int seats = _seatCountOverride > 0
                ? _seatCountOverride
                : FindObjectsByType<CustomerSeatPoint>(FindObjectsSortMode.None).Length;
            _pool.Capacity = Mathf.Max(1, seats + _buffer);
            MyLogger.LogInfo($"[GlassDispenser:{name}] Glass budget = {seats} seats + {_buffer} = {_pool.Capacity}.");

            if (_spawnButton != null) _spawnButton.Pressed += TrySpawn;

            if (_spawnOneOnStart) TrySpawn();
        }

        void OnDisable()
        {
            if (_spawnButton != null) _spawnButton.Pressed -= TrySpawn;
        }

        void Update()
        {
            if (_disabled || _pool == null) return;

            // Gray out the physical button while the budget is exhausted: PokeButton ignores
            // presses (and stays silent) when not interactable, signalling "recycle one first".
            if (_spawnButton != null) _spawnButton.Interactable = _pool.CanSpawn;

            if (_enableControllerFallback && OVRInput.GetDown(_fallbackButton, _fallbackController))
                TrySpawn();
        }

        /// <summary>Spawn one pooled glass if the budget allows; no-op when at the cap.</summary>
        public void TrySpawn()
        {
            if (_disabled || _pool == null || !_pool.CanSpawn) return;
            var t = _spawnPoint != null ? _spawnPoint : transform;
            _pool.Spawn(_glassPrefab, t.position, t.rotation);
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
