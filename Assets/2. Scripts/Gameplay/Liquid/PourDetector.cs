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

        private Bottle _bottle;
        private GrabBridge _grab;
        private Transform _neck;
        private readonly RaycastHit[] _hits = new RaycastHit[4];
        private bool _registered;
        private bool _pouring;
        private int _pourLoopHandle;
        private float _diagCooldown;   // throttles the tilt-but-no-pour diagnostic
        private float _splashTimer;    // throttles the splash burst (fixed update is ~50 fps)

        private const float SplashInterval = 0.08f;

        public bool IsPouring => _pouring;
        public event System.Action<float> Poured; // volume ml per tick

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
                MyLogger.LogWarning($"[PourDetector:{name}] UpdateService NOT ready at register time — this bottle will NOT pour.");
                return;
            }
            svc.AddFixedUpdateListener(this);
            _registered = true;
            MyLogger.LogInfo($"[PourDetector:{name}] registered for tick (so={(_bottle.SO != null ? _bottle.SO.name : "NULL")}).");
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
            if (_diagCooldown > 0f) _diagCooldown -= Time.fixedDeltaTime;

            float tiltDeg = Vector3.Angle(_neck.up, Vector3.up);
            bool tilted = tiltDeg >= _tiltThresholdDeg;

            // Pour only while a player hand actually holds the bottle. A tilted bottle sitting on the
            // shelf (or knocked over) must NOT leak liquid. _grab is resolved in OnEnable (with a
            // GetComponent fallback) so it's reliably non-null for a real bottle; if it somehow isn't,
            // we treat the bottle as not-held and refuse to pour rather than leak.
            if (_grab == null || !_grab.IsHeld)
            {
                if (tilted) Diag($"bailed: not held (grab={(_grab != null ? "ok" : "NULL")})");
                StopPouring();
                return;
            }

            if (_bottle.SO == null || _bottle.IsEmpty)
            {
                if (tilted) Diag($"bailed: SO={( _bottle.SO != null ? _bottle.SO.name : "NULL")} empty={_bottle.IsEmpty} remaining={_bottle.RemainingMl:0}");
                StopPouring();
                return;
            }

            if (!tilted) { StopPouring(); return; }

            if (_bottle.SO.Ingredient == null)
            {
                Diag($"bailed: Ingredient NULL on {_bottle.SO.name}");
                StopPouring();
                return;
            }

            float t = Mathf.InverseLerp(_tiltThresholdDeg, _maxTiltDeg, tiltDeg);
            float rateMlSec = _bottle.SO.Ingredient.PourRateMlPerSec * t;
            float volume = _bottle.Consume(rateMlSec * Time.fixedDeltaTime);
            if (volume <= 0f) { Diag($"bailed: volume<=0 (rate={rateMlSec:0.0} remaining={_bottle.RemainingMl:0})"); StopPouring(); return; }

            Diag($"POURING tilt={tiltDeg:0} vol={volume:0.00} ing={_bottle.SO.Ingredient.name}");

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
                target.Receive(_bottle.SO.Ingredient.Id, volume);

            if (_stream != null)
                _stream.Show(_neck.position, streamEnd, _bottle.SO.Ingredient.LiquidColor);

            // Splash droplets where the stream lands (only when it actually hits a container),
            // throttled so the 50 fps fixed tick doesn't flood particles.
            if (_splashTimer > 0f) _splashTimer -= Time.fixedDeltaTime;
            if (target != null && _splashTimer <= 0f
                && ServiceLocator.TryGet<IVfxService>(out var vfx))
            {
                _splashTimer = SplashInterval;
                vfx.PlayBurst(VfxId.Splash, streamEnd, _bottle.SO.Ingredient.LiquidColor);
            }

            if (_pourLoopHandle == 0 && _pourSfx != SfxId.None
                && ServiceLocator.TryGet<IAudioService>(out var audio))
            {
                _pourLoopHandle = audio.StartLoop(_pourSfx, _neck, _neck.position);
            }

            // Light sustained buzz on the pouring hand. Re-issued each fixed tick (just over one
            // tick long) so it stays alive while pouring and dies on its own when we stop.
            if (_grab != null && _grab.HeldByHand >= 0
                && ServiceLocator.TryGet<IHapticService>(out var hap))
            {
                var ctrl = _grab.HeldByHand == 0 ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                hap.Pulse(ctrl, 0.18f, Time.fixedDeltaTime + 0.02f);
            }

            Poured?.Invoke(volume);
        }

        // Throttled per-bottle diagnostic: prints at most ~once/sec so logcat shows the
        // real pour gate for each bottle without flooding. Remove once the pour bug is closed.
        private void Diag(string msg)
        {
            if (_diagCooldown > 0f) return;
            _diagCooldown = 1f;
            MyLogger.LogInfo($"[PourDetector:{name}] {msg}");
        }

        private void StopPouring()
        {
            if (_pouring && _stream != null) _stream.Hide();
            if (_pourLoopHandle != 0 && ServiceLocator.TryGet<IAudioService>(out var audio))
            {
                audio.StopLoop(_pourLoopHandle);
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
