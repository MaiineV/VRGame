using Data.Enums;
using Data.SO;
using Gameplay.Liquid;

namespace Services.Recipe
{
    public readonly struct RecipeMatch
    {
        public readonly RecipeSO Recipe;
        public readonly float Score;     // 0..1
        public readonly bool IsExact;    // every step within tolerance and no foreign

        public RecipeMatch(RecipeSO recipe, float score, bool isExact)
        {
            Recipe = recipe;
            Score = score;
            IsExact = isExact;
        }

        public bool IsValid => Recipe != null;
        public static RecipeMatch None => new(null, 0f, false);
    }

    public interface IRecipeService : IGameService
    {
        /// <summary>Match the mix against a target recipe (ticket validation).</summary>
        RecipeMatch Evaluate(LiquidMix mix, RecipeId targetRecipe);

        /// <summary>Best-fit search across all known recipes (free-pour / sandbox).</summary>
        RecipeMatch FindBest(LiquidMix mix);
    }
}
