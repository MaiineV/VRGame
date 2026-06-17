using Core.FSM;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class DrinkingState : IState<CustomerEntity>
    {
        public void Enter(CustomerEntity c) { }

        public void Update(CustomerEntity c)
        {
            c.DrinkTimer -= Time.deltaTime;
            if (c.DrinkTimer <= 0f)
                c.Machine.TransitionTo(CustomerStateId.Leaving);
        }

        public void Exit(CustomerEntity c) { }
    }
}
