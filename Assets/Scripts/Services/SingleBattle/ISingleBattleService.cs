using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

public interface ISingleBattleService
{
    bool HasBattle { get; }
    BattleStage Stage { get; }

    // 진행 중인 싱글 전투가 없으면 null (그때는 UnityEngine.Random으로 뽑는다)
    SlimeTD.Battle.BattleRandom SummonRandom { get; }
    SlimeTD.Battle.BattleRandom MergeRandom { get; }

    // 서버가 결과를 확인하기 전엔 null, 확인에 실패하면 빈 목록
    UniRx.IReadOnlyReactiveProperty<System.Collections.Generic.IReadOnlyList<ItemStack>> Rewards { get; }

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
