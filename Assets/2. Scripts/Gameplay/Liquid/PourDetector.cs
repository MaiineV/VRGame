using Data.Enums;
using Gameplay.Interactions;
using Services;
using Services.Audio;
using Services.Haptics;
using Services.UpdateService;
using Services.Vfx;
using UnityEngine;
using Utilities;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Ticks via IFixedUpdateListener only while the bottle is tilted past threshold.
    /// Raycasts down from the neck (NonAlloc) and pours into a LiquidContainer below.
    /// </summary>
    [RequireComponent(typeof(Bottle))]
    public sealed class PourDetector : MonoBehaviour, IFixedUpdateListener
    {
        [Header("Pour config")]
        [SerializeField] private float _tiltThresholdDeg = 60f;
        [SerializeField] private float _maxTiltDeg = 130f;
        [SerializeField] private float _maxRayDistance = 0.6f;
        [SerializeField] private LayerMask _containerMask = ~0;

        [Header("Visual")]
        [SerializeField] private PourStream _stream;

        [Header("Audio")]
        [SerializeField] private SfxId _pourSfx = SfxId.PourLoop;
        [Tooltip("Multiplier on the pour loop's configured volume. Kept low so the liquid sound is a " +
                 "subtle background trickle, not a roar.")]
        [Range(0f, 1f)]
        [SerializeField] private float _pourVolume = 0.05f;

        private Bottle _bottle;
        private GrabBridge _grab;
        private Transform _neck;
        private readonly RaycastHit[] _hits = new RaycastHit[4];
        private bool _registered;
        private bool _warnedNotReady;   // log the "service not ready" warning once, not every retry frame
        private bool _pouring;
        private int _pourLoopHandle;
        private float _splashTimer;    // throttles the splash burst (fixed update is ~50 fps)

        private const float SplashInterval = 0.08f;

        // Services resolved lazily once and reused, instead of a ServiceLocator.TryGet every fixed tick.
        private IAudioService _audio;
        private IVfxService _vfx;
        private IHapticService _haptics;

        void Awake()
        {
            _bottle = GetComponent<Bottle>();
            _neck = _bottle.Neck;
        }

        void OnEnable()
        {
            // Resolve the grab bridge defensively: Bottle.Awake normally caches it, but on a runtime-
            // instantiated bottle this OnEnable can run before that cache is populated, leaving
            // _bottle.Grab null. A null grab here used to throw and abort OnEnable BEFORE the tick was
            // registered — so the bottle never poured. Fall back to a direct GetComponent and, crucially,
            // always register for the tick regardless (the pour is gated on tilt+liquid, not on grab).
            _grab = _bottle != null ? _bottle.Grab : null;
            if (_grab == null) _grab = GetComponent<GrabBridge>();
            if (_grab != null)
            {
                _grab.Grabbed += HandleGrabbed;
                _grab.Released += HandleReleased;
            }

            RegisterForTick();
            if (_grab != null && _grab.IsHeld) HandleGrabbed();
        }

        // Retry the tick registration until it succeeds. A bottle instantiated at runtime (ShelfSlot) can
        // run OnEnable before IUpdateService is registered; without this retry RegisterForTick would log
        // "will NOT pour" once and give up, leaving a shop-spawned bottle permanently unable to pour. The
        // check is a single bool once registered (no per-frame TryGet after that).
        void Update()
        {
            if (!_registered) RegisterForTick();
        }

        void OnDisable()
        {
            if (_grab != null)
            {
                _grab.Grabbed -= HandleGrabbed;
                _grab.Released -= HandleReleased;
            }
            UnregisterFromTick();
            StopPouring();
        }

        private void HandleGrabbed()
        {
            if (_bottle.SO == null)
                MyLogger.LogWarning($"[PourDetector:{name}] BottleSO is null — assign it on the Bottle component in the inspector.");
            if (_bottle.SO != null && _bottle.SO.Ingredient == null)
                MyLogger.LogWarning($"[PourDetector:{name}] IngredientSO is null on {_bottle.SO.name}.");
        }

        private void HandleReleased()
        {
            StopPouring();
        }

        private void RegisterForTick()
        {
            if (_registered) return;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc))
            {
                // Not ready yet — Update() will retry. Warn only once so a few frames of startup lag
                // (runtime-spawned bottle racing service registration) don't flood the console.
                if (!_warnedNotReady)
                {
                    MyLogger.LogWarning($"[PourDetector:{name}] UpdateService not ready yet — retrying until available.");
                    _warnedNotReady = true;
                }
                return;
            }
            svc.AddFixedUpdateListener(this);
            _registered = true;
        }

        private void UnregisterFromTick()
        {
            if (!_registered) return;
            if (ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveFixedUpdateListener(this);
            _registered = false;
        }

        public void MyFixedUpdate()
        {
            float tiltDeg = Vector3.Angle(_neck.up, Vector3.up);
            bool tilted = tiltDeg >= _tiltThresholdDeg;

            // Pour only while a player hand actually holds the bottle. A tilted bottle sitting on the
            // shelf (or knocked over) must NOT leak liquid. _grab is resolved in OnEnable (with a
            // GetComponent fallback) so it's reliably non-null for a real bottle; if it somehow isn't,
            // we treat the bottle as not-held and refuse to pour rather than leak.
            if (_grab == null || !_grab.IsHeld)
            {
                StopPouring();
                return;
            }

            if (_bottle.SO == null || _bottle.IsEmpty)
            {
                StopPouring();
                return;
            }

            if (!tilted) { StopPouring(); return; }

            if (_bottle.SO.Ingredient == null)
            {
                StopPouring();
                return;
            }

            float t = Mathf.InverseLerp(_tiltThresholdDeg, _maxTiltDeg, tiltDeg);
            float rateMlSec = _bottle.SO.Ingredient.PourRateMlPerSec * t;
            float volume = _bottle.Consume(rateMlSec * Time.fixedDeltaTime);
            if (volume <= 0f) { StopPouring(); return; }

            _pouring = true;

            int hitCount = Physics.RaycastNonAlloc(
                _neck.position,
                Vector3.down,
                _hits,
                _maxRayDistance,
                _containerMask,
                QueryTriggerInteraction.Collide);

            LiquidContainer target = null;
            float closestDist = float.PositiveInfinity;
            Vector3 streamEnd = _neck.position + Vector3.down * _maxRayDistance;

            for (int i = 0; i < hitCount; i++)
            {
                var c = _hits[i].collider.GetComponentInParent<LiquidContainer>();
                if (c == null) continue;
                if (_hits[i].distance < closestDist)
                {
                    closestDist = _hits[i].distance;
                    target = c;
                    streamEnd = _hits[i].point;
                }
            }

            if (target != null)
            {
                target.Receive(_bottle.SO.Ingredient.Id, volume);

                // "Topped up" cue the moment the glass reaches the brim. GlassFull's retrigger throttle
                // (SfxDatabase) collapses the ~50 fps fixed tick down to a single ding.
                if (target.IsFull)
                {
                    if (_audio == null) ServiceLocator.TryGet<IAudioService>(out _audio);
                    _audio?.PlayOneShot(SfxId.GlassFull, streamEnd);
                }
            }

            if (_stream != null)
                _stream.Show(_neck.position, streamEnd, _bottle.SO.Ingredient.LiquidColor);

            // Splash droplets where the stream lands (only when it actually hits a container),
            // throttled so the 50 fps fixed tick doesn't flood particles.
            if (_splashTimer > 0f) _splashTimer -= Time.fixedDeltaTime;
            if (_vfx == null) ServiceLocator.TryGet<IVfxService>(out _vfx);
            if (target != null && _splashTimer <= 0f && _vfx != null)
            {
                _splashTimer = SplashInterval;
                _vfx.PlayBurst(VfxId.Splash, streamEnd, _bottle.SO.Ingredient.LiquidColor);
            }

            if (_audio == null) ServiceLocator.TryGet<IAudioService>(out _audio);
            if (_pourLoopHandle == 0 && _pourSfx != SfxId.None && _audio != null)
            {
                // Cap hard at 0.05 so the liquid is a barely-there trickle regardless of any louder
                // value still serialized on existing bottle instances.
                _pourLoopHandle = _audio.StartLoop(_pourSfx, _neck, _neck.position, Mathf.Min(_pourVolume, 0.05f));
            }

            // Light sustained buzz on the pouring hand. Re-issued each fixed tick (just over one
            // tick long) so it stays alive while pouring and dies on its own when we stop.
            if (_haptics == null) ServiceLocator.TryGet<IHapticService>(out _haptics);
            if (_grab != null && _grab.HeldByHand >= 0 && _haptics != null)
            {
                var ctrl = _grab.HeldByHand == 0 ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                _haptics.Pulse(ctrl, 0.18f, Time.fixedDeltaTime + 0.02f);
            }
        }

        private void StopPouring()
        {
            if (_pouring && _stream != null) _stream.Hide();
            if (_pourLoopHandle != 0)
            {
                if (_audio == null) ServiceLocator.TryGet<IAudioService>(out _audio);
                _audio?.StopLoop(_pourLoopHandle);
                _pourLoopHandle = 0;
            }
            _pouring = false;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var n = _neck != null ? _neck : transform;
            Gizmos.color = _pouring ? Color.cyan : Color.gray;
            Gizmos.DrawLine(n.position, n.position + Vector3.down * _maxRayDistance);
        }
#endif
    }
}
