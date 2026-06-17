using Data.Enums;
using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Ingredient", fileName = "IngredientSO")]
    public sealed class IngredientSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private IngredientId _id = IngredientId.None;
        [SerializeField] private string _displayName;
        [SerializeField] private IngredientType _type = IngredientType.Alcohol;

        [Header("Visual")]
        [SerializeField] private Color _liquidColor = Color.white;

        [Header("Pour")]
        [Tooltip("ml/sec at max tilt. Scaled down by tilt factor in PourDetector.")]
        [SerializeField] private float _pourRateMlPerSec = 30f;

        [Header("Economy")]
        [SerializeField] private int _unitCost = 1;

        public IngredientId Id => _id;
        public string DisplayName => _displayName;
        public IngredientType Type => _type;
        public Color LiquidColor => _liquidColor;
        public float PourRateMlPerSec => _pourRateMlPerSec;
        public int UnitCost => _unitCost;
    }
}
