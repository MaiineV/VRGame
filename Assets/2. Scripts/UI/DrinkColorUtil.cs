using Data.Enums;
using Services;
using Services.Database;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Resolves a drink's signature colour (the recipe's main ingredient LiquidColor) so the
    /// NPC indicator, the bottle tag and the glass tint all speak the same colour language.
    /// </summary>
    public static class DrinkColorUtil
    {
        public static Color For(RecipeId recipe)
        {
            if (!ServiceLocator.TryGet<IDatabaseService>(out var db)) return Color.white;
            var r = db.GetRecipe(recipe);
            IngredientId id = (r != null && r.Steps != null && r.Steps.Length > 0)
                ? r.Steps[0].id
                : IngredientId.None;
            if (id == IngredientId.None) return Color.white;
            var ing = db.GetIngredient(id);
            return ing != null ? ing.LiquidColor : Color.white;
        }

        /// <summary>One entry per recipe ingredient: its LiquidColor and its share of the recipe's
        /// total targetMl. Same proportion math as <see cref="Gameplay.Liquid.RecipeMatcher"/>, so the
        /// segments a UI draws from this always match what actually counts as "correct" when poured.
        /// Falls back to a single white, full-ratio segment when the recipe can't be resolved.</summary>
        public static (Color color, float ratio)[] Segments(RecipeId recipe)
        {
            var fallback = new[] { (Color.white, 1f) };
            if (!ServiceLocator.TryGet<IDatabaseService>(out var db)) return fallback;
            var r = db.GetRecipe(recipe);
            if (r == null || r.Steps == null || r.Steps.Length == 0) return fallback;

            float total = 0f;
            for (int i = 0; i < r.Steps.Length; i++) total += r.Steps[i].targetMl;
            if (total <= 0f) return fallback;

            var segments = new (Color, float)[r.Steps.Length];
            for (int i = 0; i < r.Steps.Length; i++)
            {
                var ing = db.GetIngredient(r.Steps[i].id);
                Color c = ing != null ? ing.LiquidColor : Color.white;
                segments[i] = (c, r.Steps[i].targetMl / total);
            }
            return segments;
        }
    }
}
