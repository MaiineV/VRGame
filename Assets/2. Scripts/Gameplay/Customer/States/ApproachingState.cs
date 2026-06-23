using Core.FSM;
using Gameplay;

namespace Gameplay.Customer.States
{
    public sealed class ApproachingState : IState<CustomerEntity>
    {
        // Index of the next entry-route waypoint to walk to (before heading to the seat).
        private int _wp;

        public void Enter(CustomerEntity c) => _wp = 0;

        public void Update(CustomerEntity c)
        {
            // Walk the editable entry route first (spawn → ... → near the bar), then go to the seat.
            var route = BarSceneRoot.Instance != null ? BarSceneRoot.Instance.EntryRoute : null;
            if (route != null && _wp < route.Count)
            {
                if (c.MoveTowards(route.GetPoint(_wp), 0.25f)) _wp++;
                return;
            }

            if (c.MoveTowards(c.Seat.transform.position))
                c.Machine.TransitionTo(CustomerStateId.Waiting);
        }

        public void Exit(CustomerEntity c) { }
    }
}
