using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    public sealed class SimpleVRGrabber : MonoBehaviour
    {
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.RTouch;
        [SerializeField] private float _grabRadius = 0.12f;
        [SerializeField] private LayerMask _grabMask = ~0;

        [Tooltip("Optional. If null, will auto-resolve to OVRCameraRig.rightHandAnchor / leftHandAnchor based on _controller.")]
        [SerializeField] private Transform _trackingSource;

        [SerializeField] private bool _debugLog;

        private GrabBridge _held;
        private Rigidbody _heldRb;
        private Transform _heldOriginalParent;
        private bool _heldWasKinematic;
        private Collider[] _heldColliders;
        private bool[] _heldOriginalTriggerStates;

        private readonly Collider[] _hits = new Collider[16];

        void Awake()
        {
            if (_trackingSource != null) return;

            var rig = FindAnyObjectByType<OVRCameraRig>();
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
            bool pressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, _controller)
                           || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _controller);

            if (_debugLog && pressed)
                MyLogger.LogInfo($"[SimpleVRGrabber:{name}] pressed at {transform.position} (held={_held != null})");

            if (_held == null && pressed) TryGrab();
            else if (_held != null && !pressed) Release();

            if (_held != null && _heldRb != null && _heldRb.isKinematic)
            {
                _heldRb.MovePosition(transform.position);
                _heldRb.MoveRotation(transform.rotation);
            }
        }

        void LateUpdate()
        {
            if (_trackingSource == null) return;
            transform.SetPositionAndRotation(_trackingSource.position, _trackingSource.rotation);
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
                float d = (b.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = b; }
            }
            if (best == null)
            {
                if (_debugLog && n > 0) MyLogger.LogInfo($"[SimpleVRGrabber:{name}] No GrabBridge among {n} hits.");
                return;
            }

            _held = best;
            _heldRb = best.GetComponent<Rigidbody>();
            _heldOriginalParent = best.transform.parent;
            best.transform.SetParent(transform, true);
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

            _held.SetHeld(true);
        }

        void Release()
        {
            if (_held == null) return;
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
