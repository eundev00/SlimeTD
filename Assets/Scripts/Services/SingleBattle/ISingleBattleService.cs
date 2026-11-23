using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

public interface ISingleBattleService
{
    bool HasBattle { get; }
    BattleStage Stage { get; }

    UniTask<ResultCode> StartAsync(int stageId);
    void MarkStarted();
    void RecordArrival(int slimeId);
    UniTask FinishAsync(bool victory, int reachedWave, int life);
    void Abandon();
}
