using UnityEngine;

// 서버 틱을 로컬 시간으로 이어 그린다. 알림이 0.1초마다 와서 그대로 쓰면 끊겨 보이므로, 조금 늦게(RenderDelay) 따라가며 오차를 천천히 줄인다
public class CoopBattleClock : ICoopBattleClock
{
    private const float RenderDelaySeconds = 0.15f;
    private const float MaxExtrapolationSeconds = 0.5f;
    private const float CorrectionPerSecond = 2f;
    private const float SnapSeconds = 1f;

    private readonly ICoopBattleService _service;
    private bool _started;

    public float Tick { get; private set; }

    public CoopBattleClock(ICoopBattleService service)
    {
        _service = service;
    }

    public void Advance(float deltaTime)
    {
        int ticksPerSecond = Mathf.Max(1, _service.TicksPerSecond);
        float target = TargetTick(ticksPerSecond);

        if (!_started || Mathf.Abs(target - Tick) > SnapSeconds * ticksPerSecond)
        {
            _started = true;
            Tick = Mathf.Max(Tick, target);
            return;
        }

        float next = Tick + deltaTime * ticksPerSecond;
        next += (target - next) * Mathf.Min(1f, deltaTime * CorrectionPerSecond);

        // 되감으면 슬라임이 뒤로 움직이므로 시간은 앞으로만 간다
        Tick = Mathf.Max(Tick, next);
    }

    // 서버가 알린 마지막 틱 + 받은 뒤 흐른 시간 - 그리기 지연. 알림이 끊기면 멀리 앞서 나가지 않게 막는다
    private float TargetTick(int ticksPerSecond)
    {
        float elapsed = Mathf.Min(Time.realtimeSinceStartup - _service.LatestTickReceivedAt, MaxExtrapolationSeconds);
        return Mathf.Max(0f, _service.LatestTick + (elapsed - RenderDelaySeconds) * ticksPerSecond);
    }
}
