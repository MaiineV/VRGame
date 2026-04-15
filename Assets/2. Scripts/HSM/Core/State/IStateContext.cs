using HSM.Core.Event;

namespace HSM.Core.State
{
    public interface IStateContext
    {
        IEventBus EventBus { get; }
        IStateMachine StateMachine { get; }
        T GetService<T>() where T : class;
        void SetData<T>(string key, T value);
        T GetData<T>(string key);
        bool HasData(string key);
    }
}