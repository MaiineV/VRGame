using Data.Enums;
using Data.SO;

namespace Services.Database
{
    /// <summary>
    /// Read-only catalog of game data SOs. Loaded once at boot, cached in dictionaries
    /// for O(1) lookup. Never query Resources/AssetDatabase from gameplay code.
    /// </summary>
    public interface IDatabaseService : IGameService
    {
        IngredientSO GetIngredient(IngredientId id);
        BottleSO GetBottle(IngredientId id);
        RecipeSO GetRecipe(RecipeId id);

        System.Collections.Generic.IReadOnlyList<RecipeSO> AllRecipes { get; }
    }
}
