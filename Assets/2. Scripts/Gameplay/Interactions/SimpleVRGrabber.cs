using UnityEngine;

namespace Gameplay.Interactions
{
    public sealed class SimpleVRGrabber : MonoBehaviour
    {
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.RTouch;
        [SerializeField] private float _grabRadius = 0.08f;
        [SerializeField] private LayerMask _grabMask = ~0;

        private GrabBridge _held;
        private Rigidbody _heldRb;
        private Transform _heldOriginalParent;
        private bool _heldWasKinematic;

        private readonly Collider[] _hits = new Collider[16];

        void Update()
        {
            bool pressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, _controller)
                           || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _controller);

            if (_held == null && pressed) TryGrab();
            else if (_held != null && !pressed) Release();

            if (_held != null && _heldRb != null && _heldRb.isKinematic)
            {
                _heldRb.MovePosition(transform.position);
                _heldRb.MoveRotation(transform.rotation);
            }
        }

        void TryGrab()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, _grabRadius, _hits, _grabMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            GrabBridge best = null;
            for (int i = 0; i < n; i++)
            {
                var b = _hits[i].GetComponentInParent<GrabBridge>();
                if (b == null || b.IsHeld) continue;
                float d = (b.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = b; }
            }
            if (best == null) return;

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
            _held.SetHeld(true);
        }

        void Release()
        {
            if (_held == null) return;
            _held.transform.SetParent(_heldOriginalParent, true);
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
