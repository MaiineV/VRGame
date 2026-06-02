using UnityEngine;

namespace Data.SO
{
    [CreateAssetMenu(menuName = "Pour Decisions/Customer", fileName = "CustomerSO")]
    public sealed class CustomerSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _prefab;

        [Header("Behavior")]
        [Tooltip("World units per second while walking to/from the seat.")]
        [SerializeField] private float _walkSpeed = 1.4f;
        [Tooltip("Seconds the customer waits for a drink after sitting down.")]
        [SerializeField] private float _patienceSeconds = 70f;
        [Tooltip("Seconds the customer 'drinks' before leaving happy.")]
        [SerializeField] private float _drinkSeconds = 4f;

        [Header("Economy")]
        [SerializeField] private int _baseTip = 2;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public float WalkSpeed => _walkSpeed;
        public float PatienceSeconds => _patienceSeconds;
        public float DrinkSeconds => _drinkSeconds;
        public int BaseTip => _baseTip;
    }
}
