using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

// 오프라인 테스트용: 서버에 시작·결과를 보내지 않는다. 전투가 없는 상태라 게임 씬은 자기 스테이지와 UnityEngine.Random을 쓴다
public class OfflineSingleBattleService : ISingleBattleService
{
    private readonly IAccountService _accountService;
    private readonly ReactiveProperty<IReadOnlyList<ItemStack>> _rewards = new ReactiveProperty<IReadOnlyList<ItemStack>>();

    public bool HasBattle => false;
    public BattleStage Stage => null;
    public SlimeTD.Battle.BattleRandom SummonRandom => null;
    public SlimeTD.Battle.BattleRandom MergeRandom => null;
    public IReadOnlyReactiveProperty<IReadOnlyList<ItemStack>> Rewards => _rewards;

    public OfflineSingleBattleService(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public UniTask<ResultCode> StartAsync(int stageId)
    {
        _rewards.Value = null;
        return UniTask.FromResult(ResultCode.Ok);
    }

    public void MarkStarted() { }
    public void AdvanceTick() { }
    public void RecordArrival(int slimeId) { }
    public void RecordKill(int slimeId) { }
    public void RecordSummon(Vector2Int cell, int towerId) { }
    public void RecordMerge(Vector2Int cell, Vector2Int partnerCell, int resultTowerId) { }
    public void RecordSkillGold(Vector2Int cell, int amount) { }

    public UniTask FinishAsync(bool victory, int reachedWave, int life, int gold)
    {
        if (reachedWave > _accountService.BestWave.Value)
            _accountService.SetBestWave(reachedWave);

        _rewards.Value = Array.Empty<ItemStack>();
        return UniTask.CompletedTask;
    }

    public void Abandon() { }
}
