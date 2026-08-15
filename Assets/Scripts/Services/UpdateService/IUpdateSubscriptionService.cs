namespace Services.UpdateService
{
    public interface IUpdateSubscriptionService
    {
        void RegisterUpdatable(IUpdatable updatable);
        void UnregisterUpdatable(IUpdatable updatable);

        void RegisterLateUpdatable(ILateUpdatable lateUpdatable);
        void UnregisterLateUpdatable(ILateUpdatable lateUpdatable);

        void RegisterFixedUpdatable(IFixedUpdatable fixedUpdatable);
        void UnregisterFixedUpdatable(IFixedUpdatable fixedUpdatable);

        void RegisterPeriodicUpdatable(IPeriodicUpdatable periodicUpdatable, float interval);
        void UnregisterPeriodicUpdatable(IPeriodicUpdatable periodicUpdatable);
    }
}
