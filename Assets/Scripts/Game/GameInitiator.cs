using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Object = UnityEngine.Object;

public class GameInitiator : IAsyncStartable, IDisposable
{
    private readonly IGameplayService _gameplayService;
    private readonly ITowerLevelService _towerLevelService;
    private readonly ITowerPrefabProvider _towerPrefabProvider;
    private readonly ISlimePrefabProvider _slimePrefabProvider;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly Zone _zone;
    private readonly WaveSpawner _waveSpawner;
    private readonly GameSceneSettings _sceneSettings;
    private readonly ISingleBattleService _singleBattleService;
    private readonly SlimeDepthBucketBinder _depthBucketBinder = new SlimeDepthBucketBinder();

    public GameInitiator(IGameplayService gameplayService, ITowerLevelService towerLevelService, ITowerPrefabProvider towerPrefabProvider, ISlimePrefabProvider slimePrefabProvider, IResourceLoadService resourceLoadService, Zone zone, WaveSpawner waveSpawner, GameSceneSettings sceneSettings, ISingleBattleService singleBattleService)
    {
        _gameplayService = gameplayService;
        _towerLevelService = towerLevelService;
        _towerPrefabProvider = towerPrefabProvider;
        _slimePrefabProvider = slimePrefabProvider;
        _resourceLoadService = resourceLoadService;
        _zone = zone;
        _waveSpawner = waveSpawner;
        _sceneSettings = sceneSettings;
        _singleBattleService = singleBattleService;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        var battleStage = _singleBattleService.HasBattle ? _singleBattleService.Stage : null;
        string stageKey = battleStage != null ? "Stage_" + battleStage.CodeName : _sceneSettings.StageKey;
        await _gameplayService.InitializeAsync(stageKey);
        if (battleStage != null)
            WarnStageDifference(_gameplayService.Stage, battleStage);
        await _towerPrefabProvider.LoadAsync(_gameplayService.Catalog);
        await _slimePrefabProvider.LoadAsync(_gameplayService.Stage);

        var map = await CreateMapAsync(_gameplayService.Stage);
        if (map == null)
            return;

        // TODO: 팀원 구역(slot 1)은 서버가 붙기 전까지 Zone 없이 비워 둔다
        var myZone = map.GetZone(0);
        if (myZone == null || map.Paths.Count == 0)
        {
            Debug.Log("[GameInitiator] 맵에 내 배치 구역(slot 0) 또는 경로가 없습니다.", map);
            return;
        }

        // 로비에서 레벨을 정하는 구조가 생기기 전까지 디버그 레벨을 기본값으로 쓴다.
        if (_gameplayService.Config != null)
            _towerLevelService.SetDefaultLevel(_gameplayService.Config.DebugTowerLevel);

        _depthBucketBinder.Bind(_gameplayService.DepthBucketSettings);
        _zone.Initialize(myZone);
        _waveSpawner.Initialize(_gameplayService.Stage, map.Paths);
    }

    // 클라이언트 에셋과 서버 시트 값이 다르면 서버 검증 결과가 어긋난다
    private static void WarnStageDifference(StageData local, SlimeTD.Protocol.BattleStage server)
    {
        if (local == null)
            return;

        if (local.StageId != server.StageId || local.MapKey != server.MapKey || local.FinalWave != server.FinalWave
            || local.StartingLife != server.StartingLife || local.StartingGold != server.StartingGold
            || !Mathf.Approximately(local.SpawnInterval, server.SpawnInterval) || !Mathf.Approximately(local.InterWaveDelay, server.InterWaveDelay))
        {
            Debug.LogWarning($"[GameInitiator] 스테이지 값이 서버와 다릅니다. 클라 {local.StageId}/{local.MapKey}/웨이브 {local.FinalWave}/라이프 {local.StartingLife}/골드 {local.StartingGold}/간격 {local.SpawnInterval}/대기 {local.InterWaveDelay}, " +
                $"서버 {server.StageId}/{server.MapKey}/웨이브 {server.FinalWave}/라이프 {server.StartingLife}/골드 {server.StartingGold}/간격 {server.SpawnInterval}/대기 {server.InterWaveDelay}");
        }
    }

    private async UniTask<GameMap> CreateMapAsync(StageData stage)
    {
        if (stage == null || string.IsNullOrEmpty(stage.MapKey))
        {
            Debug.Log("[GameInitiator] 스테이지에 맵 이름(mapKey)이 없습니다.");
            return null;
        }

        var prefab = await _resourceLoadService.LoadAsync<GameObject>(stage.MapKey);
        if (prefab == null)
        {
            Debug.Log($"[GameInitiator] 맵 프리팹을 찾지 못했습니다: {stage.MapKey}");
            return null;
        }

        var instance = Object.Instantiate(prefab);
        // 활성 씬이 Bootstrap이면 그쪽에 생겨 게임 씬을 내려도 남는다
        SceneManager.MoveGameObjectToScene(instance, _zone.gameObject.scene);

        var map = instance.GetComponent<GameMap>();
        if (map == null)
            Debug.Log($"[GameInitiator] 맵 프리팹에 GameMap이 없습니다: {stage.MapKey}", instance);

        return map;
    }

    public void Dispose()
    {
        _depthBucketBinder.Dispose();
    }
}
