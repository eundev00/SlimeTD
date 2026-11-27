using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.UpdateService;
using SlimeTD.Protocol;
using UnityEngine;

// 협동 화면: 서버 이벤트를 그 틱이 되면 적용한다 (웨이브 표시, 슬라임 등장·도착, 라이프, 끝)
public class CoopBattlePresenter : IUpdatable, IDisposable
{
    private readonly ICoopBattleService _coopBattleService;
    private readonly IGameplayService _gameplayService;
    private readonly IUpdateSubscriptionService _updateService;
    private readonly IPublisher<GameProgressEvent> _gameProgressPublisher;
    private readonly CoopBattleClock _clock;
    private readonly Dictionary<int, SlimeDataBase> _slimeData = new Dictionary<int, SlimeDataBase>();
    private readonly Dictionary<int, BaseSlime> _slimes = new Dictionary<int, BaseSlime>();

    private WaveSpawner _spawner;
    private StageData _stage;
    private bool _running;
    private bool _finished;
    private long _battleId;

    public bool IsCoopBattle => _coopBattleService.HasBattle;

    public CoopBattlePresenter(ICoopBattleService coopBattleService, IGameplayService gameplayService,
        IUpdateSubscriptionService updateService, IPublisher<GameProgressEvent> gameProgressPublisher)
    {
        _coopBattleService = coopBattleService;
        _gameplayService = gameplayService;
        _updateService = updateService;
        _gameProgressPublisher = gameProgressPublisher;
        _clock = new CoopBattleClock(coopBattleService);
    }

    public void Begin(StageData stage, WaveSpawner spawner)
    {
        _stage = stage;
        _spawner = spawner;
        _battleId = _coopBattleService.BattleId;
        CollectSlimeData(stage);

        _running = true;
        _updateService.RegisterUpdatable(this);
    }

    private void CollectSlimeData(StageData stage)
    {
        _slimeData.Clear();
        for (int wave = 1; wave <= stage.FinalWave; wave++)
        {
            foreach (var group in stage.GetWave(wave).Groups)
            {
                if (group.Slime != null)
                    _slimeData[group.Slime.SlimeId] = group.Slime;
            }
        }
    }

    public void ManagedUpdate()
    {
        if (!_running)
            return;

        _clock.Advance(Time.unscaledDeltaTime);

        while (_coopBattleService.TryPeekEvent(out var battleEvent) && battleEvent.Tick <= _clock.Tick)
        {
            _coopBattleService.DequeueEvent();
            Apply(battleEvent);
        }

        var end = _coopBattleService.End;
        if (end != null && !_coopBattleService.TryPeekEvent(out _) && _clock.Tick >= end.EndTick)
            Finish(end);
    }

    private void Apply(CoopBattleEvent battleEvent)
    {
        switch (battleEvent.KindCase)
        {
            case CoopBattleEvent.KindOneofCase.WaveStart:
                var wave = battleEvent.WaveStart;
                _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.WaveStarted, wave.Wave, wave.SlimeCount, wave.Wave == _stage.FinalWave));
                break;

            case CoopBattleEvent.KindOneofCase.SlimeSpawn:
                Spawn(battleEvent.SlimeSpawn, battleEvent.Tick);
                break;

            case CoopBattleEvent.KindOneofCase.SlimeArrival:
                var arrival = battleEvent.SlimeArrival;
                if (_slimes.TryGetValue(arrival.SlimeId, out var slime))
                {
                    _slimes.Remove(arrival.SlimeId);
                    slime.RemoveByServer();
                }
                _gameplayService.SetLife(arrival.Life);
                break;
        }
    }

    private void Spawn(CoopSlimeSpawn spawn, int tick)
    {
        if (!_slimeData.TryGetValue(spawn.SlimeDataId, out var data))
        {
            Debug.Log($"[CoopBattlePresenter] 스테이지에 없는 슬라임입니다: {spawn.SlimeDataId}");
            return;
        }

        // 골드는 협동에서 서버가 준다 (M1에는 처치가 없다)
        var slime = _spawner.SpawnSlime(data, spawn.Health, 0, spawn.PathIndex, spawn.SlimeId);
        if (slime == null)
            return;

        slime.FollowServerClock(_clock, tick);
        _slimes[spawn.SlimeId] = slime;
    }

    private void Finish(CoopBattleEndNtf end)
    {
        if (_finished)
            return;

        _finished = true;
        _running = false;
        _updateService.UnregisterUpdatable(this);

        _gameplayService.ReportReachedWave(end.ReachedWave);
        bool victory = end.Outcome == BattleOutcome.Victory;
        _gameProgressPublisher.Publish(victory
            ? new GameProgressEvent(GameProgressType.StageCleared, end.ReachedWave)
            : new GameProgressEvent(GameProgressType.GameOver));
    }

    public void Dispose()
    {
        if (_running)
            _updateService.UnregisterUpdatable(this);
        _running = false;
        _slimes.Clear();

        // 로비 버튼이나 씬 전환으로 나가면 서버에도 알린다 (끝난 전투면 상태만 정리한다).
        // 다시 하기는 새 매칭을 받은 뒤 씬을 다시 열므로, 그 새 전투는 건드리지 않는다
        if (_coopBattleService.HasBattle && _coopBattleService.BattleId == _battleId)
            _coopBattleService.LeaveAsync().Forget();
    }
}
