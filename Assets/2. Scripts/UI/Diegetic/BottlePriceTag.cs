using Data.SO;
using Services;
using Services.GameState;
using Services.Progression;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// World-space price tag for a purchasable bottle. Shows "$&lt;UnlockCost&gt;" only while the bottle
    /// is still locked AND the game is in <see cref="GameState.DayShop"/> (i.e. actually buyable right
    /// now). Hides once the bottle is owned, or during the night. Must live on a SEPARATE GameObject (a
    /// sibling of the bottle, not a child) so the <see cref="Gameplay.Interactions.BottleUnlockGate"/>
    /// renderer toggling doesn't also disable it.
    ///
    /// Visibility is driven by toggling the label's renderer (not this GameObject's active state) so the
    /// component keeps listening and can re-show. Service resolution happens in both OnEnable and Start
    /// to survive the GameBootstrap init-order race.
    /// </summary>
    public sealed class BottlePriceTag : MonoBehaviour
    {
        [Tooltip("The bottle this tag represents. Used to read the ingredient (lock state) and UnlockCost.")]
        [SerializeField] private BottleSO _bottle;
        [Tooltip("Label to write the price into. Defaults to a TMP_Text on this object or its children.")]
        [SerializeField] private TMP_Text _label;
        [Tooltip("If true, the tag orients ONCE toward the player (yaw only, kept upright) and then stays " +
                 "static. The old per-frame billboard tilted with the head and read as 'broken'.")]
        [SerializeField] private bool _faceCamera = true;
        [Tooltip("If true, the tag snaps above the matching bottle each frame so it stays aligned even " +
                 "if the bottle is moved, duplicated, or respawned. Off keeps the tag at its fixed spot.")]
        [SerializeField] private bool _followBottle = true;
        [Tooltip("Offset above the bottle's origin (world units) when following.")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 0.28f, 0f);

        private IProgressionService _progression;
        private IGameStateService _state;
        private bool _subscribed;
        private Gameplay.Interactions.Bottle _bottleInstance;
        private bool? _lastShow;
        // True once a ShelfSlot has bound this tag directly to its bottle. Then we never do the
        // nearest-bottle search (which broke with duplicate SOs / recreated objects) and we stop
        // following by offset — the tag is a child of the slot and sits at its authored local pose.
        private bool _bound;

        /// <summary>Bind this tag directly to the bottle instance of its <see cref="Gameplay.Systems.ShelfSlot"/>.
        /// Replaces the nearest-bottle lookup: the tag tracks exactly THIS bottle and reads THIS SO.</summary>
        public void Bind(Gameplay.Interactions.Bottle instance, BottleSO so)
        {
            _bottle = so;
            _bottleInstance = instance;
            _bound = true;
            _followBottle = false; // child of the slot; authored local pose places it (no per-frame snap)
            _lastShow = null;      // force a re-evaluate on the new binding
            Apply();
        }

        void Awake()
        {
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);

            // Center the price over the bottle. The authored label sat offset to the right (a non-zero
            // anchoredPosition.x plus a right margin), which — combined with the tag facing away — read as
            // "shifted right". Zero the horizontal offset/margins and center the text so it sits squarely
            // above the bottle.
            if (_label != null)
            {
                var rt = _label.rectTransform;
                var ap = rt.anchoredPosition; ap.x = 0f; rt.anchoredPosition = ap;
                var m = _label.margin; m.x = 0f; m.z = 0f; _label.margin = m;
                _label.alignment = TMPro.TextAlignmentOptions.Center;
            }
        }

        void OnEnable() => Bind();
        void Start() => Bind();

        void OnDisable()
        {
            if (_subscribed)
            {
                if (_progression != null) _progression.UnlocksChanged -= Apply;
                if (_state != null) _state.StateChanged -= OnStateChanged;
            }
            _subscribed = false;
            _progression = null;
            _state = null;
            _lastShow = null;
        }

        void LateUpdate()
        {
            // Keep the tag glued above its bottle so the price always reads against the right bottle,
            // even after the bottle was moved in the editor, duplicated, or destroyed/recreated by the
            // night respawn. Re-resolve the instance whenever the cached one is gone.
            if (_followBottle && _bottle != null)
            {
                if (_bottleInstance == null) _bottleInstance = FindBottleInstance();
                if (_bottleInstance != null)
                    transform.position = _bottleInstance.transform.position + _followOffset;
            }

            // Reliable safety net (same reasoning as BottleUnlockGate.Update): keep the price label in
            // sync with the owned/shop state even if a StateChanged/UnlocksChanged event was missed.
            Apply();

            // No runtime re-orientation: the tags keep their authored (fixed) rotation. They're rotated
            // in the scene to face the player — NO camera billboard (that tilted/followed the head and
            // read as broken). The tag follows the bottle's POSITION only; its rotation stays put.
        }

        // Bind to the NEAREST bottle matching the SO, not just the first. With duplicate bottles of the
        // same type (e.g. two Champagnes), "first match" made every tag follow one bottle and left the
        // others with no price. Nearest-match gives each tag the bottle it was physically placed beside.
        private Gameplay.Interactions.Bottle FindBottleInstance()
        {
            var bottles = Object.FindObjectsByType<Gameplay.Interactions.Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Gameplay.Interactions.Bottle best = null;
            float bestSqr = float.MaxValue;
            Vector3 here = transform.position;
            for (int i = 0; i < bottles.Length; i++)
            {
                var b = bottles[i];
                if (b == null || b.SO != _bottle) continue;
                float sqr = (b.transform.position - here).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = b; }
            }
            return best;
        }

        private void Bind()
        {
            if (_progression == null) ServiceLocator.TryGet<IProgressionService>(out _progression);
            if (_state == null) ServiceLocator.TryGet<IGameStateService>(out _state);
            if (!_subscribed && _progression != null)
            {
                _progression.UnlocksChanged += Apply;
                if (_state != null) _state.StateChanged += OnStateChanged;
                _subscribed = true;
            }
            Apply();
        }

        private void OnStateChanged(GameState from, GameState to) => Apply();

        private void Apply()
        {
            if (_label == null || _bottle == null || _bottle.Ingredient == null) return;

            // Per-instance ownership: this tag hides only when ITS physical bottle is bought (or free),
            // not when any bottle of the same ingredient is owned — otherwise buying one of two
            // identical bottles would wrongly hide the price on the still-for-sale twin.
            if (_bottleInstance == null && !_bound) _bottleInstance = FindBottleInstance();
            bool owned;
            if (_bottle.UnlockCost <= 0) owned = true;                       // free bottle
            else if (_bottleInstance != null && _progression != null)
                owned = _progression.IsBottleInstanceOwned(_bottleInstance.InstanceId);
            else owned = false;
            // Show the price only while it's actually buyable: locked AND in the day shop.
            bool inShop = _state == null || _state.Current == GameState.DayShop;
            bool show = !owned && inShop;

            if (_lastShow.HasValue && _lastShow.Value == show) return; // unchanged → avoid per-frame work/alloc
            _lastShow = show;

            _label.enabled = show;
            if (show) _label.text = $"${Mathf.Max(0, _bottle.UnlockCost)}";
        }
    }
}
