using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Quest thumbstick locomotion with two comfort modes (left stick):
    ///  - Smooth: glide where you look. Pair with ComfortVignette to cut motion sickness.
    ///  - Teleport: push the stick forward to aim from the left controller, release to jump
    ///    to the marked floor spot — zero vection, the most comfortable option.
    /// Right stick = snap turn (comfortable) by default. Put this on the OVRCameraRig.
    /// </summary>
    public sealed class ThumbstickLocomotion : MonoBehaviour
    {
        public enum Mode { Smooth, Teleport }

        [Header("Mode")]
        [SerializeField] private Mode _mode = Mode.Smooth;

        [Header("Move (left stick)")]
        [Tooltip("Smooth-move speed (m/s). Lower = more comfortable.")]
        [SerializeField] private float _speed = 1.2f;
        [SerializeField] private OVRInput.Controller _moveController = OVRInput.Controller.LTouch;
        [SerializeField] private float _moveDeadzone = 0.15f;

        [Header("Teleport")]
        [SerializeField] private float _teleportRange = 6f;
        [Tooltip("Forward stick push needed to start aiming a teleport.")]
        [SerializeField] private float _teleportAimThreshold = 0.6f;

        [Header("Turn (right stick)")]
        [SerializeField] private OVRInput.Controller _turnController = OVRInput.Controller.RTouch;
        [Tooltip("Snap turn (true) rotates in fixed steps per flick — comfortable. Smooth turn (false) is nauseating.")]
        [SerializeField] private bool _snapTurn = true;
        [SerializeField] private float _snapAngle = 30f;
        [SerializeField] private float _smoothTurnSpeed = 120f;
        [SerializeField] private float _turnDeadzone = 0.6f;

        private Transform _head;
        private Transform _leftHand;
        private bool _snapArmed = true;

        // Teleport state
        private Transform _marker;
        private bool _aiming;
        private bool _hasTarget;
        private Vector3 _targetPoint;

        void Start()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                _head = rig.centerEyeAnchor;
                _leftHand = rig.leftHandAnchor;
            }
            else
            {
                MyLogger.LogWarning("[ThumbstickLocomotion] No OVRCameraRig found. Movement will use local forward.");
            }
        }

        void Update()
        {
            if (_mode == Mode.Smooth) HandleSmoothMove();
            else HandleTeleport();
            HandleTurn();
        }

        private void HandleSmoothMove()
        {
            var input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _moveController);
            if (input.sqrMagnitude < _moveDeadzone * _moveDeadzone) return;

            Vector3 forward = _head != null ? _head.forward : transform.forward;
            Vector3 right = _head != null ? _head.right : transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            transform.position += (forward * input.y + right * input.x) * (_speed * Time.deltaTime);
        }

        private void HandleTeleport()
        {
            float push = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _moveController).y;

            if (push > _teleportAimThreshold)
            {
                _aiming = true;
                Transform origin = _leftHand != null ? _leftHand : _head;
                if (origin != null &&
                    Physics.Raycast(origin.position, origin.forward, out var hit, _teleportRange) &&
                    hit.normal.y > 0.7f)
                {
                    _hasTarget = true;
                    _targetPoint = hit.point;
                    ShowMarker(hit.point);
                }
                else
                {
                    _hasTarget = false;
                    HideMarker();
                }
                return;
            }

            // Stick released: commit the teleport.
            if (_aiming && _hasTarget)
            {
                Vector3 headPos = _head != null ? _head.position : transform.position;
                Vector3 delta = _targetPoint - headPos;
                delta.y = 0f;
                transform.position += delta;
            }
            _aiming = false;
            _hasTarget = false;
            HideMarker();
        }

        private void HandleTurn()
        {
            float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _turnController).x;
            Vector3 pivot = _head != null ? _head.position : transform.position;

            if (_snapTurn)
            {
                if (Mathf.Abs(x) < _turnDeadzone) { _snapArmed = true; return; }
                if (!_snapArmed) return;
                transform.RotateAround(pivot, Vector3.up, Mathf.Sign(x) * _snapAngle);
                _snapArmed = false;
            }
            else
            {
                if (Mathf.Abs(x) < _turnDeadzone) return;
                transform.RotateAround(pivot, Vector3.up, x * _smoothTurnSpeed * Time.deltaTime);
            }
        }

        private void ShowMarker(Vector3 pos)
        {
            if (_marker == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "TeleportMarker";
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.material.color = new Color(0.3f, 0.8f, 1f, 1f);
                _marker = go.transform;
            }
            _marker.position = pos + Vector3.up * 0.02f;
            if (!_marker.gameObject.activeSelf) _marker.gameObject.SetActive(true);
        }

        private void HideMarker()
        {
            if (_marker != null && _marker.gameObject.activeSelf) _marker.gameObject.SetActive(false);
        }
    }
}
