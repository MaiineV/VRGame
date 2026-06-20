using Services;
using Services.Economy;
using Services.GameState;
using Services.Progression;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Drives a pre-placed bottle's shop state and grab-to-buy purchase. Three states:
    ///   - Owned (ingredient unlocked): normal bottle — visible and grabbable.
    ///   - Locked + DayShop ("for sale"): visible with a price tag; grabbable only if the player can
    ///     afford it. Grabbing it buys it (charges + unlocks the bottle and its recipe permanently).
    ///   - Locked + not DayShop (night): hidden and non-interactive.
    ///
    /// Implements <see cref="IGrabGate"/> so <see cref="SimpleVRGrabber"/> won't pick up a for-sale
    /// bottle the player can't afford. Visibility/physics are left untouched while the bottle is held
    /// (the grabber owns it then). Keeps its own GameObject active so it keeps listening; toggles
    /// renderers/colliders (or an optional child) instead.
    /// </summary>
    [RequireComponent(typeof(Bottle))]
    public sealed class BottleUnlockGate : MonoBehaviour, IGrabGate
    {
        [Tooltip("Optional child to toggle via SetActive instead of toggling this object's renderers/" +
                 "colliders. Leave null to gate the renderers/colliders on this bottle. Must NOT be this " +
                 "same GameObject (that would disable the gate itself).")]
        [SerializeField] private GameObject _target;

        private Bottle _bottle;
        private GrabBridge _grab;
        private IProgressionService _progression;
        private IGameStateService _state;
        private IEconomyService _economy;
        private bool _subscribed;

        // Last visibility we applied. Lets Update() act only on an actual change (cheap), and makes the
        // poll a reliable safety net so the bottle always matches the desired state even if an event was
        // missed (service init-order races, a transition that didn't reach this gate, etc.).
        private bool? _lastVisible;

        void Awake()
        {
            _bottle = GetComponent<Bottle>();
            _grab = GetComponent<GrabBridge>();
        }

        void OnEnable() => Bind();
        void Start() => Bind();

        // Events drive most updates; this poll guarantees eventual consistency. It's a bool compare per
        // frame unless the desired visibility actually changed (then it toggles renderers once).
        void Update() => Apply();

        void OnDisable()
        {
            if (_subscribed)
            {
                if (_progression != null) _progression.UnlocksChanged -= Apply;
                if (_state != null) _state.StateChanged -= OnStateChanged;
                if (_grab != null) { _grab.Grabbed -= OnGrabbed; _grab.Released -= Apply; }
            }
            _subscribed = false;
            _progression = null;
            _state = null;
            _economy = null;
            _lastVisible = null;
        }

        // --- IGrabGate ---------------------------------------------------------------------------

        public bool CanGrab
        {
            get
            {
                var ing = IngredientId();
                if (ing == Data.Enums.IngredientId.None || _progression == null) return true;
                if (_progression.IsBottleUnlocked(ing)) return true;          // owned: grab freely
                if (_state == null || _state.Current != GameState.DayShop) return false; // locked outside shop
                int cost = Mathf.Max(0, _bottle.SO.UnlockCost);               // for sale: only if affordable
                return _economy != null && _economy.Cash >= cost;
            }
        }

        // --- binding -----------------------------------------------------------------------------

        private void Bind()
        {
            if (_progression == null) ServiceLocator.TryGet<IProgressionService>(out _progression);
            if (_state == null) ServiceLocator.TryGet<IGameStateService>(out _state);
            if (_economy == null) ServiceLocator.TryGet<IEconomyService>(out _economy);

            if (!_subscribed && _progression != null)
            {
                _progression.UnlocksChanged += Apply;
                if (_state != null) _state.StateChanged += OnStateChanged;
                if (_grab != null) { _grab.Grabbed += OnGrabbed; _grab.Released += Apply; }
                _subscribed = true;
            }
            Apply();
        }

        private void OnStateChanged(GameState from, GameState to) => Apply();

        // --- purchase on grab --------------------------------------------------------------------

        private void OnGrabbed()
        {
            var ing = IngredientId();
            if (ing == Data.Enums.IngredientId.None || _progression == null) return;
            if (_progression.IsBottleUnlocked(ing)) return;                  // already owned: normal grab
            if (_state == null || _state.Current != GameState.DayShop) return;
            // Affordability was already gated by CanGrab; UnlockBottle re-checks and charges + unlocks
            // the bottle and its recipe, then fires UnlocksChanged.
            _progression.UnlockBottle(ing);
        }

        // --- visibility --------------------------------------------------------------------------

        private void Apply()
        {
            if (_bottle == null || _bottle.SO == null || _bottle.SO.Ingredient == null) return;
            // While held, the grabber owns the transform/physics — don't fight it.
            if (_grab != null && _grab.IsHeld) return;
            // Wait until services resolve; until then leave the bottle as authored (don't force-hide).
            if (_progression == null || _state == null) return;

            bool vis = ShouldBeVisible();
            if (_lastVisible.HasValue && _lastVisible.Value == vis) return; // no change → skip the toggle
            _lastVisible = vis;

            if (_target != null && _target != gameObject)
                _target.SetActive(vis);
            else
                SetVisible(vis);
        }

        private bool ShouldBeVisible()
        {
            var ing = IngredientId();
            // Until services are available, leave the bottle as-is (visible).
            if (_progression == null || ing == Data.Enums.IngredientId.None) return true;
            if (_progression.IsBottleUnlocked(ing)) return true;             // owned: always
            return _state != null && _state.Current == GameState.DayShop;    // locked: only for sale in DayShop
        }

        private Data.Enums.IngredientId IngredientId() =>
            _bottle != null && _bottle.SO != null && _bottle.SO.Ingredient != null
                ? _bottle.SO.Ingredient.Id
                : Data.Enums.IngredientId.None;

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
