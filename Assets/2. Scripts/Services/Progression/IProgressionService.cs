using System;
using System.Collections.Generic;
using Data.Enums;

namespace Services.Progression
{
    /// <summary>
    /// Owns day-shop progression: which recipes/bottles are unlocked and how much stock (in
    /// millilitres) has been purchased per bottle. Charges purchases through
    /// <see cref="Economy.IEconomyService"/> and persists through <see cref="Save.ISaveService"/>.
    /// Never references scene objects (the <c>BottleUnlockGate</c> component reads from here instead).
    /// </summary>
    public interface IProgressionService : IGameService
    {
        bool IsRecipeUnlocked(RecipeId recipe);
        bool IsBottleUnlocked(IngredientId ingredient);

        IReadOnlyCollection<RecipeId> UnlockedRecipes { get; }
        IReadOnlyCollection<IngredientId> UnlockedBottles { get; }

        /// <summary>Unlock a recipe and the bottles its ingredients require. Charges the recipe's
        /// UnlockCost via the economy. No-op returning false if already unlocked or unaffordable.</summary>
        bool UnlockRecipe(RecipeId recipe);

        /// <summary>Unlock a bottle directly in the day shop, charging its BottleSO.UnlockCost. Lets a
        /// bottle be sold as a standalone item (independent of recipes, which still auto-unlock the
        /// bottles they use). No-op returning false if already unlocked, unknown, or unaffordable.</summary>
        bool UnlockBottle(IngredientId ingredient);

        /// <summary>Buy <paramref name="units"/> of stock for an already-unlocked bottle. Charges
        /// units * BottleSO.StockUnitPrice and adds units * BottleSO.MlPerStockUnit ml. Returns
        /// false if the bottle is locked, units &lt;= 0, or it's unaffordable.</summary>
        bool BuyStock(IngredientId ingredient, int units);

        /// <summary>Purchased stock for a bottle, in millilitres.</summary>
        float GetStockMl(IngredientId ingredient);

        /// <summary>Returns the purchased ml for a bottle and zeroes its entry. Called at night
        /// start to transfer stock into the physical bottle (no carryover). Persists.</summary>
        float ConsumeStockForNight(IngredientId ingredient);

        /// <summary>Raised after any unlock or stock change so UI and bottle gates can refresh.</summary>
        event Action UnlocksChanged;
    }
}
