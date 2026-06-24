using Core.FSM;
using UnityEngine;

namespace Gameplay.Customer.States
{
    /// <summary>
    /// Post-serve: the customer takes the glass (hidden, along with its fill gauge), plays a short "grab"
    /// beat at the bar, then
    /// walks to a reserved <see cref="CustomerTablePoint"/> to drink. Falls back to the legacy in-place
    /// dwell (<see cref="CustomerStateId.Wandering"/>) when no table point is free or authored, so the
    /// feature degrades gracefully. A walk timeout guards against a table point left off the NavMesh.
    /// </summary>
    public sealed class GoingToTableState : IState<CustomerEntity>
    {
        // Seconds the customer pauses at the bar "grabbing" the glass before walking off.
        private const float GrabBeatSeconds = 0.6f;
        // Hard cap on the walk to the table; if exceeded (e.g. table off the NavMesh) drink where we are.
        private const float WalkTimeoutSeconds = 12f;

        private float _beatTimer;
        private float _walkTimer;
        private bool _hasTable;

        public void Enter(CustomerEntity c)
        {
            _beatTimer = GrabBeatSeconds;
            _walkTimer = WalkTimeoutSeconds;

            c.Stand();                    // release the seated agent so it can walk again
            c.CarryServedGlassHidden();   // take the glass, but hide it and its fill gauge once grabbed

            var table = BarSceneRoot.Instance != null ? BarSceneRoot.Instance.GetFreeTablePoint() : null;
            _hasTable = table != null;
            if (_hasTable)
            {
                c.Table = table;
                table.Bind(c);
            }

            c.PlayGrabBeat();             // hold Idle while "grabbing"
        }

        public void Update(CustomerEntity c)
        {
            // Brief grab beat at the bar before moving.
            if (_beatTimer > 0f)
            {
                _beatTimer -= Time.deltaTime;
                if (_beatTimer <= 0f) c.ClearForcedAnim();
                return;
            }

            // No table available → drink in place (legacy behaviour).
            if (!_hasTable)
            {
                c.Machine.TransitionTo(CustomerStateId.Wandering);
                return;
            }

            // Backstop: never hang if the table point can't be reached.
            _walkTimer -= Time.deltaTime;
            if (_walkTimer <= 0f)
            {
                c.Machine.TransitionTo(CustomerStateId.Drinking);
                return;
            }

            if (c.MoveTowards(c.Table.transform.position, 0.2f))
                c.Machine.TransitionTo(CustomerStateId.Drinking);
        }

        public void Exit(CustomerEntity c) { }
    }
}
