using Data.Enums;
using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Night Config", fileName = "NightConfigSO")]
    public sealed class NightConfigSO : ScriptableObject
    {
        [Header("Duration")]
        [Tooltip("Total night length in seconds.")]
        [SerializeField] private float _durationSeconds = 180f;

        [Header("Spawning")]
        [Tooltip("Seconds between spawn attempts. Skipped if no free seat.")]
        [SerializeField] private float _spawnIntervalSeconds = 8f;
        [SerializeField] private int _maxSimultaneous = 3;

        [Header("Pools")]
        [SerializeField] private CustomerSO[] _customerPool;
        [SerializeField] private RecipeId[] _recipePool;

        [Header("Rules")]
        [SerializeField] private DrunkennessConfigSO _drunkennessConfig;

        public DrunkennessConfigSO DrunkennessConfig => _drunkennessConfig;

        public float DurationSeconds => _durationSeconds;
        public float SpawnIntervalSeconds => _spawnIntervalSeconds;
        public int MaxSimultaneous => _maxSimultaneous;
        public CustomerSO[] CustomerPool => _customerPool;
        public RecipeId[] RecipePool => _recipePool;
    }
}
