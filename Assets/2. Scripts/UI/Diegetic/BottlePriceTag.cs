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
        private Transform _cam;
        private Gameplay.Interactions.Bottle _bottleInstance;
        private bool? _lastShow;
        private bool _oriented;   // true once the tag has been locked to its static facing

        void Awake()
        {
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
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

            // Orient once toward the player, yaw only (upright), then leave it static. A full per-frame
            // LookRotation pitched the tag up/down as the player tilted their head to look at the shelf,
            // which read as "broken" text. Static + upright + facing the player fixes that.
            if (!_faceCamera || _oriented) return;
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam == null) return;
            Vector3 away = transform.position - _cam.position; // TMP's readable face is -Z, so +Z points away from the viewer
            away.y = 0f;
            if (away.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
            _oriented = true;
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

            bool owned = _progression != null && _progression.IsBottleUnlocked(_bottle.Ingredient.Id);
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
