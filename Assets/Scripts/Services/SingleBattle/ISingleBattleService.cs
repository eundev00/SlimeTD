using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

public interface ISingleBattleService
{
    bool HasBattle { get; }
    BattleStage Stage { get; }

    UniTask<ResultCode> StartAsync(int stageId);
    void MarkStarted();
    void AdvanceTick();
    void RecordArrival(int slimeId);
    void RecordKill(int slimeId);
    void RecordSummon(UnityEngine.Vector2Int cell, int towerId);
    void RecordMerge(UnityEngine.Vector2Int cell, UnityEngine.Vector2Int partnerCell, int resultTowerId);
    void RecordSkillGold(UnityEngine.Vector2Int cell, int amount);
    UniTask FinishAsync(bool victory, int reachedWave, int life, int gold);
    void Abandon();
}
