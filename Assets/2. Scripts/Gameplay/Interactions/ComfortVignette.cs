using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gameplay.Interactions
{
    /// <summary>
    /// VR comfort tunnelling: closes a URP Vignette in proportion to artificial-locomotion
    /// speed (the OVRCameraRig moving via thumbstick), which strongly reduces motion sickness
    /// from vection. Real-room walking doesn't move the rig origin, so it doesn't trigger.
    /// Put this on the Global Volume (with a Vignette override) and it auto-finds the rig.
    /// </summary>
    public sealed class ComfortVignette : MonoBehaviour
    {
        [SerializeField] private Volume _volume;
        [Tooltip("Rig speed (m/s) that maps to the maximum vignette.")]
        [SerializeField] private float _speedForMax = 1.0f;
        [Tooltip("Vignette intensity at max speed (0..1).")]
        [SerializeField] private float _maxIntensity = 0.45f;
        [SerializeField] private float _smoothing = 10f;

        private Vignette _vignette;
        private Transform _rig;
        private Vector3 _lastPos;

        void Start()
        {
            if (_volume == null) _volume = GetComponent<Volume>();
            if (_volume == null) _volume = FindAnyObjectByType<Volume>();
            if (_volume != null && _volume.profile != null) _volume.profile.TryGet(out _vignette);

            var rig = FindAnyObjectByType<OVRCameraRig>();
            _rig = rig != null ? rig.transform : null;
            if (_rig != null) _lastPos = _rig.position;
        }

        void Update()
        {
            if (_vignette == null || _rig == null) return;

            Vector3 p = _rig.position;
            float dt = Mathf.Max(1e-5f, Time.deltaTime);
            float speed = (p - _lastPos).magnitude / dt;
            _lastPos = p;

            float target = Mathf.Clamp01(speed / Mathf.Max(0.01f, _speedForMax)) * _maxIntensity;
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, target, _smoothing * dt);
        }
    }
}
