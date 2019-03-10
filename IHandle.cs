namespace Engine.Support.EventAggregator
{
    public interface IHandle {}
    public interface IHandle<in T> : IHandle
    {
        void Handle(T @event);
    }
}