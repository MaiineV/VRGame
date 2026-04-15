using Data.Enums;

namespace Services.Economy
{
    public interface IEconomyService : IGameService
    {
        int Cash { get; }
        int Sales { get; }
        int FailedOrders { get; }
        int Expenses { get; }

        /// <summary>Sales gross minus expenses accumulated during the current night.</summary>
        int NightlyEarnings { get; }

        /// <summary>Add a paid sale. Returns the gross amount applied (price + tip).</summary>
        int RegisterSale(RecipeId recipe, float score, int tip);

        /// <summary>Customer left without paying (patience expired or wrong drink).</summary>
        void RegisterFailure(RecipeId recipe);

        /// <summary>Deduct cash for an expense (e.g. broken bottle). Reason is free-form for UI/telemetry.</summary>
        int RegisterExpense(int amount, string reason);

        void ResetForNewNight();

        event System.Action<int> CashChanged; // new cash value
        event System.Action<RecipeId, int> SaleRegistered; // recipe, gross
        event System.Action<int, string> ExpenseRegistered; // amount, reason
    }
}
