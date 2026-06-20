using System;
using System.Collections.Generic;
using Data.Enums;
using Data.SO;
using Services.Database;
using Services.Economy;
using Services.Save;
using UnityEngine;
using Utilities;

namespace Services.Progression
{
    public sealed class ProgressionService : IProgressionService
    {
        private readonly IEconomyService _economy;
        private readonly ISaveService _save;
        private readonly IDatabaseService _db;

        private readonly HashSet<RecipeId> _recipes = new();
        private readonly HashSet<IngredientId> _bottles = new();
        private readonly Dictionary<IngredientId, int> _stockMl = new();

        public IReadOnlyCollection<RecipeId> UnlockedRecipes => _recipes;
        public IReadOnlyCollection<IngredientId> UnlockedBottles => _bottles;

        public event Action UnlocksChanged;

        public ProgressionService(IEconomyService economy, ISaveService save, IDatabaseService db)
        {
            _economy = economy;
            _save = save;
            _db = db;
        }

        public void Initialize()
        {
            LoadFromSave();

            // An empty recipe set means a brand-new or freshly-migrated (v1) save. Seed the starter
            // unlocks so night 1 is playable without forcing a shop visit. Recipes can't be un-
            // unlocked, so empty unambiguously means "never seeded".
            if (_recipes.Count == 0)
            {
                SeedDefaults();
                Persist();
                MyLogger.LogInfo($"[Progression] Seeded defaults: {_recipes.Count} recipe(s), {_bottles.Count} bottle(s).");
            }
            else
            {
                MyLogger.LogInfo($"[Progression] Loaded {_recipes.Count} recipe(s), {_bottles.Count} bottle(s), {_stockMl.Count} stock entr(ies).");
            }
        }

        public bool IsRecipeUnlocked(RecipeId recipe) => _recipes.Contains(recipe);
        public bool IsBottleUnlocked(IngredientId ingredient) => _bottles.Contains(ingredient);

        public float GetStockMl(IngredientId ingredient) =>
            _stockMl.TryGetValue(ingredient, out var ml) ? ml : 0f;

        public bool UnlockRecipe(RecipeId recipe)
        {
            if (recipe == RecipeId.None) return false;
            if (_recipes.Contains(recipe)) return false;            // already unlocked: no charge

            var so = _db.GetRecipe(recipe);
            if (so == null) { MyLogger.LogWarning($"[Progression] UnlockRecipe: unknown recipe {recipe}."); return false; }

            int cost = Mathf.Max(0, so.UnlockCost);
            if (_economy.Cash < cost) return false;                 // unaffordable

            if (cost > 0) _economy.RegisterExpense(cost, $"Unlock {so.DisplayName}");
            ApplyRecipeUnlock(so);
            Persist();
            UnlocksChanged?.Invoke();
            MyLogger.LogInfo($"[Progression] Unlocked recipe {recipe} for {cost} -> cash {_economy.Cash}.");
            return true;
        }

        public bool UnlockBottle(IngredientId ingredient)
        {
            if (ingredient == IngredientId.None) return false;
            if (_bottles.Contains(ingredient)) return false;        // already unlocked: no charge

            var bottle = _db.GetBottle(ingredient);
            if (bottle == null) { MyLogger.LogWarning($"[Progression] UnlockBottle: no bottle for {ingredient}."); return false; }

            int cost = Mathf.Max(0, bottle.UnlockCost);
            if (_economy.Cash < cost) return false;                 // unaffordable

            if (cost > 0) _economy.RegisterExpense(cost, $"Unlock bottle {ingredient}");
            _bottles.Add(ingredient);
            UnlockRecipesUsing(ingredient);
            Persist();
            UnlocksChanged?.Invoke();
            MyLogger.LogInfo($"[Progression] Unlocked bottle {ingredient} for {cost} -> cash {_economy.Cash}.");
            return true;
        }

        public bool BuyStock(IngredientId ingredient, int units)
        {
            if (units <= 0) return false;
            if (!_bottles.Contains(ingredient)) return false;       // can only stock unlocked bottles

            var bottle = _db.GetBottle(ingredient);
            if (bottle == null) { MyLogger.LogWarning($"[Progression] BuyStock: no bottle for {ingredient}."); return false; }

            int cost = Mathf.Max(0, bottle.StockUnitPrice) * units;
            if (_economy.Cash < cost) return false;                 // unaffordable

            if (cost > 0) _economy.RegisterExpense(cost, $"Stock {ingredient} x{units}");
            int addMl = Mathf.RoundToInt(Mathf.Max(0f, bottle.MlPerStockUnit) * units);
            _stockMl.TryGetValue(ingredient, out var cur);
            _stockMl[ingredient] = cur + addMl;
            Persist();
            UnlocksChanged?.Invoke();
            MyLogger.LogInfo($"[Progression] Bought {units}x stock of {ingredient} (+{addMl}ml) for {cost} -> {_stockMl[ingredient]}ml.");
            return true;
        }

        public float ConsumeStockForNight(IngredientId ingredient)
        {
            if (!_stockMl.TryGetValue(ingredient, out var ml) || ml <= 0) return 0f;
            _stockMl[ingredient] = 0;
            Persist();
            return ml;
        }

        // --- internals ----------------------------------------------------------------------------

        private void ApplyRecipeUnlock(RecipeSO so)
        {
            _recipes.Add(so.Id);
            if (so.Steps == null) return;
            for (int i = 0; i < so.Steps.Length; i++)
            {
                var ing = so.Steps[i].id;
                if (ing != IngredientId.None) _bottles.Add(ing);
            }
        }

        /// <summary>Unlock (recipe id only, no bottle auto-grant) every recipe whose steps use this
        /// ingredient, so buying a bottle makes its drink serveable. Multi-ingredient recipes still
        /// need their other bottles physically present to be poured.</summary>
        private void UnlockRecipesUsing(IngredientId ingredient)
        {
            var recipes = _db.AllRecipes;
            if (recipes == null) return;
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                if (r == null || r.Id == RecipeId.None || r.Steps == null) continue;
                for (int s = 0; s < r.Steps.Length; s++)
                {
                    if (r.Steps[s].id == ingredient) { _recipes.Add(r.Id); break; }
                }
            }
        }

        private void SeedDefaults()
        {
            var recipes = _db.AllRecipes;
            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    var r = recipes[i];
                    if (r != null && r.Id != RecipeId.None && r.UnlockedByDefault)
                        ApplyRecipeUnlock(r);
                }
            }

            // Bottles flagged UnlockedByDefault start usable from night 1 on their own, independent of
            // any recipe (e.g. a starter bottle to pour with before its recipe is unlocked/bought).
            var bottles = _db.AllBottles;
            if (bottles != null)
            {
                for (int i = 0; i < bottles.Count; i++)
                {
                    var b = bottles[i];
                    if (b != null && b.Ingredient != null && b.UnlockedByDefault)
                        _bottles.Add(b.Ingredient.Id);
                }
            }
        }

        private void LoadFromSave()
        {
            _recipes.Clear();
            _bottles.Clear();
            _stockMl.Clear();

            var d = _save.Current;
            if (d == null) return;

            if (d.unlockedRecipes != null)
                foreach (var id in d.unlockedRecipes) _recipes.Add((RecipeId)id);
            if (d.unlockedBottles != null)
                foreach (var id in d.unlockedBottles) _bottles.Add((IngredientId)id);
            if (d.stock != null)
                foreach (var e in d.stock)
                    if (e.ml > 0) _stockMl[(IngredientId)e.ingredientId] = e.ml;
        }

        private void WriteToSave()
        {
            var d = _save.Current;
            if (d == null) return;

            d.unlockedRecipes ??= new List<int>();
            d.unlockedBottles ??= new List<int>();
            d.stock ??= new List<StockEntry>();

            d.unlockedRecipes.Clear();
            foreach (var r in _recipes) d.unlockedRecipes.Add((int)r);

            d.unlockedBottles.Clear();
            foreach (var b in _bottles) d.unlockedBottles.Add((int)b);

            d.stock.Clear();
            foreach (var kv in _stockMl)
                if (kv.Value > 0) d.stock.Add(new StockEntry { ingredientId = (int)kv.Key, ml = kv.Value });
        }

        /// <summary>Mirror in-memory state into SaveData, keep cash in sync (purchases happen
        /// outside the night boundary where GameStateService normally persists), and write to disk.</summary>
        private void Persist()
        {
            WriteToSave();
            if (_save.Current != null) _save.Current.cash = _economy.Cash;
            _save.Save();
        }
    }
}
