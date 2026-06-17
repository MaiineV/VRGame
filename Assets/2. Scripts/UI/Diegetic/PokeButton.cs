using Data.Enums;
using Services;
using Services.Audio;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Diegetic
{
    /// <summary>
    /// Physical press-button for VR. A finger (or any collider on the configured layer)
    /// entering the trigger zone fires Pressed. Optional visual depression: the cap transform
    /// lerps down while held, back up on release. Debounced to avoid repeat fires.
    ///
    /// Requires this GameObject to have a trigger Collider. The finger collider must be on a
    /// layer included in _pressLayers (and should have a Rigidbody, kinematic is fine).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PokeButton : MonoBehaviour, IUpdateListener
    {
        [Header("Detection")]
        [SerializeField] private LayerMask _pressLayers = ~0;
        [Tooltip("Seconds during which additional presses are ignored after firing.")]
        [SerializeField] private float _debounceSeconds = 0.25f;
        [SerializeField] private bool _interactable = true;

        [Header("Visual (optional)")]
        [SerializeField] private Transform _cap;
        [SerializeField] private float _pressDepth = 0.005f;
        [SerializeField] private float _animSpeed = 20f;

        [Header("Audio")]
        [SerializeField] private SfxId _pressSfx = SfxId.ButtonPress;

        [Header("Events")]
        [SerializeField] private UnityEvent _onPressed;

        public event System.Action Pressed;
        public bool Interactable { get => _interactable; set => _interactable = value; }

        private IUpdateService _updates;
        private bool _ticking;
        private float _cooldown;
        private int _contactCount;
        private Vector3 _capRestLocal;
        private bool _capCached;

        void OnEnable()
        {
            ServiceLocator.TryGet<IUpdateService>(out _updates);
            if (_cap != null && !_capCached) { _capRestLocal = _cap.localPosition; _capCached = true; }
        }

        void OnDisable()
        {
            StopTicking();
            _contactCount = 0;
            _updates = null;
            if (_cap != null && _capCached) _cap.localPosition = _capRestLocal;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPressLayer(other.gameObject.layer)) return;
            _contactCount++;
            StartTicking();
            if (!_interactable || _cooldown > 0f) return;
            _cooldown = _debounceSeconds;
            if (_pressSfx != SfxId.None && ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.PlayOneShot(_pressSfx, transform.position);
            _onPressed?.Invoke();
            Pressed?.Invoke();
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPressLayer(other.gameObject.layer)) return;
            if (_contactCount > 0) _contactCount--;
        }

        public void MyUpdate()
        {
            float dt = Time.deltaTime;
            if (_cooldown > 0f) _cooldown -= dt;

            if (_cap != null && _capCached)
            {
                var target = _capRestLocal + (_contactCount > 0 ? Vector3.down * _pressDepth : Vector3.zero);
                _cap.localPosition = Vector3.Lerp(_cap.localPosition, target, _animSpeed * dt);
            }

            if (_cooldown <= 0f && _contactCount == 0 &&
                (_cap == null || !_capCached ||
                 (_cap.localPosition - _capRestLocal).sqrMagnitude < 0.0000001f))
            {
                StopTicking();
            }
        }

        private bool IsPressLayer(int layer) => (_pressLayers.value & (1 << layer)) != 0;

        private void StartTicking()
        {
            if (_ticking || _updates == null) return;
            _updates.AddUpdateListener(this);
            _ticking = true;
        }

        private void StopTicking()
        {
            if (!_ticking || _updates == null) return;
            _updates.RemoveUpdateListener(this);
            _ticking = false;
        }
    }
}
