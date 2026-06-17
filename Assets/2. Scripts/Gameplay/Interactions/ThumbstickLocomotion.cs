using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Quest thumbstick locomotion with two comfort modes (left stick):
    ///  - Smooth: glide where you look. Pair with ComfortVignette to cut motion sickness.
    ///  - Teleport: push the stick forward to aim a parabolic arc from the left controller,
    ///    release to jump to the marked floor spot — zero vection, the most comfortable option.
    /// While aiming, an arc line + a floor reticle are always visible (green = valid landing,
    /// red = no valid floor), so the destination is never a guess.
    /// Right stick X = snap turn (comfortable) by default; right stick Y (while holding the
    /// height modifier) raises/lowers the player. Put this on the OVRCameraRig.
    /// </summary>
    public sealed class ThumbstickLocomotion : MonoBehaviour
    {
        public enum Mode { Smooth, Teleport }

        [Header("Mode")]
        [Tooltip("Teleport is the most comfortable (zero vection) and is the default. Smooth move " +
                 "induces motion sickness — only use it with a working comfort vignette.")]
        [SerializeField] private Mode _mode = Mode.Teleport;

        [Header("Move (left stick)")]
        [Tooltip("Smooth-move speed (m/s). Lower = more comfortable.")]
        [SerializeField] private float _speed = 1.0f;
        [SerializeField] private OVRInput.Controller _moveController = OVRInput.Controller.LTouch;
        [SerializeField] private float _moveDeadzone = 0.15f;

        [Header("Teleport")]
        [SerializeField] private float _teleportRange = 6f;
        [Tooltip("Forward stick push needed to start aiming a teleport.")]
        [SerializeField] private float _teleportAimThreshold = 0.6f;
        [Tooltip("Initial speed of the aiming arc (m/s). Higher = flatter, longer reach.")]
        [SerializeField] private float _arcStrength = 8f;
        [Tooltip("How many segments the arc is sampled into. More = smoother line, slightly costlier.")]
        [SerializeField] private int _arcSegments = 30;
        [Tooltip("Seconds of simulated flight time the arc spans.")]
        [SerializeField] private float _arcDuration = 1.5f;

        [Header("Teleport visuals")]
        [Tooltip("Optional reticle prefab shown at the landing spot. If null, a flat disc is built " +
                 "procedurally so artists can swap in a nicer model without touching code.")]
        [SerializeField] private GameObject _reticlePrefab;
        [SerializeField] private Color _validColor = new Color(0.3f, 0.9f, 1f, 1f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.25f, 1f);
        [Tooltip("Width of the aiming arc line (m).")]
        [SerializeField] private float _arcWidth = 0.03f;

        [Header("Turn (right stick)")]
        [SerializeField] private OVRInput.Controller _turnController = OVRInput.Controller.RTouch;
        [Tooltip("Snap turn (true) rotates in fixed steps per flick — comfortable. Smooth turn (false) is nauseating.")]
        [SerializeField] private bool _snapTurn = true;
        [SerializeField] private float _snapAngle = 30f;
        [SerializeField] private float _smoothTurnSpeed = 120f;
        [SerializeField] private float _turnDeadzone = 0.6f;

        [Header("Height")]
        [Tooltip("Vertical offset applied on start so a seated/short player still stands at a comfortable " +
                 "bar height. Tweak live with the height modifier + right stick Y.")]
        [SerializeField] private float _standingHeightOffset = 0.4f;
        [Tooltip("Hold this button + push right stick Y to raise/lower the view at runtime.")]
        [SerializeField] private OVRInput.Button _heightModifier = OVRInput.Button.Three; // X on left Touch
        [SerializeField] private float _heightAdjustSpeed = 0.6f;
        [SerializeField] private float _minHeightOffset = -0.5f;
        [SerializeField] private float _maxHeightOffset = 1.2f;

        private Transform _head;
        private Transform _leftHand;
        private bool _snapArmed = true;
        private float _heightOffset;

        // Teleport state
        private Transform _reticle;
        private LineRenderer _arc;
        private Vector3[] _arcPoints;
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

            ApplyHeightOffset(_standingHeightOffset);
        }

        void Update()
        {
            if (_mode == Mode.Smooth) HandleSmoothMove();
            else HandleTeleport();
            HandleTurn();
            HandleHeight();
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
                _hasTarget = ComputeArc(out _targetPoint);
                ShowAim(_hasTarget);
                return;
            }

            // Stick released: commit the teleport if we had a valid landing spot.
            if (_aiming && _hasTarget)
            {
                Vector3 headPos = _head != null ? _head.position : transform.position;
                Vector3 delta = _targetPoint - headPos;
                delta.y = 0f;
                transform.position += delta;
            }
            _aiming = false;
            _hasTarget = false;
            HideAim();
        }

        /// <summary>
        /// Samples a parabolic arc from the left controller and finds the first floor hit.
        /// Fills the arc point buffer for the line renderer either way (so the curve is shown
        /// even when there's no valid landing). Returns true when a flat-enough floor was hit.
        /// </summary>
        private bool ComputeArc(out Vector3 hitPoint)
        {
            EnsureArcBuffer();

            Transform origin = _leftHand != null ? _leftHand : _head;
            if (origin == null)
            {
                hitPoint = Vector3.zero;
                return false;
            }

            Vector3 pos = origin.position;
            Vector3 vel = origin.forward * _arcStrength;
            float dt = _arcDuration / _arcSegments;
            Vector3 gravity = Physics.gravity;

            float traveled = 0f;
            hitPoint = pos;
            bool found = false;
            int written = 0;

            _arcPoints[written++] = pos;
            for (int i = 1; i <= _arcSegments; i++)
            {
                Vector3 next = pos + vel * dt + 0.5f * gravity * (dt * dt);
                Vector3 step = next - pos;
                float stepLen = step.magnitude;

                // Stop the arc once we've drawn out to the configured range.
                if (traveled + stepLen > _teleportRange) stepLen = _teleportRange - traveled;

                if (stepLen > 0.0001f &&
                    Physics.Raycast(pos, step.normalized, out var hit, stepLen, ~0, QueryTriggerInteraction.Ignore))
                {
                    _arcPoints[written++] = hit.point;
                    hitPoint = hit.point;
                    found = hit.normal.y > 0.7f; // flat enough to stand on
                    break;
                }

                vel += gravity * dt;
                pos = next;
                traveled += stepLen;
                _arcPoints[written++] = pos;

                if (traveled >= _teleportRange) break;
            }

            _arc.positionCount = written;
            for (int i = 0; i < written; i++) _arc.SetPosition(i, _arcPoints[i]);
            return found;
        }

        private void HandleTurn()
        {
            // Right stick Y is reserved for height adjust while the modifier is held — don't turn then.
            if (OVRInput.Get(_heightModifier)) return;

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

        private void HandleHeight()
        {
            if (!OVRInput.Get(_heightModifier)) return;
            float y = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _turnController).y;
            if (Mathf.Abs(y) < _turnDeadzone) return;

            float target = Mathf.Clamp(_heightOffset + y * _heightAdjustSpeed * Time.deltaTime,
                                       _minHeightOffset, _maxHeightOffset);
            ApplyHeightOffset(target);
        }

        private void ApplyHeightOffset(float newOffset)
        {
            newOffset = Mathf.Clamp(newOffset, _minHeightOffset, _maxHeightOffset);
            float delta = newOffset - _heightOffset;
            if (Mathf.Approximately(delta, 0f)) { _heightOffset = newOffset; return; }
            var p = transform.position;
            p.y += delta;
            transform.position = p;
            _heightOffset = newOffset;
        }

        // --- Aim visuals -------------------------------------------------------

        private void EnsureArcBuffer()
        {
            if (_arc == null)
            {
                var go = new GameObject("TeleportArc");
                go.transform.SetParent(transform, false);
                _arc = go.AddComponent<LineRenderer>();
                _arc.useWorldSpace = true;
                _arc.widthMultiplier = _arcWidth;
                _arc.numCapVertices = 4;
                _arc.material = new Material(Shader.Find("Sprites/Default"));
                _arc.textureMode = LineTextureMode.Stretch;
                _arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _arc.receiveShadows = false;
            }
            // +2 covers the origin point plus a possible hit point past the last sample.
            if (_arcPoints == null || _arcPoints.Length < _arcSegments + 2)
                _arcPoints = new Vector3[_arcSegments + 2];
        }

        private void ShowAim(bool valid)
        {
            EnsureReticle();
            Color c = valid ? _validColor : _invalidColor;

            _arc.startColor = _arc.endColor = c;
            if (!_arc.gameObject.activeSelf) _arc.gameObject.SetActive(true);

            // Reticle only makes sense at a real landing spot.
            _reticle.gameObject.SetActive(valid);
            if (valid)
            {
                _reticle.position = _targetPoint + Vector3.up * 0.02f;
                TintReticle(c);
            }
        }

        private void HideAim()
        {
            if (_arc != null && _arc.gameObject.activeSelf) _arc.gameObject.SetActive(false);
            if (_reticle != null && _reticle.gameObject.activeSelf) _reticle.gameObject.SetActive(false);
        }

        private void EnsureReticle()
        {
            if (_reticle != null) return;

            if (_reticlePrefab != null)
            {
                _reticle = Instantiate(_reticlePrefab).transform;
                _reticle.name = "TeleportReticle";
            }
            else
            {
                // Procedural fallback: a thin flat disc. Swappable via _reticlePrefab.
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "TeleportReticle";
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.localScale = new Vector3(0.4f, 0.01f, 0.4f);
                _reticle = go.transform;
            }
        }

        private void TintReticle(Color c)
        {
            var rend = _reticle.GetComponentInChildren<Renderer>();
            if (rend != null) rend.material.color = c;
        }
    }
}
