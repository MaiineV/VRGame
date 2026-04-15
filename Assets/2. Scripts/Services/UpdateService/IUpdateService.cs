namespace Services.UpdateService
{
    public interface IUpdateService : IGameService
    {
        void AddUpdateListener(IUpdateListener listener);
        void AddFixedUpdateListener(IFixedUpdateListener listener);
        void AddLateUpdateListener(ILateUpdateListener listener);

        void RemoveUpdateListener(IUpdateListener listener);
        void RemoveFixedUpdateListener(IFixedUpdateListener listener);
        void RemoveLateUpdateListener(ILateUpdateListener listener);

        void MyUpdate();
        void MyFixedUpdate();
        void MyLateUpdate();
    }
}
