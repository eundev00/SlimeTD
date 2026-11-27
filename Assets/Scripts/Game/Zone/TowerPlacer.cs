using System;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

// 구역 그리드 칸에 타워를 만들고 지운다. 비용·뽑기·기록은 부르는 쪽이 한다
public class TowerPlacer : IDisposable
{
    private readonly IObjectResolver _resolver;
    private readonly IGroundHeightSampler _groundHeightSampler;
    private readonly ITowerPrefabProvider _prefabProvider;
    private readonly TowerCells _towerCells;
    private readonly Transform _spawnRoot;
    private readonly bool _localAttack;

    private Transform _holder;

    public TowerCells Cells => _towerCells;

    public TowerPlacer(IObjectResolver resolver, IGroundHeightSampler groundHeightSampler, ITowerPrefabProvider prefabProvider,
        TowerCells towerCells, Transform spawnRoot, bool localAttack)
    {
        _resolver = resolver;
        _groundHeightSampler = groundHeightSampler;
        _prefabProvider = prefabProvider;
        _towerCells = towerCells;
        _spawnRoot = spawnRoot;
        _localAttack = localAttack;
    }

    public bool HasPrefab(TowerData towerData)
    {
        return towerData != null && _prefabProvider.TryGet(towerData, out _);
    }

    public bool TryPlace(TowerData towerData, Vector2Int cell, int level, out BaseTower tower)
    {
        tower = null;

        var gridMapData = _towerCells?.GridMapData;
        if (gridMapData == null || !_prefabProvider.TryGet(towerData, out var prefab))
        {
            Debug.Log($"[TowerPlacer] 타워를 놓지 못했습니다: {towerData?.CodeName}");
            return false;
        }

        var position = _groundHeightSampler.SnapToGround(GridUtility.GridToWorld(cell.x, cell.y, gridMapData));
        var instance = TowerFactory.Create(_resolver, prefab, towerData, GetHolder(), _spawnRoot, position, level);

        tower = instance.GetComponent<BaseTower>();
        if (tower == null)
        {
            Debug.Log("[TowerPlacer] 타워 프리팹에 BaseTower가 없습니다.", instance);
            Object.Destroy(instance);
            return false;
        }

        if (!_localAttack)
            tower.DisableLocalAttack();

        tower.Cell = cell;
        _towerCells.Register(cell, tower);
        return true;
    }

    public void Remove(Vector2Int cell)
    {
        if (_towerCells == null || !_towerCells.TryGetTower(cell, out var tower))
            return;

        _towerCells.Unregister(cell);
        if (tower is MonoBehaviour behaviour && behaviour != null)
            Object.Destroy(behaviour.gameObject);
    }

    private Transform GetHolder()
    {
        if (_holder != null)
            return _holder;

        var holderObject = new GameObject("[TowerSpawnHolder]");
        holderObject.SetActive(false);

        _holder = holderObject.transform;
        return _holder;
    }

    public void Dispose()
    {
        if (_holder != null)
            Object.Destroy(_holder.gameObject);
        _holder = null;
    }
}
