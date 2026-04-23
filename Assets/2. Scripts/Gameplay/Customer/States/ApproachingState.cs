using Core.FSM;

namespace Gameplay.Customer.States
{
    public sealed class ApproachingState : IState<CustomerEntity>
    {
        public void Enter(CustomerEntity c) { }

        public void Update(CustomerEntity c)
        {
            if (c.MoveTowards(c.Seat.transform.position))
                c.Machine.TransitionTo(CustomerStateId.Waiting);
        }

        public void Exit(CustomerEntity c) { }
    }
}
