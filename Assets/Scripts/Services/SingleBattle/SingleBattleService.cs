using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UnityEngine;

// 싱글은 클라이언트가 혼자 플레이하고, 끝나면 서버가 같은 전투를 다시 계산해 결과를 확인한다
public class SingleBattleService : ISingleBattleService
{
    private const float TicksPerSecond = 30f;

    private readonly IGameServerConnection _connection;
    private readonly IAccountService _accountService;
    private readonly List<SlimeArrival> _arrivals = new List<SlimeArrival>();

    private long _battleId;
    private float _startTime;
    private bool _started;

    public bool HasBattle { get; private set; }
    public BattleStage Stage { get; private set; }

    public SingleBattleService(IGameServerConnection connection, IAccountService accountService)
    {
        _connection = connection;
        _accountService = accountService;
    }

    public async UniTask<ResultCode> StartAsync(int stageId)
    {
        Clear();

        try
        {
            var res = await _connection.RequestAsync(MsgId.SingleBattleStartReq, new SingleBattleStartReq { StageId = stageId }, MsgId.SingleBattleStartRes, SingleBattleStartRes.Parser);
            if (res.Result != ResultCode.Ok)
            {
                Debug.Log($"[SingleBattleService] 전투 시작 실패: {res.Result}");
                return res.Result;
            }

            _battleId = res.BattleId;
            Stage = res.Stage;
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

        _startTime = Time.time;
        _started = true;
    }

    public void RecordArrival(int slimeId)
    {
        if (!HasBattle || !_started)
            return;

        _arrivals.Add(new SlimeArrival { SlimeId = slimeId, Tick = CurrentTick() });
    }

    public async UniTask FinishAsync(bool victory, int reachedWave, int life)
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
        };
        req.Arrivals.AddRange(_arrivals);
        Clear();

        try
        {
            var res = await _connection.RequestAsync(MsgId.SingleBattleFinishReq, req, MsgId.SingleBattleFinishRes, SingleBattleFinishRes.Parser);
            if (res.Result == ResultCode.Ok)
            {
                _accountService.SetBestWave(res.BestWave);
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
        return Mathf.RoundToInt((Time.time - _startTime) * TicksPerSecond);
    }

    private void Clear()
    {
        HasBattle = false;
        Stage = null;
        _battleId = 0;
        _started = false;
        _arrivals.Clear();
    }
}
