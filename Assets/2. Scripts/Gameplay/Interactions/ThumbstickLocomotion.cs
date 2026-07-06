using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Quest thumbstick locomotion with two comfort modes (left stick):
    ///  - Smooth: glide where you look. Pair with ComfortVignette to cut motion sickness.
    ///  - Teleport: push the stick forward to aim, release to jump to the marked floor spot —
    ///    zero vection, the most comfortable option. The aim/arc/reticle/commit itself is owned
    ///    entirely by the Meta Interaction SDK's Teleport Building Block (NavMesh variant,
    ///    TeleportInteractor + TeleportInteractable + native ReticleDataTeleport) installed on
    ///    this same rig — this component only gates HandleSmoothMove() off while in Teleport mode
    ///    so it doesn't fight the SDK's aim gesture on the same stick.
    /// Right stick X = snap turn (comfortable) by default; right stick Y (while holding the
    /// height modifier) raises/lowers the player. Put this on the OVRCameraRig.
    /// </summary>
    public sealed class ThumbstickLocomotion : MonoBehaviour
    {
        public enum Mode { Smooth, Teleport }

        [Header("Mode")]
        [Tooltip("Teleport is the most comfortable (zero vection) and is the default. Smooth move " +
                 "induces motion sickness — only use it with a working comfort vignette. In Teleport " +
                 "mode, aim/commit is handled entirely by the SDK's installed Teleport Building Block.")]
        [SerializeField] private Mode _mode = Mode.Teleport;

        [Header("Move (left stick)")]
        [Tooltip("Smooth-move speed (m/s). Lower = more comfortable.")]
        [SerializeField] private float _speed = 1.0f;
        [SerializeField] private OVRInput.Controller _moveController = OVRInput.Controller.LTouch;
        [SerializeField] private float _moveDeadzone = 0.15f;

        [Header("Turn (right stick)")]
        [SerializeField] private OVRInput.Controller _turnController = OVRInput.Controller.RTouch;
        [Tooltip("Snap turn (true) rotates in fixed steps per flick — comfortable. Smooth turn (false) is nauseating.")]
        [SerializeField] private bool _snapTurn = true;
        [SerializeField] private float _snapAngle = 30f;
        [SerializeField] private float _smoothTurnSpeed = 120f;
        [SerializeField] private float _turnDeadzone = 0.6f;

        [Header("Height")]
        [Tooltip("Auto-calibrate the view height on start so the player's eyes land at TargetEyeHeight " +
                 "whether they're seated or standing (no need to stand up). When off, falls back to the " +
                 "fixed StandingHeightOffset below.")]
        [SerializeField] private bool _autoCalibrateHeight = true;
        [Tooltip("Eye height (m) the bar was designed around. Calibration lifts/lowers the rig so the " +
                 "headset sits at this height regardless of the player's real seated/standing height.")]
        [SerializeField] private float _targetEyeHeight = 1.6f;
        [Tooltip("Press to re-run height calibration at runtime (e.g. after shifting in your chair). " +
                 "Y on the left Touch by default.")]
        [SerializeField] private OVRInput.Button _recenterButton = OVRInput.Button.Four; // Y on left Touch
        [Tooltip("Vertical offset applied on start when auto-calibrate is OFF, so a seated/short player " +
                 "still reaches a comfortable bar height. Tweak live with the height modifier + right stick Y.")]
        [SerializeField] private float _standingHeightOffset = 0.4f;
        [Tooltip("Hold this button + push right stick Y to raise/lower the view at runtime.")]
        [SerializeField] private OVRInput.Button _heightModifier = OVRInput.Button.Three; // X on left Touch
        [SerializeField] private float _heightAdjustSpeed = 0.6f;
        [SerializeField] private float _minHeightOffset = -0.5f;
        [SerializeField] private float _maxHeightOffset = 1.5f;
        [Tooltip("Minimum plausible head world-Y before calibration runs. Guards against calibrating while " +
                 "the headset pose is still settling at start (a transient low/origin reading).")]
        [SerializeField] private float _minValidHeadHeight = 0.6f;
        [Tooltip("After tracking becomes valid, keep re-calibrating for this long before locking in, so the " +
                 "final height uses the settled pose (not a noisy first frame). Avoids starting up too high.")]
        [SerializeField] private float _calibrationSettleTime = 0.6f;
        [Tooltip("Log measured head height and applied offset on each calibration (debugging only).")]
        [SerializeField] private bool _debugLog = false;

        private Transform _head;
        private bool _snapArmed = true;
        private float _heightOffset;
        private bool _calibrated;
        private float _calibTimer;

        void Start()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                _head = rig.centerEyeAnchor;
            }
            else
            {
                MyLogger.LogWarning("[ThumbstickLocomotion] No OVRCameraRig found. Movement will use local forward.");
            }

            // With auto-calibrate, wait for valid tracking and let Update() calibrate on the first good
            // frame (the headset pose is still at the origin on frame 0). Otherwise apply the fixed offset.
            if (_autoCalibrateHeight) _calibrated = false;
            else ApplyHeightOffset(_standingHeightOffset);
        }

        void Update()
        {
            // Teleport mode: aim/arc/commit is owned entirely by the SDK's installed Teleport
            // Building Block on this same rig — nothing to drive here.
            if (_mode == Mode.Smooth) HandleSmoothMove();
            HandleTurn();
            HandleHeight();
            HandleCalibration();
        }

        // Auto-calibrate once tracking is valid, and re-calibrate on demand via the recenter button.
        // We don't lock on the first valid frame: the headset pose is noisy right after launch and can
        // briefly read low, which would lock in too high an offset (player floats). Instead we keep
        // re-calibrating across a short settle window so the final, settled pose wins, then lock.
        private void HandleCalibration()
        {
            // Recenter re-arms a fresh settle pass (also covers manual recenter after auto-calibration).
            if (OVRInput.GetDown(_recenterButton)) { _calibrated = false; _calibTimer = 0f; }

            if (!_autoCalibrateHeight || _calibrated) return;

            // Wait for plausible tracking; reset the settle timer until then.
            if (_head == null || _head.position.y < _minValidHeadHeight) { _calibTimer = 0f; return; }

            Calibrate();                              // idempotent: snaps eyes to target, last write wins
            _calibTimer += Time.deltaTime;
            if (_calibTimer >= _calibrationSettleTime) _calibrated = true;
        }

        /// <summary>
        /// Lifts/lowers the rig so the headset (centerEyeAnchor) sits at <see cref="_targetEyeHeight"/>,
        /// regardless of whether the player is seated or standing. The measured eye Y already includes any
        /// offset applied so far, so we add the remaining delta on top of the current offset. Returns false
        /// (no-op) until the headset pose is plausible, to avoid calibrating against the frame-0 origin.
        /// </summary>
        private bool Calibrate()
        {
            if (_head == null || _head.position.y < _minValidHeadHeight) return false;

            float delta = _targetEyeHeight - _head.position.y;
            ApplyHeightOffset(_heightOffset + delta);

            if (_debugLog)
                MyLogger.LogInfo($"[ThumbstickLocomotion] Calibrated: measured eye Y={_head.position.y:F2}m, " +
                                 $"target={_targetEyeHeight:F2}m, offset now={_heightOffset:F2}m.");
            return true;
        }

        private void HandleSmoothMove()
        {
            var input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _moveController);
            if (input.sqrMagnitude < _moveDeadzone * _moveDeadzone) return;

            Vector3 forward = _head != null ? _head.forward : transform.forward;
            Vector3 right = _head != null ? _head.right : transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            Vector3 move = (forward * input.y + right * input.x) * (_speed * Time.deltaTime);
            transform.position += ClampMoveForObstacles(move);
        }

        // Block gliding through solid scene geometry (bar, shelves, walls, tables). Sweeps a body-sized
        // capsule at the headset's floor position along the move direction and stops short of any
        // non-trigger collider. Held objects are triggers, so they're ignored and never block the player.
        private Vector3 ClampMoveForObstacles(Vector3 move)
        {
            if (_head == null) return move;
            float dist = move.magnitude;
            if (dist < 1e-4f) return move;
            Vector3 dir = move / dist;

            const float radius = 0.22f;
            const float skin = 0.05f;
            Vector3 p0 = new Vector3(_head.position.x, transform.position.y + radius, _head.position.z);
            Vector3 p1 = new Vector3(_head.position.x, Mathf.Max(_head.position.y - radius, p0.y), _head.position.z);

            if (Physics.CapsuleCast(p0, p1, radius, dir, out var hit, dist + skin, ~0, QueryTriggerInteraction.Ignore))
                return dir * Mathf.Max(0f, hit.distance - skin);
            return move;
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
    }
}
