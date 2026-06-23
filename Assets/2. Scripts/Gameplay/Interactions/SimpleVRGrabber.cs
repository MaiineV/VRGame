using Services;
using Services.Haptics;
using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    public sealed class SimpleVRGrabber : MonoBehaviour
    {
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.RTouch;
        [SerializeField] private float _grabRadius = 0.12f;
        [SerializeField] private LayerMask _grabMask = ~0;

        [Tooltip("Hold (false): keep the trigger pressed to hold the object — release to drop. " +
                 "Toggle (true): one press grabs, the next press drops. Default hold keeps the " +
                 "current feel; toggle is gentler on the hand for long sessions.")]
        [SerializeField] private bool _toggleGrab;

        [Tooltip("Optional. If null, will auto-resolve to OVRCameraRig.rightHandAnchor / leftHandAnchor based on _controller.")]
        [SerializeField] private Transform _trackingSource;

        [SerializeField] private bool _debugLog;

        [Header("Throw")]
        [Tooltip("How many recent frames of hand motion to average for the release velocity. More = " +
                 "smoother but laggier; ~5 (about 0.1s) feels natural. Used as a fallback when the " +
                 "Oculus controller-velocity API reads ~0 (e.g. in the editor with no headset).")]
        [SerializeField] private int _velocitySamples = 5;

        [Tooltip("Multiplier on the released velocity. 1 = exact hand speed (1:1); >1 makes throws " +
                 "fly further with less effort. ~1.2 feels good without being floaty.")]
        [SerializeField] private float _throwVelocityScale = 1.2f;

        // Tracking space the Oculus controller velocities are expressed in (local to the rig).
        // Cached in Awake so Release can convert them to world space.
        private Transform _trackingSpace;

        private readonly System.Collections.Generic.List<Vector3> _linVels = new();
        private readonly System.Collections.Generic.List<Vector3> _angVels = new();
        private Vector3 _lastTrackPos;
        private Quaternion _lastTrackRot;
        private bool _hasLastSample;

        private GrabBridge _held;
        private Rigidbody _heldRb;
        private Transform _heldOriginalParent;
        private Vector3 _heldLocalPos;        // pose of the held object relative to the hand, captured at grab
        private Quaternion _heldLocalRot;
        private bool _heldWasKinematic;
        private Collider[] _heldColliders;
        private bool[] _heldOriginalTriggerStates;

        private readonly Collider[] _hits = new Collider[16];

        void Awake()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            // Cache the tracking space: the Oculus controller-velocity API reports in this space,
            // so Release converts those local velocities to world via this transform.
            if (rig != null) _trackingSpace = rig.trackingSpace != null ? rig.trackingSpace : rig.transform;

            if (_trackingSource != null) return;

            if (rig == null)
            {
                if (_debugLog) MyLogger.LogWarning($"[SimpleVRGrabber:{name}] No OVRCameraRig found and no _trackingSource assigned. Grabber will use its own transform.");
                return;
            }

            _trackingSource = _controller switch
            {
                OVRInput.Controller.RTouch => rig.rightHandAnchor,
                OVRInput.Controller.LTouch => rig.leftHandAnchor,
                _ => null
            };

            if (_debugLog)
            {
                if (_trackingSource != null)
                    MyLogger.LogInfo($"[SimpleVRGrabber:{name}] Auto-resolved _trackingSource to {_trackingSource.name}.");
                else
                    MyLogger.LogWarning($"[SimpleVRGrabber:{name}] Could not resolve a hand anchor for controller {_controller}.");
            }
        }

        void Update()
        {
            if (_toggleGrab)
            {
                bool toggled = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, _controller)
                               || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, _controller);

                if (_debugLog && toggled)
                    MyLogger.LogInfo($"[SimpleVRGrabber:{name}] toggle at {transform.position} (held={_held != null})");

                if (toggled)
                {
                    if (_held == null) TryGrab();
                    else Release();
                }
            }
            else
            {
                bool pressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, _controller)
                               || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _controller);

                if (_debugLog && pressed)
                    MyLogger.LogInfo($"[SimpleVRGrabber:{name}] pressed at {transform.position} (held={_held != null})");

                if (_held == null && pressed) TryGrab();
                else if (_held != null && !pressed) Release();
            }

            if (_held != null && _heldRb != null && _heldRb.isKinematic)
            {
                // Hold the object at the pose it had RELATIVE to the hand when grabbed, instead of
                // snapping its pivot onto the hand anchor each frame. Snapping the pivot made the object
                // jump to an offset (the glass "floated" away from the hand and never looked attached).
                _heldRb.MovePosition(transform.TransformPoint(_heldLocalPos));
                _heldRb.MoveRotation(transform.rotation * _heldLocalRot);
            }
        }

        void LateUpdate()
        {
            if (_trackingSource == null) return;
            transform.SetPositionAndRotation(_trackingSource.position, _trackingSource.rotation);
            SampleVelocity();
        }

        // Records the hand's linear/angular velocity each frame so Release can throw the held object
        // with the real motion of the hand (averaged over the last few frames for a stable result).
        private void SampleVelocity()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (_hasLastSample)
            {
                Vector3 linVel = (pos - _lastTrackPos) / dt;

                Quaternion delta = rot * Quaternion.Inverse(_lastTrackRot);
                delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
                if (angleDeg > 180f) angleDeg -= 360f;
                Vector3 angVel = axis.sqrMagnitude > 0.0001f
                    ? axis.normalized * (angleDeg * Mathf.Deg2Rad) / dt
                    : Vector3.zero;

                if (!float.IsNaN(linVel.x) && !float.IsNaN(angVel.x))
                {
                    Push(_linVels, linVel);
                    Push(_angVels, angVel);
                }
            }

            _lastTrackPos = pos;
            _lastTrackRot = rot;
            _hasLastSample = true;
        }

        private void Push(System.Collections.Generic.List<Vector3> buf, Vector3 v)
        {
            buf.Add(v);
            int max = Mathf.Max(1, _velocitySamples);
            while (buf.Count > max) buf.RemoveAt(0);
        }

        private static Vector3 Average(System.Collections.Generic.List<Vector3> buf)
        {
            if (buf.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < buf.Count; i++) sum += buf[i];
            return sum / buf.Count;
        }

        private void ResetVelocityTracking()
        {
            _linVels.Clear();
            _angVels.Clear();
            _hasLastSample = false;
        }

        // Release velocity for a thrown object. Prefers the Oculus controller-velocity API (reliable
        // on-device, reports in tracking space → converted to world), and falls back to the per-frame
        // position sampler when that reads ~0 (e.g. running in the editor without a headset).
        private void GetThrowVelocity(out Vector3 linear, out Vector3 angular)
        {
            linear = Vector3.zero;
            angular = Vector3.zero;

            Vector3 localLin = OVRInput.GetLocalControllerVelocity(_controller);
            Vector3 localAng = OVRInput.GetLocalControllerAngularVelocity(_controller);

            if (localLin.sqrMagnitude > 0.0001f)
            {
                linear = _trackingSpace != null ? _trackingSpace.TransformVector(localLin) : localLin;
                angular = _trackingSpace != null ? _trackingSpace.TransformDirection(localAng) : localAng;
                return;
            }

            // Fallback: differentiated hand-anchor motion (averaged over the last few frames).
            linear = Average(_linVels);
            angular = Average(_angVels);
        }

        void TryGrab()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, _grabRadius, _hits, _grabMask, QueryTriggerInteraction.Ignore);
            if (_debugLog) MyLogger.LogInfo($"[SimpleVRGrabber:{name}] OverlapSphere hits={n} at {transform.position} r={_grabRadius}");

            float bestDist = float.MaxValue;
            GrabBridge best = null;
            for (int i = 0; i < n; i++)
            {
                var b = _hits[i].GetComponentInParent<GrabBridge>();
                if (b == null || b.IsHeld) continue;
                // A grabbable can veto being picked up right now (e.g. a for-sale bottle the player
                // can't afford). Skip it so the grabber simply doesn't latch on.
                var gate = b.GetComponentInParent<IGrabGate>();
                if (gate != null && !gate.CanGrab) continue;
                float d = (b.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = b; }
            }
            if (best == null)
            {
                if (_debugLog && n > 0) MyLogger.LogInfo($"[SimpleVRGrabber:{name}] No GrabBridge among {n} hits.");
                return;
            }

            _held = best;
            ResetVelocityTracking();
            _heldRb = best.GetComponent<Rigidbody>();
            _heldOriginalParent = best.transform.parent;
            best.transform.SetParent(transform, true);
            // Remember where the object sat relative to the hand at the moment of grab, so Update can
            // hold it there (it stays attached where you grabbed it instead of snapping its pivot onto
            // the controller and appearing to float).
            _heldLocalPos = best.transform.localPosition;
            _heldLocalRot = best.transform.localRotation;
            if (_heldRb != null)
            {
                _heldWasKinematic = _heldRb.isKinematic;
                _heldRb.isKinematic = true;
                _heldRb.linearVelocity = Vector3.zero;
                _heldRb.angularVelocity = Vector3.zero;
            }
            _heldColliders = best.GetComponentsInChildren<Collider>();
            _heldOriginalTriggerStates = new bool[_heldColliders.Length];
            for (int i = 0; i < _heldColliders.Length; i++)
            {
                _heldOriginalTriggerStates[i] = _heldColliders[i].isTrigger;
                _heldColliders[i].isTrigger = true;
            }

            // Tag the holding hand so gameplay (e.g. pour) can buzz the correct controller.
            _held.SetHeldBy(_controller == OVRInput.Controller.LTouch ? 0 : 1);
            _held.SetHeld(true);

            if (ServiceLocator.TryGet<IHapticService>(out var hap))
                hap.Pulse(_controller, 0.4f, 0.06f);
        }

        void Release()
        {
            if (_held == null) return;

            if (ServiceLocator.TryGet<IHapticService>(out var hap))
                hap.Pulse(_controller, 0.25f, 0.04f);

            _held.transform.SetParent(_heldOriginalParent, true);

            if (_heldColliders != null)
            {
                for (int i = 0; i < _heldColliders.Length; i++)
                {
                    if (_heldColliders[i] != null)
                        _heldColliders[i].isTrigger = _heldOriginalTriggerStates[i];
                }
                _heldColliders = null;
                _heldOriginalTriggerStates = null;
            }

            if (_heldRb != null)
            {
                _heldRb.isKinematic = _heldWasKinematic;
                // Throw: hand off the hand's velocity so a flick launches the object. Released without
                // moving → ~zero velocity, so it just settles.
                if (!_heldRb.isKinematic)
                {
                    GetThrowVelocity(out Vector3 lin, out Vector3 ang);
                    _heldRb.linearVelocity = lin * _throwVelocityScale;
                    _heldRb.angularVelocity = ang;
                }
            }
            _held.SetHeld(false);
            _held = null;
            _heldRb = null;
            _heldOriginalParent = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _grabRadius);
        }
    }
}
