using Oculus.Interaction;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// Drives a <see cref="PokeButton"/> from the Meta Interaction SDK poke path: when the SDK
    /// PokeInteractable under this button is selected (finger pressed through its surface), the
    /// button fires exactly as if a physical finger collider had entered its trigger. Keeps every
    /// PokeButton consumer (GlassDispenser, NightClipboard) untouched.
    /// </summary>
    [RequireComponent(typeof(PokeButton))]
    public sealed class PokeButtonSdkAdapter : MonoBehaviour
    {
        [Tooltip("Optional. Auto-resolves to the PokeInteractable in children (created by the SDK wizard).")]
        [SerializeField] private PokeInteractable _pokeInteractable;

        private PokeButton _button;

        void Awake()
        {
            _button = GetComponent<PokeButton>();
            if (_pokeInteractable == null) _pokeInteractable = GetComponentInChildren<PokeInteractable>(true);
        }

        void OnEnable()
        {
            if (_pokeInteractable != null) _pokeInteractable.WhenPointerEventRaised += HandlePointerEvent;
        }

        void OnDisable()
        {
            if (_pokeInteractable != null) _pokeInteractable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select) _button.TryPress();
        }
    }
}
