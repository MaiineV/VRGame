using System.Collections.Generic;
using Data.Enums;
using Data.SO;
using Services;
using Services.Database;
using Services.Economy;
using Services.GameState;
using Services.Progression;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// Diegetic day-shop panel, active only in <see cref="GameState.DayShop"/>. Mirrors the
    /// NightClipboard pattern: physical PokeButtons, state-gated group, no pointer/raycaster.
    ///
    /// A single cursor walks a combined catalog so the physical button count stays fixed regardless
    /// of how many items exist:
    ///   - locked drinks  -> "Desbloquear" (charges RecipeSO.UnlockCost, enables its bottles)
    ///   - unlocked bottles -> "Comprar stock" (charges BottleSO.StockUnitPrice * units)
    /// Prev/Next move the cursor, Buy acts on the selection, Start Night begins the next night.
    /// </summary>
    public sealed class ShopBoard : MonoBehaviour
    {
        private enum EntryKind { UnlockRecipe, UnlockBottle, BuyStock }

        private struct ShopEntry
        {
            public EntryKind kind;
            public RecipeId recipe;
            public IngredientId ingredient;
        }

        [Header("Config")]
        [Tooltip("Staged as PendingConfig when enabled, so Start Night knows which night to run.")]
        [SerializeField] private NightConfigSO _config;

        [Header("Group (active only in DayShop)")]
        [SerializeField] private GameObject _shopGroup;

        [Header("Buttons")]
        [SerializeField] private PokeButton _prevButton;
        [SerializeField] private PokeButton _nextButton;
        [SerializeField] private PokeButton _buyButton;
        [SerializeField] private PokeButton _startNightButton;

        [Header("Labels")]
        [SerializeField] private TMP_Text _selectionLabel; // current item: name, action, cost
        [SerializeField] private TMP_Text _listLabel;      // optional full catalog
        [SerializeField] private TMP_Text _cashLabel;      // optional cash readout

        [Header("Tuning")]
        [Tooltip("How many stock units a single Buy press purchases.")]
        [SerializeField] private int _stockUnitsPerBuy = 1;

        private IGameStateService _state;
        private IProgressionService _progression;
        private IDatabaseService _db;
        private IEconomyService _economy;

        private readonly List<ShopEntry> _entries = new();
        private int _index;
        private bool _servicesBound;

        void OnEnable()
        {
            // Button events don't depend on services and are safe to wire immediately.
            if (_prevButton != null) _prevButton.Pressed += OnPrevPressed;
            if (_nextButton != null) _nextButton.Pressed += OnNextPressed;
            if (_buyButton != null) _buyButton.Pressed += OnBuyPressed;
            if (_startNightButton != null) _startNightButton.Pressed += OnStartNightPressed;

            BindServices();
        }

        // Start() retries the service binding in case OnEnable ran before GameBootstrap.Awake
        // registered the services (scene init-order race).
        void Start() => BindServices();

        void OnDisable()
        {
            if (_prevButton != null) _prevButton.Pressed -= OnPrevPressed;
            if (_nextButton != null) _nextButton.Pressed -= OnNextPressed;
            if (_buyButton != null) _buyButton.Pressed -= OnBuyPressed;
            if (_startNightButton != null) _startNightButton.Pressed -= OnStartNightPressed;

            if (_servicesBound)
            {
                if (_state != null) _state.StateChanged -= OnStateChanged;
                if (_progression != null) _progression.UnlocksChanged -= OnUnlocksChanged;
                if (_economy != null) _economy.CashChanged -= OnCashChanged;
            }
            _servicesBound = false;
            _state = null;
            _progression = null;
            _db = null;
            _economy = null;
        }

        private void BindServices()
        {
            if (_servicesBound) return;
            if (!ServiceLocator.TryGet<IGameStateService>(out _state)) return; // not ready; Start() retries

            ServiceLocator.TryGet<IProgressionService>(out _progression);
            ServiceLocator.TryGet<IDatabaseService>(out _db);
            ServiceLocator.TryGet<IEconomyService>(out _economy);

            if (_config != null) _state.SetPendingConfig(_config);

            _state.StateChanged += OnStateChanged;
            if (_progression != null) _progression.UnlocksChanged += OnUnlocksChanged;
            if (_economy != null) _economy.CashChanged += OnCashChanged;
            _servicesBound = true;

            ApplyState(_state.Current);
        }

        // --- state -------------------------------------------------------------------------------

        private void OnStateChanged(GameState from, GameState to) => ApplyState(to);

        private void ApplyState(GameState s)
        {
            bool inShop = s == GameState.DayShop;
            if (_shopGroup != null) _shopGroup.SetActive(inShop);
            if (inShop) RebuildCatalog();
            RefreshInteractable();
        }

        private void OnUnlocksChanged() => RebuildCatalog();
        private void OnCashChanged(int _) => RefreshLabels();

        private bool InShop => _state != null && _state.Current == GameState.DayShop;

        // --- presses -----------------------------------------------------------------------------

        private void OnPrevPressed()
        {
            if (!InShop || _entries.Count == 0) return;
            _index = (_index - 1 + _entries.Count) % _entries.Count;
            RefreshLabels();
        }

        private void OnNextPressed()
        {
            if (!InShop || _entries.Count == 0) return;
            _index = (_index + 1) % _entries.Count;
            RefreshLabels();
        }

        private void OnBuyPressed()
        {
            if (!InShop || _progression == null || _entries.Count == 0) return;
            var e = _entries[Mathf.Clamp(_index, 0, _entries.Count - 1)];
            if (e.kind == EntryKind.UnlockRecipe)
                _progression.UnlockRecipe(e.recipe);
            else if (e.kind == EntryKind.UnlockBottle)
                _progression.UnlockBottle(e.ingredient);
            else
                _progression.BuyStock(e.ingredient, Mathf.Max(1, _stockUnitsPerBuy));
            // On success UnlocksChanged fires -> RebuildCatalog. Rebuild anyway in case nothing changed.
            RebuildCatalog();
        }

        private void OnStartNightPressed()
        {
            if (InShop) _state?.BeginNight();
        }

        // --- catalog -----------------------------------------------------------------------------

        private void RebuildCatalog()
        {
            _entries.Clear();
            if (_db != null)
            {
                var recipes = _db.AllRecipes;
                if (recipes != null)
                {
                    for (int i = 0; i < recipes.Count; i++)
                    {
                        var r = recipes[i];
                        if (r == null || r.Id == RecipeId.None) continue;
                        if (_progression == null || !_progression.IsRecipeUnlocked(r.Id))
                            _entries.Add(new ShopEntry { kind = EntryKind.UnlockRecipe, recipe = r.Id });
                    }
                }

                // NOTE: locked bottles are NOT listed here. Each physical bottle is bought individually by
                // grabbing it on the shelf (BottleUnlockGate.OnGrabbed → UnlockBottleInstance), so a menu
                // entry keyed by ingredient would wrongly buy every bottle of that type at once.

                if (_progression != null)
                {
                    foreach (var ing in _progression.UnlockedBottles)
                    {
                        if (_db.GetBottle(ing) != null)
                            _entries.Add(new ShopEntry { kind = EntryKind.BuyStock, ingredient = ing });
                    }
                }
            }

            if (_index >= _entries.Count) _index = _entries.Count > 0 ? _entries.Count - 1 : 0;
            RefreshLabels();
        }

        private int CostOf(ShopEntry e)
        {
            if (_db == null) return 0;
            if (e.kind == EntryKind.UnlockRecipe)
            {
                var r = _db.GetRecipe(e.recipe);
                return r != null ? Mathf.Max(0, r.UnlockCost) : 0;
            }
            if (e.kind == EntryKind.UnlockBottle)
            {
                var bo = _db.GetBottle(e.ingredient);
                return bo != null ? Mathf.Max(0, bo.UnlockCost) : 0;
            }
            var b = _db.GetBottle(e.ingredient);
            return b != null ? Mathf.Max(0, b.StockUnitPrice) * Mathf.Max(1, _stockUnitsPerBuy) : 0;
        }

        private string LabelOf(ShopEntry e)
        {
            if (_db == null) return e.ToString();
            if (e.kind == EntryKind.UnlockRecipe)
            {
                var r = _db.GetRecipe(e.recipe);
                string n = r != null ? r.DisplayName : e.recipe.ToString();
                return $"Desbloquear: {n}";
            }
            else if (e.kind == EntryKind.UnlockBottle)
            {
                var b = _db.GetBottle(e.ingredient);
                string n = b != null && b.Ingredient != null ? b.Ingredient.DisplayName : e.ingredient.ToString();
                return $"Comprar botella: {n}";
            }
            else
            {
                var b = _db.GetBottle(e.ingredient);
                string n = b != null && b.Ingredient != null ? b.Ingredient.DisplayName : e.ingredient.ToString();
                float ml = _progression != null ? _progression.GetStockMl(e.ingredient) : 0f;
                return $"Stock x{Mathf.Max(1, _stockUnitsPerBuy)}: {n} (tenés {Mathf.RoundToInt(ml)}ml)";
            }
        }

        private void RefreshLabels()
        {
            int cash = _economy != null ? _economy.Cash : 0;
            if (_cashLabel != null) _cashLabel.text = $"${cash}";

            if (_selectionLabel != null)
            {
                if (_entries.Count == 0)
                {
                    _selectionLabel.text = "(nada para comprar)";
                }
                else
                {
                    var e = _entries[Mathf.Clamp(_index, 0, _entries.Count - 1)];
                    int cost = CostOf(e);
                    string aff = cash >= cost ? "" : "  (sin plata)";
                    _selectionLabel.text = $"[{_index + 1}/{_entries.Count}] {LabelOf(e)}  ${cost}{aff}";
                }
            }

            if (_listLabel != null) _listLabel.text = BuildListText();

            RefreshInteractable();
        }

        private string BuildListText()
        {
            if (_entries.Count == 0) return string.Empty;
            var sb = new System.Text.StringBuilder(256);
            for (int i = 0; i < _entries.Count; i++)
            {
                if (i == _index) sb.Append("> "); else sb.Append("  ");
                sb.Append(LabelOf(_entries[i])).Append("  $").Append(CostOf(_entries[i])).Append('\n');
            }
            return sb.ToString();
        }

        private void RefreshInteractable()
        {
            bool inShop = InShop;
            int cash = _economy != null ? _economy.Cash : 0;
            bool has = _entries.Count > 0;
            bool afford = has && cash >= CostOf(_entries[Mathf.Clamp(_index, 0, _entries.Count - 1)]);

            if (_prevButton != null) _prevButton.Interactable = inShop && _entries.Count > 1;
            if (_nextButton != null) _nextButton.Interactable = inShop && _entries.Count > 1;
            if (_buyButton != null) _buyButton.Interactable = inShop && has && afford;
            if (_startNightButton != null) _startNightButton.Interactable = inShop;
        }
    }
}
