using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Bottle", fileName = "BottleSO")]
    public sealed class BottleSO : ScriptableObject
    {
        [Header("Content")]
        [SerializeField] private IngredientSO _ingredient;
        [SerializeField] private float _capacityMl = 10000f;

        [Header("Visual")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject _brokenPrefab;

        [Header("Economy")]
        [SerializeField] private int _repairCost = 50;

        [Header("Shop / Stock")]
        [Tooltip("Informational: cost to first enable this bottle. Bottles are actually unlocked as a " +
                 "side effect of unlocking a recipe that uses this ingredient.")]
        [SerializeField] private int _unlockCost = 0;
        [Tooltip("Cash charged per stock unit bought in the day shop.")]
        [SerializeField] private int _stockUnitPrice = 5;
        [Tooltip("Millilitres granted per stock unit purchased. Stock is consumed into the bottle at " +
                 "night start (no carryover).")]
        [SerializeField] private float _mlPerStockUnit = 1000f;
        [Tooltip("If true, this bottle starts unlocked on a fresh/migrated save.")]
        [SerializeField] private bool _unlockedByDefault = false;

        public IngredientSO Ingredient => _ingredient;
        public float CapacityMl => _capacityMl;
        public GameObject Prefab => _prefab;
        public GameObject BrokenPrefab => _brokenPrefab;
        public int RepairCost => _repairCost;
        public int UnlockCost => _unlockCost;
        public int StockUnitPrice => _stockUnitPrice;
        public float MlPerStockUnit => _mlPerStockUnit;
        public bool UnlockedByDefault => _unlockedByDefault;
    }
}
