using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

// 협동은 서버가 전투를 돌리고 클라이언트는 받은 이벤트로 화면만 그린다 (서버 Protocol/coop_battle.proto)
public class CoopBattleService : ICoopBattleService, IDisposable
{
    private readonly IGameServerConnection _connection;
    private readonly IAccountService _accountService;
    private readonly Queue<CoopBattleEvent> _events = new Queue<CoopBattleEvent>();
    private readonly List<CoopBattlePlayer> _players = new List<CoopBattlePlayer>();
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private UniTaskCompletionSource<bool> _startSource;

    public long BattleId => _battleId;
    private long _battleId;

    public bool HasBattle { get; private set; }
    public BattleStage Stage { get; private set; }
    public int MySlot { get; private set; }
    public IReadOnlyList<CoopBattlePlayer> Players => _players;
    public int TicksPerSecond { get; private set; }
    public int LatestTick { get; private set; }
    public float LatestTickReceivedAt { get; private set; }
    public CoopBattleEndNtf End { get; private set; }

    public CoopBattleService(IGameServerConnection connection, IAccountService accountService)
    {
        _connection = connection;
        _accountService = accountService;

        _connection.ObserveNotification(MsgId.CoopBattleStartNtf, CoopBattleStartNtf.Parser).Subscribe(OnStart).AddTo(_disposables);
        _connection.ObserveNotification(MsgId.CoopBattleEventNtf, CoopBattleEventNtf.Parser).Subscribe(OnEvents).AddTo(_disposables);
        _connection.ObserveNotification(MsgId.CoopBattleEndNtf, CoopBattleEndNtf.Parser).Subscribe(OnEnd).AddTo(_disposables);
        _connection.IsConnected.Where(connected => !connected).Subscribe(_ => OnDisconnected()).AddTo(_disposables);
    }

    public async UniTask<ResultCode> MatchAsync(int stageId, bool withBot)
    {
        Clear();
        _startSource = new UniTaskCompletionSource<bool>();

        try
        {
            var res = await _connection.RequestAsync(MsgId.CoopBattleMatchReq, new CoopBattleMatchReq { StageId = stageId, WithBot = withBot }, MsgId.CoopBattleMatchRes, CoopBattleMatchRes.Parser);

            if (res.Result == ResultCode.Ok || res.Result == ResultCode.NotEnoughEnergy)
            {
                _accountService.ApplyItems(res.Items);
                _accountService.SetEnergyRecoverAt(res.EnergyRecoverAtMs);
            }

            if (res.Result != ResultCode.Ok)
                Debug.Log($"[CoopBattleService] 매칭 실패: {res.Result}");

            return res.Result;
        }
        catch (Exception e)
        {
            Debug.Log($"[CoopBattleService] 매칭 요청 실패: {e.Message}");
            return ResultCode.ServerError;
        }
    }

    public UniTask<bool> WaitStartAsync()
    {
        if (HasBattle)
            return UniTask.FromResult(true);

        return _startSource != null ? _startSource.Task : UniTask.FromResult(false);
    }

    public bool TryPeekEvent(out CoopBattleEvent battleEvent)
    {
        if (_events.Count == 0)
        {
            battleEvent = null;
            return false;
        }

        battleEvent = _events.Peek();
        return true;
    }

    public void DequeueEvent()
    {
        if (_events.Count > 0)
            _events.Dequeue();
    }

    public async UniTask LeaveAsync()
    {
        if (!HasBattle || End != null)
        {
            Clear();
            return;
        }

        long battleId = _battleId;
        Clear();

        try
        {
            var res = await _connection.RequestAsync(MsgId.CoopBattleLeaveReq, new CoopBattleLeaveReq(), MsgId.CoopBattleLeaveRes, CoopBattleLeaveRes.Parser);
            Debug.Log($"[CoopBattleService] 전투 나가기: {battleId} ({res.Result})");
        }
        catch (Exception e)
        {
            Debug.Log($"[CoopBattleService] 나가기 요청 실패: {e.Message}");
        }
    }

    private void OnStart(CoopBattleStartNtf ntf)
    {
        _events.Clear();
        _players.Clear();
        _players.AddRange(ntf.Players);

        _battleId = ntf.BattleId;
        Stage = ntf.Stage;
        MySlot = ntf.MySlot;
        TicksPerSecond = ntf.TicksPerSecond;
        LatestTick = 0;
        LatestTickReceivedAt = Time.realtimeSinceStartup;
        End = null;
        HasBattle = true;

        Debug.Log($"[CoopBattleService] 전투 시작: {_battleId}, 스테이지 {Stage.StageId} {Stage.CodeName}, 내 자리 {MySlot}");
        _startSource?.TrySetResult(true);
    }

    private void OnEvents(CoopBattleEventNtf ntf)
    {
        if (!HasBattle)
            return;

        LatestTick = ntf.Tick;
        LatestTickReceivedAt = Time.realtimeSinceStartup;

        foreach (var battleEvent in ntf.Events)
            _events.Enqueue(battleEvent);
    }

    private void OnEnd(CoopBattleEndNtf ntf)
    {
        if (!HasBattle || ntf.BattleId != _battleId)
            return;

        End = ntf;
        Debug.Log($"[CoopBattleService] 전투 끝: {ntf.Outcome}, 도달 웨이브 {ntf.ReachedWave}, 틱 {ntf.EndTick}");
    }

    private void OnDisconnected()
    {
        _startSource?.TrySetResult(false);
    }

    private void Clear()
    {
        HasBattle = false;
        Stage = null;
        End = null;
        _battleId = 0;
        _events.Clear();
        _players.Clear();
        _startSource?.TrySetResult(false);
        _startSource = null;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
