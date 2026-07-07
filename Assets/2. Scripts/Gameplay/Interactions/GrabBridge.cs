using Services;
using Services.Haptics;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Stack-agnostic bridge between the VR grab system and gameplay components.
    /// Either XRI's XRGrabInteractable (via UnityEvents on selectEntered/Exited) or
    /// Meta Interaction SDK's Grabbable plug into SetHeld(true/false).
    /// This keeps Bottle/Glass logic independent from the specific VR package.
    /// </summary>
    public sealed class GrabBridge : MonoBehaviour
    {
        [SerializeField] private UnityEvent _onGrabbed;
        [SerializeField] private UnityEvent _onReleased;

        public bool IsHeld { get; private set; }

        /// <summary>Which hand currently holds this: 0 = left, 1 = right, -1 = none. Set by the grab
        /// system so gameplay (e.g. pour haptics) can target the holding controller without this
        /// stack-agnostic bridge depending on any specific VR SDK type.</summary>
        public int HeldByHand { get; private set; } = -1;

        public void SetHeldBy(int hand) => HeldByHand = hand;

        public event System.Action Grabbed;
        public event System.Action Released;

        public void SetHeld(bool held)
        {
            if (IsHeld == held) return;
            IsHeld = held;
            if (!held) HeldByHand = -1;
            if (held)
            {
                _onGrabbed?.Invoke();
                Grabbed?.Invoke();
                if (ServiceLocator.TryGet<IHapticService>(out var hap)) hap.PulseBoth(0.4f, 0.06f);
            }
            else
            {
                _onReleased?.Invoke();
                Released?.Invoke();
                if (ServiceLocator.TryGet<IHapticService>(out var hap)) hap.PulseBoth(0.25f, 0.04f);
            }
        }

        public void OnGrab() => SetHeld(true);
        public void OnRelease() => SetHeld(false);

#if UNITY_EDITOR
        // Debug-only: outlines this grabbable's colliders in the Scene view so grab zones are easy
        // to spot without hunting for the SDK interactable's own gizmo. Green while held, blue when free.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsHeld ? new Color(0.3f, 1f, 0.4f, 0.6f) : new Color(0.3f, 0.7f, 1f, 0.4f);
            foreach (var col in GetComponentsInChildren<Collider>())
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
#endif
    }
}
