using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.UpdateService;
using SlimeTD.Protocol;
using UnityEngine;

// 협동 화면: 서버 이벤트를 그 틱이 되면 적용한다 (웨이브 표시, 슬라임 등장·도착, 라이프, 헌터 소환·합성, 골드, 끝)
public class CoopBattlePresenter : IUpdatable, IDisposable
{
    private readonly ICoopBattleService _coopBattleService;
    private readonly IGameplayService _gameplayService;
    private readonly IUpdateSubscriptionService _updateService;
    private readonly IPublisher<GameProgressEvent> _gameProgressPublisher;
    private readonly CoopBattleClock _clock;
    private readonly Dictionary<int, SlimeDataBase> _slimeData = new Dictionary<int, SlimeDataBase>();
    private readonly Dictionary<int, BaseSlime> _slimes = new Dictionary<int, BaseSlime>();

    private readonly Dictionary<int, (int Slot, Vector2Int Cell)> _hunters = new Dictionary<int, (int, Vector2Int)>();
    private readonly Dictionary<int, TowerPlacer> _teammateBoards = new Dictionary<int, TowerPlacer>();

    private WaveSpawner _spawner;
    private Zone _myZone;
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

    public void Begin(StageData stage, WaveSpawner spawner, Zone myZone, GameMap map)
    {
        _stage = stage;
        _spawner = spawner;
        _myZone = myZone;
        _battleId = _coopBattleService.BattleId;
        CollectSlimeData(stage);
        CreateTeammateBoards(map);

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

    private void CreateTeammateBoards(GameMap map)
    {
        foreach (var player in _coopBattleService.Players)
        {
            if (player.Slot == _coopBattleService.MySlot)
                continue;

            var board = _myZone.CreateViewOnlyPlacer(map.GetZone(player.Slot));
            if (board == null)
            {
                Debug.Log($"[CoopBattlePresenter] 맵에 팀원 구역(slot {player.Slot})이 없습니다.", map);
                continue;
            }

            _teammateBoards[player.Slot] = board;
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

            case CoopBattleEvent.KindOneofCase.HunterSummon:
                var summon = battleEvent.HunterSummon;
                PlaceHunter(summon.Slot, summon.HunterId, summon.TowerId, summon.Level, new Vector2Int(summon.X, summon.Y));
                break;

            case CoopBattleEvent.KindOneofCase.HunterMerge:
                var merge = battleEvent.HunterMerge;
                foreach (int removedId in merge.RemovedHunterIds)
                    RemoveHunter(removedId);
                PlaceHunter(merge.Slot, merge.HunterId, merge.TowerId, merge.Level, new Vector2Int(merge.X, merge.Y));
                break;

            case CoopBattleEvent.KindOneofCase.GoldChange:
                var gold = battleEvent.GoldChange;
                if (gold.Slot == _coopBattleService.MySlot)
                    _gameplayService.SetGold(gold.Gold, gold.SummonCost);
                break;
        }
    }

    private void PlaceHunter(int slot, int hunterId, int towerId, int level, Vector2Int cell)
    {
        if (_gameplayService.Catalog == null || !_gameplayService.Catalog.TryGetById(towerId, out var towerData))
        {
            Debug.Log($"[CoopBattlePresenter] 카탈로그에 없는 헌터입니다: {towerId}");
            return;
        }

        bool placed = slot == _coopBattleService.MySlot
            ? _myZone.PlaceFromServer(towerData, cell, level)
            : _teammateBoards.TryGetValue(slot, out var board) && board.TryPlace(towerData, cell, level, out _);

        if (placed)
            _hunters[hunterId] = (slot, cell);
    }

    private void RemoveHunter(int hunterId)
    {
        if (!_hunters.TryGetValue(hunterId, out var hunter))
            return;

        _hunters.Remove(hunterId);
        if (hunter.Slot == _coopBattleService.MySlot)
            _myZone.RemoveFromServer(hunter.Cell);
        else if (_teammateBoards.TryGetValue(hunter.Slot, out var board))
            board.Remove(hunter.Cell);
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
        _hunters.Clear();

        foreach (var board in _teammateBoards.Values)
            board.Dispose();
        _teammateBoards.Clear();

        // 로비 버튼이나 씬 전환으로 나가면 서버에도 알린다 (끝난 전투면 상태만 정리한다).
        // 다시 하기는 새 매칭을 받은 뒤 씬을 다시 열므로, 그 새 전투는 건드리지 않는다
        if (_coopBattleService.HasBattle && _coopBattleService.BattleId == _battleId)
            _coopBattleService.LeaveAsync().Forget();
    }
}
