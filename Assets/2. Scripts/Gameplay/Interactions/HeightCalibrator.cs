using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Auto height calibration extracted from the retired ThumbstickLocomotion (whose locomotion is
    /// now the Meta Interaction SDK's). Lifts the rig's TrackingSpace so the player's eyes land at
    /// TargetEyeHeight whether seated or standing. Offsetting TrackingSpace (not the rig root) keeps
    /// the calibration composable with the SDK's PlayerLocomotor, which moves/teleports the rig root.
    /// Recenter is on Start (left menu button ≡) — Y is reserved for exit-to-menu (GameControls).
    /// </summary>
    public sealed class HeightCalibrator : MonoBehaviour
    {
        [Tooltip("Eye height (m) the bar was designed around.")]
        [SerializeField] private float _targetEyeHeight = 1.6f;
        [Tooltip("Press to re-run height calibration at runtime. Start = left menu button (≡).")]
        [SerializeField] private OVRInput.Button _recenterButton = OVRInput.Button.Start;
        [Tooltip("Minimum plausible head world-Y before calibration runs (guards the frame-0 origin pose).")]
        [SerializeField] private float _minValidHeadHeight = 0.6f;
        [Tooltip("Keep re-calibrating this long after tracking becomes valid, so the settled pose wins.")]
        [SerializeField] private float _calibrationSettleTime = 0.6f;
        [SerializeField] private float _minOffset = -0.5f;
        [SerializeField] private float _maxOffset = 1.5f;
        [SerializeField] private bool _debugLog;

        private Transform _head;
        private Transform _trackingSpace;
        private float _offset;
        private bool _calibrated;
        private float _calibTimer;

        void Start()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig == null)
            {
                MyLogger.LogWarning("[HeightCalibrator] No OVRCameraRig found — disabled.");
                enabled = false;
                return;
            }
            _head = rig.centerEyeAnchor;
            _trackingSpace = rig.trackingSpace != null ? rig.trackingSpace : rig.transform;
        }

        void Update()
        {
            if (OVRInput.GetDown(_recenterButton)) { _calibrated = false; _calibTimer = 0f; }
            if (_calibrated) return;

            if (_head == null || _head.position.y < _minValidHeadHeight) { _calibTimer = 0f; return; }

            float delta = _targetEyeHeight - _head.position.y;
            ApplyOffset(_offset + delta);

            _calibTimer += Time.deltaTime;
            if (_calibTimer >= _calibrationSettleTime)
            {
                _calibrated = true;
                if (_debugLog)
                    MyLogger.LogInfo($"[HeightCalibrator] Locked eye height at {_head.position.y:F2}m (offset {_offset:F2}m).");
            }
        }

        private void ApplyOffset(float newOffset)
        {
            newOffset = Mathf.Clamp(newOffset, _minOffset, _maxOffset);
            float delta = newOffset - _offset;
            if (Mathf.Approximately(delta, 0f)) { _offset = newOffset; return; }
            var p = _trackingSpace.localPosition;
            p.y += delta;
            _trackingSpace.localPosition = p;
            _offset = newOffset;
        }
    }
}
