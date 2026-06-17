using Gameplay.Interactions;
using Services.UpdateService;
using Services;
using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Drives _WobbleVelocity on the liquid shader from the container's Rigidbody
    /// horizontal motion. On-demand LateUpdate: only ticks while the container is
    /// held AND the smoothed velocity is above an idle threshold.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LiquidWobble : MonoBehaviour, ILateUpdateListener
    {
        [SerializeField] private LiquidRenderer _renderer;
        [SerializeField] private GrabBridge _grab;

        [Tooltip("Lowpass smoothing for the horizontal velocity signal (higher = snappier).")]
        [SerializeField] private float _responsiveness = 8f;
        [Tooltip("Max horizontal speed mapped into the shader (clamp to keep tilt bounded).")]
        [SerializeField] private float _maxSpeed = 3f;
        [Tooltip("Below this smoothed speed for IdleFrames, the driver deregisters itself.")]
        [SerializeField] private float _idleSpeed = 0.02f;
        [SerializeField] private int _idleFrames = 30;

        private Rigidbody _body;
        private IUpdateService _updates;
        private Vector2 _smoothed;
        private int _idleCount;
        private bool _ticking;

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (_renderer == null) _renderer = GetComponentInChildren<LiquidRenderer>();
            if (_grab == null) _grab = GetComponent<GrabBridge>();
        }

        void Start()
        {
            _updates = ServiceLocator.Get<IUpdateService>();
            if (_grab != null)
            {
                _grab.Grabbed += StartTicking;
                _grab.Released += StopTicking;
                if (_grab.IsHeld) StartTicking();
            }
            else
            {
                StartTicking();
            }
        }

        void OnDestroy()
        {
            if (_grab != null)
            {
                _grab.Grabbed -= StartTicking;
                _grab.Released -= StopTicking;
            }
            StopTicking();
        }

        private void StartTicking()
        {
            if (_ticking || _updates == null) return;
            _ticking = true;
            _idleCount = 0;
            _updates.AddLateUpdateListener(this);
        }

        private void StopTicking()
        {
            if (!_ticking || _updates == null) return;
            _ticking = false;
            _updates.RemoveLateUpdateListener(this);
            _smoothed = Vector2.zero;
            if (_renderer != null) _renderer.SetWobbleVelocity(Vector2.zero);
        }

        public void MyLateUpdate()
        {
            var v = _body.linearVelocity;
            var target = new Vector2(v.x, v.z);

            float max = _maxSpeed;
            if (target.sqrMagnitude > max * max) target = target.normalized * max;

            float t = 1f - Mathf.Exp(-_responsiveness * Time.deltaTime);
            _smoothed = Vector2.Lerp(_smoothed, target, t);

            if (_renderer != null) _renderer.SetWobbleVelocity(_smoothed);

            if (_grab == null || !_grab.IsHeld)
            {
                if (_smoothed.sqrMagnitude < _idleSpeed * _idleSpeed)
                {
                    if (++_idleCount >= _idleFrames) StopTicking();
                }
                else _idleCount = 0;
            }
        }
    }
}
