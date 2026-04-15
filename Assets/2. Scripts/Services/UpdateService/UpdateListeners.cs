namespace Services.UpdateService
{
    public interface IUpdateListener
    {
        void MyUpdate();
    }

    public interface IFixedUpdateListener
    {
        void MyFixedUpdate();
    }

    public interface ILateUpdateListener
    {
        void MyLateUpdate();
    }
}
