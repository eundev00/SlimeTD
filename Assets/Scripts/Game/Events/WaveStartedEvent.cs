public readonly struct WaveStartedEvent
{
    public readonly int WaveIndex;

    public WaveStartedEvent(int waveIndex)
    {
        WaveIndex = waveIndex;
    }
}
