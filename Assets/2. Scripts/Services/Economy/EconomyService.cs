using Data.Enums;
using Services.Database;
using Services.Save;
using UnityEngine;
using Utilities;

namespace Services.Economy
{
    public sealed class EconomyService : IEconomyService
    {
        private readonly IDatabaseService _db;
        private readonly ISaveService _save;

        public int Cash { get; private set; }
        public int Sales { get; private set; }
        public int FailedOrders { get; private set; }
        public int Expenses { get; private set; }
        public int NightlyEarnings { get; private set; }

        public event System.Action<int> CashChanged;
        public event System.Action<RecipeId, int> SaleRegistered;
        public event System.Action<int, string> ExpenseRegistered;

        public EconomyService(IDatabaseService db, ISaveService save)
        {
            _db = db;
            _save = save;
        }

        public void Initialize()
        {
            Cash = _save.Current.cash;
            CashChanged?.Invoke(Cash);
        }

        public int RegisterSale(RecipeId recipe, float score, int tip)
        {
            var so = _db.GetRecipe(recipe);
            if (so == null) return 0;

            float scoreFactor = Mathf.Clamp01(score);
            int basePay = Mathf.RoundToInt(so.BasePrice * scoreFactor);
            int gross = basePay + Mathf.Max(0, tip);

            Cash += gross;
            Sales++;
            NightlyEarnings += gross;
            CashChanged?.Invoke(Cash);
            SaleRegistered?.Invoke(recipe, gross);
            MyLogger.LogInfo($"[Economy] Sale {recipe} +{gross} (base {basePay}, tip {tip}) -> cash {Cash}");
            return gross;
        }

        public void RegisterFailure(RecipeId recipe)
        {
            FailedOrders++;
            MyLogger.LogInfo($"[Economy] Failed order {recipe} (total failures {FailedOrders})");
        }

        public int RegisterExpense(int amount, string reason)
        {
            if (amount <= 0) return 0;
            Cash -= amount;
            Expenses += amount;
            NightlyEarnings -= amount;
            CashChanged?.Invoke(Cash);
            ExpenseRegistered?.Invoke(amount, reason);
            MyLogger.LogInfo($"[Economy] Expense -{amount} ({reason}) -> cash {Cash}");
            return amount;
        }

        /// <summary>Clears per-night counters only. Cash persists across nights.</summary>
        public void ResetForNewNight()
        {
            Sales = 0;
            FailedOrders = 0;
            Expenses = 0;
            NightlyEarnings = 0;
        }
    }
}
