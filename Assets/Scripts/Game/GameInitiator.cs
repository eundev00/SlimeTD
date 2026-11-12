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
    private readonly SlimeDepthBucketBinder _depthBucketBinder = new SlimeDepthBucketBinder();

    public GameInitiator(IGameplayService gameplayService, ITowerLevelService towerLevelService, ITowerPrefabProvider towerPrefabProvider, ISlimePrefabProvider slimePrefabProvider, IResourceLoadService resourceLoadService, Zone zone)
    {
        _gameplayService = gameplayService;
        _towerLevelService = towerLevelService;
        _towerPrefabProvider = towerPrefabProvider;
        _slimePrefabProvider = slimePrefabProvider;
        _resourceLoadService = resourceLoadService;
        _zone = zone;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _gameplayService.InitializeAsync();
        await _towerPrefabProvider.LoadAsync(_gameplayService.Catalog);
        await _slimePrefabProvider.LoadAsync(_gameplayService.Stage);

        var map = await CreateMapAsync(_gameplayService.Stage);
        if (map == null || map.Lanes.Count == 0)
            return;

        // 로비에서 레벨을 정하는 구조가 생기기 전까지 디버그 레벨을 기본값으로 쓴다.
        if (_gameplayService.Config != null)
            _towerLevelService.SetDefaultLevel(_gameplayService.Config.DebugTowerLevel);

        _depthBucketBinder.Bind(_gameplayService.DepthBucketSettings);
        _zone.Initialize(_gameplayService.Stage, map.Lanes[0]);
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
