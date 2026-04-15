using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Drunkenness Config", fileName = "DrunkennessConfigSO")]
    public sealed class DrunkennessConfigSO : ScriptableObject
    {
        [Header("Alcohol to drunkenness")]
        [Tooltip("Alcohol ml that saturates drunkenness to 1.")]
        [SerializeField] private float _alcoholMlForMax = 60f;

        [Header("Effects")]
        [Tooltip("Tip multiplier at drunkenness=1. At 0 no bonus. Linear.")]
        [SerializeField] private float _maxTipMultiplier = 1.5f;

        [Tooltip("Lateral wobble amplitude (meters) at drunkenness=1 when walking away.")]
        [SerializeField] private float _wobbleAmplitude = 0.2f;

        [Tooltip("Wobble frequency (Hz).")]
        [SerializeField] private float _wobbleFrequency = 2.0f;

        public float AlcoholMlForMax => _alcoholMlForMax;
        public float MaxTipMultiplier => _maxTipMultiplier;
        public float WobbleAmplitude => _wobbleAmplitude;
        public float WobbleFrequency => _wobbleFrequency;

        public float TipMultiplier(float drunkenness) => Mathf.Lerp(1f, _maxTipMultiplier, Mathf.Clamp01(drunkenness));
    }
}
