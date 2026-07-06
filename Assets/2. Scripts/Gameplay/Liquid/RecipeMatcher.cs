using Data.SO;
using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Matches a poured mix against a recipe by ingredient PROPORTIONS, not absolute ml. The fill
    /// level is a separate scored axis (the customer picks a target level per order), so the poured
    /// total rarely equals the recipe's authored total — normalizing both sides against their own
    /// totals lets a correct 1:3 gin/tonic pass at any requested fill level, while keeping the
    /// authored targetMl/toleranceMl numbers as the designer-intended ratios.
    /// </summary>
    public static class RecipeMatcher
    {
        // Absorbs float noise so a zero-tolerance step can't fail on a rounding error.
        private const float RatioEpsilon = 0.001f;

        /// <summary>
        /// True when every recipe step's share of the pour is within its tolerance band and the
        /// unlisted ("foreign") share stays within ForeignToleranceMl — all normalized against the
        /// recipe's total volume. Missing recipe data defaults to true (don't punish on bad data,
        /// same stance as the previous dominant-ingredient check).
        /// </summary>
        public static bool Matches(LiquidMix mix, RecipeSO recipe)
        {
            if (mix == null || mix.IsEmpty || mix.TotalMl <= 0f) return false;
            if (recipe == null || recipe.Steps == null || recipe.Steps.Length == 0) return true;

            var steps = recipe.Steps;
            float recipeTotal = 0f;
            for (int i = 0; i < steps.Length; i++) recipeTotal += steps[i].targetMl;
            if (recipeTotal <= 0f) return true;

            float poured = mix.TotalMl;
            float listedMl = 0f;
            // Note: a recipe listing the same IngredientId in two steps would double-count listedMl;
            // no current recipe asset does that.
            for (int i = 0; i < steps.Length; i++)
            {
                float have = mix.VolumeOf(steps[i].id);
                listedMl += have;

                float targetRatio = steps[i].targetMl / recipeTotal;
                float tolRatio = steps[i].toleranceMl / recipeTotal;
                if (Mathf.Abs(have / poured - targetRatio) > tolRatio + RatioEpsilon) return false;
            }

            float foreignRatio = (poured - listedMl) / poured;
            return foreignRatio <= recipe.ForeignToleranceMl / recipeTotal + RatioEpsilon;
        }
    }
}
