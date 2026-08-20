public enum GameProgressType
{
    WaveStarted,
    WaveSpawnFinished,
    WaveCleared,
    GameOver
}

public readonly struct GameProgressEvent
{
    public readonly GameProgressType EventType;
    public readonly int WaveIndex;
    public readonly int SlimeCount; // WaveStarted: 예상 스폰 개수, WaveSpawnFinished: 실제 스폰된 개수

    public GameProgressEvent(GameProgressType eventType, int waveIndex = 0, int slimeCount = 0)
    {
        EventType = eventType;
        WaveIndex = waveIndex;
        SlimeCount = slimeCount;
    }
}
