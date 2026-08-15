namespace Services.UpdateService
{
    public interface IPeriodicUpdatable
    {
        // deltaTime: 마지막 호출 이후 경과 시간
        void ManagedPeriodicUpdate(float deltaTime);
    }
}
