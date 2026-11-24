using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

// 싱글은 클라이언트가 혼자 플레이하고, 끝나면 서버가 같은 전투를 다시 계산해 결과를 확인한다
public class SingleBattleService : ISingleBattleService
{
    private readonly IGameServerConnection _connection;
    private readonly IAccountService _accountService;
    private readonly List<SlimeArrival> _arrivals = new List<SlimeArrival>();
    private readonly List<SlimeKill> _kills = new List<SlimeKill>();
    private readonly List<HunterSummon> _summons = new List<HunterSummon>();
    private readonly List<HunterMerge> _merges = new List<HunterMerge>();
    private readonly List<SkillGold> _skillGolds = new List<SkillGold>();
    private readonly ReactiveProperty<IReadOnlyList<ItemStack>> _rewards = new ReactiveProperty<IReadOnlyList<ItemStack>>();

    private long _battleId;
    private int _tick;
    private bool _started;

    public bool HasBattle { get; private set; }
    public BattleStage Stage { get; private set; }
    public SlimeTD.Battle.BattleRandom SummonRandom { get; private set; }
    public SlimeTD.Battle.BattleRandom MergeRandom { get; private set; }
    public IReadOnlyReactiveProperty<IReadOnlyList<ItemStack>> Rewards => _rewards;

    public SingleBattleService(IGameServerConnection connection, IAccountService accountService)
    {
        _connection = connection;
        _accountService = accountService;
    }

    public async UniTask<ResultCode> StartAsync(int stageId)
    {
        Clear();
        _rewards.Value = null;

        try
        {
            var res = await _connection.RequestAsync(MsgId.SingleBattleStartReq, new SingleBattleStartReq { StageId = stageId }, MsgId.SingleBattleStartRes, SingleBattleStartRes.Parser);

            // 성공이면 쓴 뒤, 에너지가 모자라면 회복을 반영한 현재 에너지가 온다
            if (res.Result == ResultCode.Ok || res.Result == ResultCode.NotEnoughEnergy)
            {
                _accountService.ApplyItems(res.Items);
                _accountService.SetEnergyRecoverAt(res.EnergyRecoverAtMs);
            }

            if (res.Result != ResultCode.Ok)
            {
                Debug.Log($"[SingleBattleService] 전투 시작 실패: {res.Result}");
                return res.Result;
            }

            _battleId = res.BattleId;
            Stage = res.Stage;
            SummonRandom = SlimeTD.Battle.BattleRandom.ForSummon(res.Seed);
            MergeRandom = SlimeTD.Battle.BattleRandom.ForMerge(res.Seed);
            HasBattle = true;
            Debug.Log($"[SingleBattleService] 전투 시작: {_battleId}, 스테이지 {Stage.StageId} {Stage.CodeName}");
            return ResultCode.Ok;
        }
        catch (Exception e)
        {
            Debug.Log($"[SingleBattleService] 전투 시작 요청 실패: {e.Message}");
            return ResultCode.ServerError;
        }
    }

    public void MarkStarted()
    {
        if (!HasBattle)
            return;

        _tick = 0;
        _started = true;
    }

    // 고정 업데이트 1번 = 1틱 (WaveSpawner가 매 고정 업데이트 처음에 부른다)
    public void AdvanceTick()
    {
        if (IsRecording)
            _tick++;
    }

    public void RecordArrival(int slimeId)
    {
        if (!HasBattle || !_started)
            return;

        _arrivals.Add(new SlimeArrival { SlimeId = slimeId, Tick = CurrentTick() });
    }

    public void RecordKill(int slimeId)
    {
        if (IsRecording)
            _kills.Add(new SlimeKill { SlimeId = slimeId, Tick = CurrentTick() });
    }

    public void RecordSummon(Vector2Int cell, int towerId)
    {
        if (IsRecording)
            _summons.Add(new HunterSummon { Tick = CurrentTick(), X = cell.x, Y = cell.y, TowerId = towerId });
    }

    public void RecordMerge(Vector2Int cell, Vector2Int partnerCell, int resultTowerId)
    {
        if (IsRecording)
            _merges.Add(new HunterMerge { Tick = CurrentTick(), X = cell.x, Y = cell.y, PartnerX = partnerCell.x, PartnerY = partnerCell.y, ResultTowerId = resultTowerId });
    }

    public void RecordSkillGold(Vector2Int cell, int amount)
    {
        if (IsRecording)
            _skillGolds.Add(new SkillGold { Tick = CurrentTick(), X = cell.x, Y = cell.y, Amount = amount });
    }

    private bool IsRecording => HasBattle && _started;

    public async UniTask FinishAsync(bool victory, int reachedWave, int life, int gold)
    {
        if (!HasBattle || !_started)
            return;

        var req = new SingleBattleFinishReq
        {
            BattleId = _battleId,
            Outcome = victory ? BattleOutcome.Victory : BattleOutcome.Defeat,
            ReachedWave = reachedWave,
            Life = life,
            EndTick = CurrentTick(),
            Gold = gold,
        };
        req.Arrivals.AddRange(_arrivals);
        req.Kills.AddRange(_kills);
        req.Summons.AddRange(_summons);
        req.Merges.AddRange(_merges);
        req.SkillGolds.AddRange(_skillGolds);
        Clear();

        try
        {
            var res = await _connection.RequestAsync(MsgId.SingleBattleFinishReq, req, MsgId.SingleBattleFinishRes, SingleBattleFinishRes.Parser);
            if (res.Result == ResultCode.Ok)
            {
                _accountService.SetBestWave(res.BestWave);
                _accountService.ApplyItems(res.Items);
                _rewards.Value = new List<ItemStack>(res.Rewards);
                Debug.Log($"[SingleBattleService] 서버 검증 일치: {res.Outcome}, 웨이브 {res.ReachedWave}, 최고 {res.BestWave}, 신기록 {res.NewRecord}, 끝 틱 {req.EndTick}");
                return;
            }

            Debug.LogWarning($"[SingleBattleService] 전투 결과 확인 실패: {res.Result}, 서버 계산 {res.Outcome} 웨이브 {res.ReachedWave}");
            foreach (var m in res.Mismatches)
                Debug.LogWarning($"[SingleBattleService] 불일치 {m.Field}: 클라 {m.Client} / 서버 {m.Server}");
        }
        catch (Exception e)
        {
            Debug.Log($"[SingleBattleService] 전투 종료 요청 실패: {e.Message}");
        }

        _rewards.Value = Array.Empty<ItemStack>();
    }

    // 끝나기 전에 게임 씬을 나가면 버린다. 서버 쪽 전투는 다음 시작 때 무효가 된다
    public void Abandon()
    {
        if (HasBattle)
            Debug.Log($"[SingleBattleService] 전투를 끝내지 않고 나갔습니다: {_battleId}");

        Clear();
    }

    private int CurrentTick()
    {
        return _tick;
    }

    private void Clear()
    {
        HasBattle = false;
        Stage = null;
        SummonRandom = null;
        MergeRandom = null;
        _battleId = 0;
        _started = false;
        _tick = 0;
        _arrivals.Clear();
        _kills.Clear();
        _summons.Clear();
        _merges.Clear();
        _skillGolds.Clear();
    }
}
