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

        // --- v3: per-instance bottle ownership ------------------------------------------------
        // Each purchasable bottle placed in the scene has a unique BottleUnlockGate._instanceId.
        // Buying a bottle records its instance id here, so two bottles of the SAME ingredient are
        // bought independently (buying one no longer unlocks both). unlockedBottles still tracks
        // which INGREDIENTS are usable (recipe serveability / pour), granted when any instance is owned.
        public List<int> ownedBottleInstances = new();

        // v4: no shape change — bumped only to force the v3→v4 migration to WIPE ownedBottleInstances.
        // Early v3 builds (and a prior null-SO respawn bug) left stale per-instance ownership in saved
        // files, so purchasable bottles came back owned/free without being paid for. Clearing the list
        // on migration returns them to "for sale"; ownership earned afterwards (by grabbing/paying in the
        // shop) is permanent as designed.
        public const int CurrentVersion = 4;
    }

    /// <summary>Purchased stock for one bottle, in millilitres, keyed by IngredientId (as int).</summary>
    [System.Serializable]
    public struct StockEntry
    {
        public int ingredientId;
        public int ml;
    }
}
