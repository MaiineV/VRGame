using System.Collections.Generic;

namespace Services.Save
{
    /// <summary>
    /// On-disk progression state. Keep fields flat and JsonUtility-friendly
    /// (no properties, no Dictionary, no nullable). Bump <see cref="version"/>
    /// whenever the shape changes so old files can be migrated or discarded.
    /// </summary>
    [System.Serializable]
    public sealed class SaveData
    {
        public int version = CurrentVersion;
        public int cash;
        public int nightsCompleted;
        public int bestNightEarnings;

        // --- v2: day-shop progression ---------------------------------------------------------
        // Unlocked recipes (RecipeId cast to int) and the bottles their ingredients enable
        // (IngredientId cast to int). Flat Lists because JsonUtility can't serialize HashSet /
        // Dictionary. Per-bottle purchased stock (in millilitres) lives in <see cref="stock"/>.
        public List<int> unlockedRecipes = new();
        public List<int> unlockedBottles = new();
        public List<StockEntry> stock = new();

        public const int CurrentVersion = 2;
    }

    /// <summary>Purchased stock for one bottle, in millilitres, keyed by IngredientId (as int).</summary>
    [System.Serializable]
    public struct StockEntry
    {
        public int ingredientId;
        public int ml;
    }
}
