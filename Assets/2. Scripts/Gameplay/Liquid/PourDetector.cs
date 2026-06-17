using Data.Enums;
using Gameplay.Interactions;
using Services;
using Services.Audio;
using Services.UpdateService;
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
        private Transform _neck;
        private readonly RaycastHit[] _hits = new RaycastHit[4];
        private bool _registered;
        private bool _pouring;
        private int _pourLoopHandle;

        public bool IsPouring => _pouring;
        public event System.Action<float> Poured; // volume ml per tick

        void Awake()
        {
            _bottle = GetComponent<Bottle>();
            _neck = _bottle.Neck;
        }

        void OnEnable()
        {
            _bottle.Grab.Grabbed += HandleGrabbed;
            _bottle.Grab.Released += HandleReleased;
            // Tick regardless of grab state. The pour only actually happens when the bottle is
            // tilted past the threshold and has liquid (gated in MyFixedUpdate), so always ticking
            // is safe — and it avoids depending on the grab system reliably firing Grabbed, which
            // left most bottles unable to pour while only one happened to work.
            RegisterForTick();
            if (_bottle.Grab.IsHeld) HandleGrabbed();
        }

        void OnDisable()
        {
            _bottle.Grab.Grabbed -= HandleGrabbed;
            _bottle.Grab.Released -= HandleReleased;
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
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
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
            if (_bottle.SO == null || _bottle.IsEmpty) { StopPouring(); return; }

            float tiltDeg = Vector3.Angle(_neck.up, Vector3.up);
            if (tiltDeg < _tiltThresholdDeg) { StopPouring(); return; }

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
                target.Receive(_bottle.SO.Ingredient.Id, volume);

            if (_stream != null)
                _stream.Show(_neck.position, streamEnd, _bottle.SO.Ingredient.LiquidColor);

            if (_pourLoopHandle == 0 && _pourSfx != SfxId.None
                && ServiceLocator.TryGet<IAudioService>(out var audio))
            {
                _pourLoopHandle = audio.StartLoop(_pourSfx, _neck, _neck.position);
            }

            Poured?.Invoke(volume);
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
