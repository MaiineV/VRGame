using Data.Enums;
using Data.SO;
using Gameplay.Liquid;
using Services.Database;
using UnityEngine;

namespace Services.Recipe
{
    public sealed class RecipeService : IRecipeService
    {
        private readonly IDatabaseService _db;

        public RecipeService(IDatabaseService db)
        {
            _db = db;
        }

        public void Initialize() { }

        public RecipeMatch Evaluate(LiquidMix mix, RecipeId targetRecipe)
        {
            var recipe = _db.GetRecipe(targetRecipe);
            if (recipe == null || mix == null) return RecipeMatch.None;
            return Score(mix, recipe);
        }

        public RecipeMatch FindBest(LiquidMix mix)
        {
            if (mix == null || mix.IsEmpty) return RecipeMatch.None;

            var all = _db.AllRecipes;
            if (all == null || all.Count == 0) return RecipeMatch.None;

            var best = RecipeMatch.None;
            for (int i = 0; i < all.Count; i++)
            {
                var m = Score(mix, all[i]);
                if (m.Score > best.Score) best = m;
            }
            return best;
        }

        private static RecipeMatch Score(LiquidMix mix, RecipeSO recipe)
        {
            var steps = recipe.Steps;
            if (steps == null || steps.Length == 0) return new RecipeMatch(recipe, 0f, false);

            float total = 0f;
            bool exact = true;
            float listedSum = 0f;

            for (int i = 0; i < steps.Length; i++)
            {
                var s = steps[i];
                float actual = mix.VolumeOf(s.id);
                listedSum += actual;

                float diff = Mathf.Abs(actual - s.targetMl);
                float band = Mathf.Max(0.01f, s.toleranceMl);
                float stepScore = Mathf.Clamp01(1f - diff / Mathf.Max(band, s.targetMl));
                total += stepScore;

                if (diff > band) exact = false;
            }

            float foreignMl = Mathf.Max(0f, mix.TotalMl - listedSum);
            if (foreignMl > recipe.ForeignToleranceMl) exact = false;

            float avg = total / steps.Length;
            float foreignPenalty = Mathf.Clamp01(foreignMl / Mathf.Max(1f, mix.TotalMl));
            float score = Mathf.Clamp01(avg * (1f - foreignPenalty));

            return new RecipeMatch(recipe, score, exact);
        }
    }
}
