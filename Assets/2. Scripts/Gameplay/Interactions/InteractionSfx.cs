using Data.Enums;
using Services;
using Services.Audio;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Natural one-shots for handling a grabbable object: grab, release, and setting it down (a soft
    /// collision below the break threshold). Stack-agnostic — grab/release ride on <see cref="GrabBridge"/>
    /// events (so they work with any VR grab backend), while the set-down clink rides on physics
    /// collisions. Loudness/pitch/retrigger all come from the SfxDatabase, so this component only decides
    /// WHEN to play, never how loud. Lives on the Glass/Bottle prefabs.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class InteractionSfx : MonoBehaviour
    {
        [Header("Grab / Release (optional — needs a GrabBridge)")]
        [SerializeField] private SfxId _grabSfx = SfxId.GrabObject;
        [SerializeField] private SfxId _releaseSfx = SfxId.ReleaseObject;

        [Header("Set-down / clink (collision)")]
        [SerializeField] private SfxId _placeSfx = SfxId.GlassPlace;
        [Tooltip("Min collision impulse to play the place/clink — ignores micro-jitter while the object rests.")]
        [SerializeField] private float _minImpulse = 0.35f;
        [Tooltip("Max collision impulse for the place sound. Above this it's a hard hit (Breakable handles " +
                 "the smash), so we stay silent to avoid clink+shatter stacking.")]
        [SerializeField] private float _maxImpulse = 2.5f;
        [Range(0f, 1f)]
        [Tooltip("Volume scale applied to the place/clink one-shot (grab/release play at the clip's base volume).")]
        [SerializeField] private float _placeVolume = 0.9f;

        private GrabBridge _grab;
        private IAudioService _audio;

        void Awake() => _grab = GetComponent<GrabBridge>();

        void OnEnable()
        {
            if (_grab == null) return;
            _grab.Grabbed += HandleGrabbed;
            _grab.Released += HandleReleased;
        }

        void OnDisable()
        {
            if (_grab == null) return;
            _grab.Grabbed -= HandleGrabbed;
            _grab.Released -= HandleReleased;
        }

        private void HandleGrabbed() => Play(_grabSfx, transform.position);
        private void HandleReleased() => Play(_releaseSfx, transform.position);

        void OnCollisionEnter(Collision col)
        {
            if (_placeSfx == SfxId.None) return;
            float impulse = col.impulse.magnitude;
            if (impulse < _minImpulse || impulse >= _maxImpulse) return;
            Vector3 at = col.contactCount > 0 ? col.GetContact(0).point : transform.position;
            Play(_placeSfx, at, _placeVolume);
        }

        private void Play(SfxId id, Vector3 at, float volumeScale = 1f)
        {
            if (id == SfxId.None) return;
            // Resolve the audio service lazily and cache it; grabs/collisions are not a hot path.
            if (_audio == null && !ServiceLocator.TryGet(out _audio)) return;
            _audio.PlayOneShot(id, at, volumeScale);
        }
    }
}
