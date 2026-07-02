using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Applies this object's <see cref="IGrabGate"/> (e.g. BottleUnlockGate on unaffordable for-sale
    /// bottles) to the Meta Interaction SDK interactables. The old SimpleVRGrabber consulted the gate
    /// itself; SDK interactors don't know about it, so this enforcer enables/disables the interactable
    /// components to veto grabbing. Cheap enough to poll: a handful of bottles, one bool each.
    /// </summary>
    public sealed class GrabGateEnforcer : MonoBehaviour
    {
        private IGrabGate _gate;
        private GrabInteractable[] _grab;
        private HandGrabInteractable[] _handGrab;
        private DistanceGrabInteractable[] _distanceGrab;
        private DistanceHandGrabInteractable[] _distanceHandGrab;
        private bool _lastAllowed = true;

        void Awake()
        {
            _gate = GetComponentInParent<IGrabGate>();
            _grab = GetComponentsInChildren<GrabInteractable>(true);
            _handGrab = GetComponentsInChildren<HandGrabInteractable>(true);
            _distanceGrab = GetComponentsInChildren<DistanceGrabInteractable>(true);
            _distanceHandGrab = GetComponentsInChildren<DistanceHandGrabInteractable>(true);
        }

        void Update()
        {
            if (_gate == null) return;
            bool allowed = _gate.CanGrab;
            if (allowed == _lastAllowed) return;
            _lastAllowed = allowed;

            foreach (var i in _grab) i.enabled = allowed;
            foreach (var i in _handGrab) i.enabled = allowed;
            foreach (var i in _distanceGrab) i.enabled = allowed;
            foreach (var i in _distanceHandGrab) i.enabled = allowed;
        }
    }
}
