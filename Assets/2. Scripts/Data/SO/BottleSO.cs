using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Bottle", fileName = "BottleSO")]
    public sealed class BottleSO : ScriptableObject
    {
        [Header("Content")]
        [SerializeField] private IngredientSO _ingredient;
        [SerializeField] private float _capacityMl = 750f;

        [Header("Visual")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject _brokenPrefab;

        [Header("Economy")]
        [SerializeField] private int _repairCost = 50;

        public IngredientSO Ingredient => _ingredient;
        public float CapacityMl => _capacityMl;
        public GameObject Prefab => _prefab;
        public GameObject BrokenPrefab => _brokenPrefab;
        public int RepairCost => _repairCost;
    }
}
