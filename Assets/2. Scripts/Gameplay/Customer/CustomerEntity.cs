using Core.FSM;
using Data.Enums;
using Data.SO;
using Gameplay.Customer.States;
using Gameplay.Liquid;
using Services;
using Services.Recipe;
using Services.UpdateService;
using UnityEngine;
using Utilities;

namespace Gameplay.Customer
{
    public sealed class CustomerEntity : MonoBehaviour, IUpdateListener
    {
        public CustomerSO So { get; private set; }
        public CustomerSeatPoint Seat { get; private set; }
        public RecipeId TargetRecipe { get; private set; }
        public Transform ExitPoint { get; private set; }
        public StateMachine<CustomerStateId, CustomerEntity> Machine { get; private set; }

        public float WaitTimer;
        public float DrinkTimer;
        public float Drunkenness;

        public event System.Action<CustomerEntity, RecipeId, float, bool> Served;
        public event System.Action<CustomerEntity, bool> Left;

        public void RaiseServed(RecipeId recipe, float score, bool isExact) => Served?.Invoke(this, recipe, score, isExact);
        public void RaiseLeft(bool happy) => Left?.Invoke(this, happy);

        private bool _registered;
        private bool _initialized;

        public void Init(CustomerSO so, CustomerSeatPoint seat, RecipeId recipe, Transform exitPoint)
        {
            So = so;
            Seat = seat;
            TargetRecipe = recipe;
            ExitPoint = exitPoint;
            WaitTimer = so.PatienceSeconds;
            DrinkTimer = so.DrinkSeconds;

            Machine = new StateMachine<CustomerStateId, CustomerEntity>(this);
            Machine.AddState(CustomerStateId.Approaching, new ApproachingState());
            Machine.AddState(CustomerStateId.Waiting, new WaitingState());
            Machine.AddState(CustomerStateId.Drinking, new DrinkingState());
            Machine.AddState(CustomerStateId.Leaving, new LeavingState());
            Machine.Start(CustomerStateId.Approaching);

            _initialized = true;
            RegisterTick();
        }

        void OnEnable()
        {
            if (_initialized) RegisterTick();
        }

        void OnDisable() => UnregisterTick();

        void OnDestroy()
        {
            UnregisterTick();
            if (Seat != null && Seat.CurrentCustomer == this) Seat.Clear();
            Machine?.Shutdown();
        }

        public void MyUpdate() => Machine?.Update();

        public bool MoveTowards(Vector3 target, float arriveDistance = 0.05f)
        {
            var step = So.WalkSpeed * Time.deltaTime;
            var pos = transform.position;
            var dir = target - pos;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist <= arriveDistance) return true;

            transform.position = pos + dir.normalized * Mathf.Min(step, dist);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
            return false;
        }

        public void DespawnNow() => Destroy(gameObject);

        private void RegisterTick()
        {
            if (_registered) return;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
            svc.AddUpdateListener(this);
            _registered = true;
        }

        private void UnregisterTick()
        {
            if (!_registered) return;
            if (ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _registered = false;
        }
    }
}
