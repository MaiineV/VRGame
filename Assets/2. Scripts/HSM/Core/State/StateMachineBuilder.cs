using HSM.Core.Event;

namespace HSM.Core.State
{
    public static class StateMachineBuilder
    {
        public static IStateMachine Create()
        {
            var eventBus = new EventBus();
            return Create(eventBus);
        }

        public static IStateMachine Create(IEventBus eventBus)
        {
            var context = new StateContext(eventBus);
            var stateMachine = new HierarchicalStateMachine(context);
            
            context.SetStateMachine(stateMachine);

            return stateMachine;
        }
    }
}