using Data.Enums;
using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Recipe", fileName = "RecipeSO")]
    public sealed class RecipeSO : ScriptableObject
    {
        [System.Serializable]
        public struct Step
        {
            public IngredientId id;
            [Tooltip("Target volume in ml.")]
            public float targetMl;
            [Tooltip("± tolerance in ml. Outside this band the step fails.")]
            public float toleranceMl;
        }

        [Header("Identity")]
        [SerializeField] private RecipeId _id = RecipeId.None;
        [SerializeField] private string _displayName;
        [SerializeField] private int _basePrice = 5;

        [Header("Progression")]
        [Tooltip("Cash charged to unlock this drink in the day shop. Unlocking it also enables the " +
                 "bottles for its ingredients.")]
        [SerializeField] private int _unlockCost = 0;
        [Tooltip("If true, this drink starts unlocked on a fresh/migrated save (keeps night 1 playable).")]
        [SerializeField] private bool _unlockedByDefault = false;

        [Header("Composition")]
        [SerializeField] private Step[] _steps;
        [Tooltip("Max volume of unlisted ingredients tolerated before the recipe fails.")]
        [SerializeField] private float _foreignToleranceMl = 5f;

        public RecipeId Id => _id;
        public string DisplayName => _displayName;
        public int BasePrice => _basePrice;
        public Step[] Steps => _steps;
        public float ForeignToleranceMl => _foreignToleranceMl;
        public int UnlockCost => _unlockCost;
        public bool UnlockedByDefault => _unlockedByDefault;
    }
}
