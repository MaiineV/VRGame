using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Feeds Meta Interaction SDK grab state into the stack-agnostic <see cref="GrabBridge"/>.
    /// Listens to the Grabbable's pointer events (Select/Unselect/Cancel) so PourDetector, pooling
    /// and the rest of gameplay keep reading GrabBridge.IsHeld exactly as they did with the old
    /// custom grabber. Counts concurrent selections so a two-hand grab releases only when the last
    /// hand lets go.
    /// </summary>
    [RequireComponent(typeof(GrabBridge))]
    public sealed class GrabBridgeAdapter : MonoBehaviour
    {
        [Tooltip("Optional. Auto-resolves to the Grabbable on this object or its children.")]
        [SerializeField] private Grabbable _grabbable;

        private GrabBridge _bridge;
        private int _selectCount;

        void Awake()
        {
            _bridge = GetComponent<GrabBridge>();
            if (_grabbable == null) _grabbable = GetComponentInChildren<Grabbable>(true);
        }

        void OnEnable()
        {
            if (_grabbable != null) _grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }

        void OnDisable()
        {
            if (_grabbable != null) _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
            // Pooled objects can be disabled mid-hold (e.g. glass recycled); don't leave the bridge held.
            _selectCount = 0;
            if (_bridge != null && _bridge.IsHeld) _bridge.SetHeld(false);
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    _selectCount++;
                    if (_selectCount == 1)
                    {
                        _bridge.SetHeldBy(ResolveHand(evt.Data));
                        _bridge.SetHeld(true);
                    }
                    break;

                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    if (_selectCount > 0 && --_selectCount == 0)
                        _bridge.SetHeld(false);
                    break;
            }
        }

        /// <summary>0 = left, 1 = right (GrabBridge convention). The event's Data is the interactor,
        /// which lives under the rig's per-hand Controller — walk up to it for handedness.</summary>
        private static int ResolveHand(object interactorData)
        {
            var component = interactorData as Component;
            if (component != null)
            {
                var controller = component.GetComponentInParent<Controller>();
                if (controller != null) return controller.Handedness == Handedness.Left ? 0 : 1;
                var hand = component.GetComponentInParent<Hand>();
                if (hand != null) return hand.Handedness == Handedness.Left ? 0 : 1;
            }
            return 1;
        }
    }
}
