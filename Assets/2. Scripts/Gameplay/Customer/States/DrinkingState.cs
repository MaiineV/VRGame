using Core.FSM;
using UnityEngine;

namespace Gameplay.Customer.States
{
    /// <summary>
    /// At the bar seat: the customer takes the served glass in hand, stays seated facing the bar, and
    /// plays the drink animation for <see cref="CustomerEntity.DrinkTimer"/> (= <c>CustomerSO.DrinkSeconds</c>)
    /// seconds, then leaves through the door. Entered straight from <see cref="WaitingState"/> on an
    /// accepted serve — the customer no longer walks off to a table to drink. The
    /// <see cref="CustomerEntity.ReleaseTable"/> on exit is a defensive no-op in this flow (no table is
    /// ever reserved).
    /// </summary>
    public sealed class DrinkingState : IState<CustomerEntity>
    {
        public void Enter(CustomerEntity c)
        {
            c.StopAgent();
            // Drink in place at the bar seat: hold the served glass in hand and keep the seated facing
            // (Sit() in WaitingState already turned the customer toward the serve point / bar). The table
            // branch is kept only as a defensive no-op — this flow never reserves a table.
            c.CarryServedGlassVisible();
            if (c.Table != null) c.FaceWorldPoint(c.Table.LookAtPoint.position);
            c.DrankAtTable = true;
            c.PlayDrink();
        }

        public void Update(CustomerEntity c)
        {
            // DrinkTimer doubles as the post-serve "time in the bar" budget; counting it down here keeps
            // LeavingState's happy check (DrinkTimer < DrinkSeconds) correct.
            c.DrinkTimer -= Time.deltaTime;
            if (c.DrinkTimer <= 0f)
                c.Machine.TransitionTo(CustomerStateId.Leaving);
        }

        public void Exit(CustomerEntity c)
        {
            c.ClearForcedAnim();
            c.ReleaseTable();
        }
    }
}
