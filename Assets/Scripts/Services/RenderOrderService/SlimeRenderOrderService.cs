public class SlimeRenderOrderService : ISlimeRenderOrderService
{
    private const int NormalBaseOrder = 10000;
    private const int BossBaseOrder = 20000;

    private int _normalCounter;
    private int _bossCounter;

    public int Next(SlimeRenderGroup group)
    {
        return group == SlimeRenderGroup.Boss
            ? BossBaseOrder - _bossCounter++
            : NormalBaseOrder - _normalCounter++;
    }

    public void Reset()
    {
        _normalCounter = 0;
        _bossCounter = 0;
    }
}
