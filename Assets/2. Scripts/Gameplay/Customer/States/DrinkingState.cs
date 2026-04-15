using HSM.Core.State;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class DrinkingState : BaseState
    {
        public override string StateId => CustomerStateIds.Drinking;

        protected override void OnUpdate(IStateContext context)
        {
            var c = context.GetService<CustomerEntity>();
            if (c == null) return;

            c.DrinkTimer -= Time.deltaTime;
            if (c.DrinkTimer <= 0f)
                context.StateMachine.TransitionTo(CustomerStateIds.Leaving);
        }
    }
}
