using Core.FSM;
using UnityEngine;

namespace Gameplay.Customer.States
{
    /// <summary>
    /// Post-serve state: the satisfied customer stays seated "drinking" for
    /// <see cref="CustomerEntity.DrinkTimer"/> seconds (= <c>CustomerSO.DrinkSeconds</c>), then leaves via
    /// the exit route. No wandering — the bar's wander points were removed in favour of the editable
    /// entry/exit routes, so the customer simply dwells in place and then transitions to
    /// <see cref="CustomerStateId.Leaving"/>.
    /// </summary>
    public sealed class WanderingState : IState<CustomerEntity>
    {
        public void Enter(CustomerEntity c) { }

        public void Update(CustomerEntity c)
        {
            // DrinkTimer doubles as the "time spent in the bar after being served" budget. Counting it
            // down here keeps LeavingState's happy check (DrinkTimer < DrinkSeconds) correct.
            c.DrinkTimer -= Time.deltaTime;
            if (c.DrinkTimer <= 0f)
                c.Machine.TransitionTo(CustomerStateId.Leaving);
        }

        public void Exit(CustomerEntity c) { }
    }
}
