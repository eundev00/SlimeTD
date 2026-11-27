using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

public interface ICoopBattleService
{
    // 시작 알림을 받은 뒤부터 전투가 끝나거나 나갈 때까지 true
    bool HasBattle { get; }
    long BattleId { get; }
    BattleStage Stage { get; }
    int MySlot { get; }
    IReadOnlyList<CoopBattlePlayer> Players { get; }
    int TicksPerSecond { get; }

    // 서버가 진행한 마지막 틱과 그 알림을 받은 시각 (Time.realtimeSinceStartup)
    int LatestTick { get; }
    float LatestTickReceivedAt { get; }

    // 끝 알림을 받기 전이면 null
    CoopBattleEndNtf End { get; }

    // 매칭 요청. Ok면 시작 알림을 WaitStartAsync로 기다린다
    UniTask<ResultCode> MatchAsync(int stageId, bool withBot);
    UniTask<bool> WaitStartAsync();

    // 받은 순서대로 꺼낸다. 게임 씬이 뜨기 전에 온 이벤트도 쌓여 있다
    bool TryPeekEvent(out CoopBattleEvent battleEvent);
    void DequeueEvent();

    UniTask LeaveAsync();
}
