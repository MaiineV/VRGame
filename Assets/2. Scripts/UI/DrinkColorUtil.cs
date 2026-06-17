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
    }
}
