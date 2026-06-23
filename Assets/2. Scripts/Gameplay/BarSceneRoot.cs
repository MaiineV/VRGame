using Gameplay.Customer;
using Services;
using Services.GameState;
using UnityEngine;
using Utilities;

namespace Gameplay
{
    public sealed class BarSceneRoot : MonoBehaviour
    {
        public static BarSceneRoot Instance { get; private set; }

        [Header("Player")]
        [SerializeField] private Transform _playerAnchor;

        [Header("Seats (customer positions facing the bar)")]
        [SerializeField] private CustomerSeatPoint[] _seats;

        [Header("Customer flow")]
        [SerializeField] private Transform _customerSpawnPoint;
        [SerializeField] private Transform _customerExitPoint;
        [Tooltip("Editable path customers walk on the way IN (spawn → seat). Each child of the CustomerRoute " +
                 "is a waypoint; NavMesh handles avoiding obstacles between them. Optional — empty = go direct.")]
        [SerializeField] private Customer.CustomerRoute _entryRoute;
        [Tooltip("Editable path customers walk on the way OUT (seat → exit). Same idea as the entry route.")]
        [SerializeField] private Customer.CustomerRoute _exitRoute;

        [Tooltip("Spots a served customer walks to in order to 'drink' before leaving (place on the NavMesh " +
                 "near tables). Optional — if empty, served customers fall back to drinking in place.")]
        [SerializeField] private Customer.CustomerTablePoint[] _tablePoints;

        [Header("Bottle storage positions (behind the bar)")]
        [SerializeField] private Transform[] _bottleShelfPoints;

        [Header("Cash register")]
        [SerializeField] private Transform _cashRegisterAnchor;

        [Header("Debug")]
        [Tooltip("If true, BeginNight() is called on Start (skips the clipboard). Use only to verify the NPC/spawn flow when interactions are broken.")]
        [SerializeField] private bool _autoStartNight;

        public Transform PlayerAnchor => _playerAnchor;
        public CustomerSeatPoint[] Seats => _seats;
        public Transform CustomerSpawnPoint => _customerSpawnPoint;
        public Transform CustomerExitPoint => _customerExitPoint;
        public Customer.CustomerRoute EntryRoute => _entryRoute;
        public Customer.CustomerRoute ExitRoute => _exitRoute;
        public Customer.CustomerTablePoint[] TablePoints => _tablePoints;
        public Transform[] BottleShelfPoints => _bottleShelfPoints;
        public Transform CashRegisterAnchor => _cashRegisterAnchor;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                MyLogger.LogWarning("[BarSceneRoot] Duplicate instance, destroying.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (!_autoStartNight) return;

            if (ServiceLocator.TryGet<IGameStateService>(out var state))
            {
                MyLogger.LogInfo("[BarSceneRoot] _autoStartNight=true → calling BeginNight().");
                state.BeginNight();
            }
            else
            {
                MyLogger.LogWarning("[BarSceneRoot] _autoStartNight=true but IGameStateService not registered.");
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public CustomerSeatPoint GetFreeSeat()
        {
            if (_seats == null) return null;
            for (int i = 0; i < _seats.Length; i++)
                if (_seats[i] != null && !_seats[i].IsOccupied)
                    return _seats[i];
            return null;
        }

        public Customer.CustomerTablePoint GetFreeTablePoint()
        {
            if (_tablePoints == null) return null;
            for (int i = 0; i < _tablePoints.Length; i++)
                if (_tablePoints[i] != null && !_tablePoints[i].IsOccupied)
                    return _tablePoints[i];
            return null;
        }
    }
}
