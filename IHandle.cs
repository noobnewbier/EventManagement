namespace EventManagement
{
    public interface IHandle
    {
    }

    public interface IHandle<in T> : IHandle
    {
        void Handle(T @event);
    }
}