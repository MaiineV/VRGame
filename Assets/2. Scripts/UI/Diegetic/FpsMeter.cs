using Services;
using Services.UpdateService;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// World-space FPS readout that billboards to the main camera every frame.
    /// Exists to satisfy the grading requirement that the game must visibly run above
    /// 72 FPS on Meta Quest 3 (target refresh: 90 Hz). The label turns green when the
    /// smoothed frame-rate is at or above <see cref="_minAcceptable"/> and red otherwise,
    /// giving a clear at-a-glance pass/fail signal during playtests and grading sessions.
    /// </summary>
    public sealed class FpsMeter : MonoBehaviour, IUpdateListener
    {
        [Tooltip("TMP label that displays the current smoothed FPS. Assign in the Inspector.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("FPS threshold: label turns green at or above this value, red below it. " +
                 "72 matches the Quest 2 refresh floor; Quest 3 targets 90.")]
        [SerializeField] private float _minAcceptable = 72f;

        [Tooltip("Exponential smoothing factor (0..1). Lower = smoother but slower to react.")]
        [SerializeField] private float _smoothing = 0.1f;

        // ── runtime state ────────────────────────────────────────────────────────────

        private bool      _registered;
        private Transform _cam;
        private float     _smoothedFps;
        private bool      _wasGreen; // track last colour bucket to avoid per-frame churn

        // ── colours ──────────────────────────────────────────────────────────────────

        private static readonly Color ColorGreen = new Color(0.3f, 1f,    0.4f);
        private static readonly Color ColorRed   = new Color(1f,   0.35f, 0.3f);

        // ── lifecycle ────────────────────────────────────────────────────────────────

        void OnEnable()
        {
            // Fallback so a TMP on the same GameObject is picked up without manual Inspector wiring.
            if (_label == null) _label = GetComponent<TMP_Text>();
            _cam = Camera.main != null ? Camera.main.transform : null;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
            svc.AddUpdateListener(this);
            _registered = true;
        }

        void OnDisable()
        {
            if (_registered && ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _registered = false;
        }

        // ── IUpdateListener ──────────────────────────────────────────────────────────

        public void MyUpdate()
        {
            UpdateFps();
            UpdateLabel();
            Billboard();
        }

        // ── helpers ──────────────────────────────────────────────────────────────────

        private void UpdateFps()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            float instant = 1f / dt;
            // Exponential moving average — smooth out single-frame spikes.
            _smoothedFps = Mathf.Lerp(_smoothedFps, instant, _smoothing);
        }

        private void UpdateLabel()
        {
            if (_label == null) return;

            int fps = Mathf.RoundToInt(_smoothedFps);
            _label.text = $"{fps} FPS";

            bool isGreen = _smoothedFps >= _minAcceptable;
            if (isGreen != _wasGreen)
            {
                _label.color = isGreen ? ColorGreen : ColorRed;
                _wasGreen = isGreen;
            }
        }

        private void Billboard()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }
    }
}
