using System.Collections.Generic;
using Data.Enums;
using Data.SO;
using UnityEngine;
using Utilities;

namespace Services.Database
{
    public sealed class DatabaseService : IDatabaseService
    {
        private const string IngredientsResourcesPath = "Database/Ingredients";
        private const string BottlesResourcesPath = "Database/Bottles";
        private const string RecipesResourcesPath = "Database/Recipes";

        private readonly Dictionary<IngredientId, IngredientSO> _ingredients = new();
        private readonly Dictionary<IngredientId, BottleSO> _bottles = new();
        private readonly Dictionary<RecipeId, RecipeSO> _recipes = new();
        private RecipeSO[] _recipesList;

        public IReadOnlyList<RecipeSO> AllRecipes => _recipesList;

        public void Initialize()
        {
            LoadAll(IngredientsResourcesPath, _ingredients, so => so.Id);
            LoadAll(BottlesResourcesPath, _bottles, so => so.Ingredient != null ? so.Ingredient.Id : IngredientId.None);

            var recipes = Resources.LoadAll<RecipeSO>(RecipesResourcesPath);
            _recipesList = recipes;
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i] == null || recipes[i].Id == RecipeId.None) continue;
                _recipes[recipes[i].Id] = recipes[i];
            }

            MyLogger.LogInfo($"[DatabaseService] Loaded {_ingredients.Count} ingredients, {_bottles.Count} bottles, {_recipes.Count} recipes.");
        }

        public IngredientSO GetIngredient(IngredientId id) => _ingredients.TryGetValue(id, out var v) ? v : null;
        public BottleSO GetBottle(IngredientId id) => _bottles.TryGetValue(id, out var v) ? v : null;
        public RecipeSO GetRecipe(RecipeId id) => _recipes.TryGetValue(id, out var v) ? v : null;

        private static void LoadAll<TKey, TValue>(string path, Dictionary<TKey, TValue> dict, System.Func<TValue, TKey> keyOf)
            where TValue : Object
        {
            var assets = Resources.LoadAll<TValue>(path);
            for (int i = 0; i < assets.Length; i++)
            {
                var key = keyOf(assets[i]);
                if (key == null) continue;
                dict[key] = assets[i];
            }
        }
    }
}
