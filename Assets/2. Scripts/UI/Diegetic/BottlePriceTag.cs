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
        [Tooltip("If true, the tag rotates to face the main camera each frame (billboard).")]
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

            if (!_faceCamera) return;
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }

        private Gameplay.Interactions.Bottle FindBottleInstance()
        {
            var bottles = Object.FindObjectsByType<Gameplay.Interactions.Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bottles.Length; i++)
                if (bottles[i] != null && bottles[i].SO == _bottle) return bottles[i];
            return null;
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

            _label.enabled = show;
            if (show) _label.text = $"${Mathf.Max(0, _bottle.UnlockCost)}";
        }
    }
}
