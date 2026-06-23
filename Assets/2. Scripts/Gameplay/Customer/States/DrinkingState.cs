using Core.FSM;
using UnityEngine;

namespace Gameplay.Customer.States
{
    /// <summary>
    /// At the table: the customer stops, faces the table, and plays the drink animation for
    /// <see cref="CustomerEntity.DrinkTimer"/> (= <c>CustomerSO.DrinkSeconds</c>) seconds, then leaves.
    /// Releases the reserved table point on exit. This is the table-flow counterpart to the in-place
    /// dwell of <see cref="WanderingState"/>.
    /// </summary>
    public sealed class DrinkingState : IState<CustomerEntity>
    {
        public void Enter(CustomerEntity c)
        {
            c.StopAgent();
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
