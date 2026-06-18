using Services;
using Services.Progression;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Shows or hides a pre-placed bottle based on whether its ingredient is unlocked in the day shop.
    /// Locked bottles stay in the scene but are made invisible/non-interactive until the recipe that
    /// uses them is unlocked. The component keeps its own GameObject active (so it keeps listening to
    /// <see cref="IProgressionService.UnlocksChanged"/>) and toggles renderers/colliders instead —
    /// disabling the root GameObject would kill this component and it could never re-enable.
    ///
    /// Service resolution happens in Start() as well as OnEnable() because OnEnable can run before
    /// GameBootstrap.Awake has registered the services (init-order race) — Start() is guaranteed to
    /// run after every Awake, so the progression service is always available by then.
    /// </summary>
    [RequireComponent(typeof(Bottle))]
    public sealed class BottleUnlockGate : MonoBehaviour
    {
        [Tooltip("Optional child to toggle via SetActive instead of toggling this object's renderers/" +
                 "colliders. Leave null to gate the renderers/colliders on this bottle. Must NOT be this " +
                 "same GameObject (that would disable the gate itself).")]
        [SerializeField] private GameObject _target;

        private Bottle _bottle;
        private IProgressionService _progression;
        private bool _subscribed;

        void Awake() => _bottle = GetComponent<Bottle>();

        // OnEnable handles re-enables (service ready); Start handles first scene load, where OnEnable
        // may have run before the services were registered.
        void OnEnable() => Bind();
        void Start() => Bind();

        void OnDisable()
        {
            if (_subscribed && _progression != null) _progression.UnlocksChanged -= Apply;
            _subscribed = false;
            _progression = null;
        }

        private void Bind()
        {
            if (_progression == null) ServiceLocator.TryGet<IProgressionService>(out _progression);
            if (_progression != null && !_subscribed)
            {
                _progression.UnlocksChanged += Apply;
                _subscribed = true;
            }
            Apply();
        }

        private void Apply()
        {
            if (_bottle == null || _bottle.SO == null || _bottle.SO.Ingredient == null) return;

            // Until the progression service is available, leave the bottle as-is (visible). Start()
            // re-runs this once the service exists, so locked bottles get hidden before the first frame.
            bool unlocked = _progression == null || _progression.IsBottleUnlocked(_bottle.SO.Ingredient.Id);

            if (_target != null && _target != gameObject)
                _target.SetActive(unlocked);
            else
                SetVisible(unlocked);
        }

        private void SetVisible(bool visible)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = visible;

            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = visible;

            // Freeze a hidden bottle so the invisible rigidbody can't fall or be grabbed.
            if (_bottle != null && _bottle.Body != null) _bottle.Body.isKinematic = !visible;
        }
    }
}
