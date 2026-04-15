using HSM.Core.State;
using HSM.Events;

namespace Gameplay.Customer.States
{
    public sealed class ApproachingState : BaseState
    {
        public override string StateId => CustomerStateIds.Approaching;

        protected override void OnUpdate(IStateContext context)
        {
            var c = context.GetService<CustomerEntity>();
            if (c == null) return;

            if (c.MoveTowards(c.Seat.transform.position))
            {
                PublishEvent(new CustomerOrderPlacedEvent(c.Seat.Index, c.TargetRecipe));
                context.StateMachine.TransitionTo(CustomerStateIds.Waiting);
            }
        }
    }
}
