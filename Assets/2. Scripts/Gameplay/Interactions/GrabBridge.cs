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
            }
            else
            {
                _onReleased?.Invoke();
                Released?.Invoke();
            }
        }

        public void OnGrab() => SetHeld(true);
        public void OnRelease() => SetHeld(false);
    }
}
