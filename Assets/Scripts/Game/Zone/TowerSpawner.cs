using UnityEngine;
using VContainer;

public class TowerSpawner : MonoBehaviour
{
    private const int DefaultSummonGradeId = 1;

    private IGameplayService _gameplayService;
    private IAccountService _accountService;
    private ISingleBattleService _singleBattleService;
    private TowerCells _towerCells;
    private TowerPlacer _placer;

    [Inject]
    public void Construct(
        IGameplayService gameplayService,
        IAccountService accountService,
        ISingleBattleService singleBattleService)
    {
        _singleBattleService = singleBattleService;
        _gameplayService = gameplayService;
        _accountService = accountService;
    }

    public void Initialize(TowerPlacer placer)
    {
        _placer = placer;
        _towerCells = placer?.Cells;
    }

    public bool TrySpawn(TowerData towerData = null, Vector2Int? targetCell = null, int level = 0)
    {
        if (_placer == null || _towerCells?.GridMapData == null)
        {
            Debug.Log("[TowerSpawner] Initialize가 호출되지 않았습니다.", this);
            return false;
        }

        Vector2Int cell;
        if (targetCell.HasValue)
        {
            cell = targetCell.Value;
            if (!_towerCells.IsFree(cell))
                return false;
        }
        else if (!_towerCells.TryGetRandomFreeCell(out cell))
        {
            Debug.Log("[TowerSpawner] 배치 가능한 빈 칸이 없습니다.", this);
            return false;
        }

        if (!CanAffordSummon())
        {
            Debug.Log($"[TowerSpawner] 골드가 부족합니다. 필요: {_gameplayService.Info.SummonCost.Value}, 보유: {_gameplayService.Info.Gold.Value}", this);
            return false;
        }

        // 서버 검증: 소환이 실제로 성공할 때만 시드 목록에서 뽑는다. 뒤에서 실패하면 뽑기 전 위치로 되돌린다
        var random = _singleBattleService.SummonRandom;
        ulong savedRandom = random != null ? random.Save() : 0;
        if (towerData == null && !TryDrawTower(out towerData, random))
            return false;

        if (!_placer.HasPrefab(towerData) || !_gameplayService.TrySpendSummonCost())
        {
            Debug.Log($"[TowerSpawner] 소환하지 못했습니다: {towerData.CodeName}", this);
            random?.Restore(savedRandom);
            return false;
        }

        if (!_placer.TryPlace(towerData, cell, level, out _))
            return false;

        _singleBattleService.RecordSummon(cell, towerData.Id);
        return true;
    }

    public bool TryMerge(TowerData resultData, Vector2Int resultCell, Vector2Int partnerCell)
    {
        if (_placer == null || !_placer.HasPrefab(resultData))
        {
            Debug.Log("[TowerSpawner] 합성 결과 타워에 프리팹이 없습니다.", this);
            return false;
        }

        _placer.Remove(resultCell);
        _placer.Remove(partnerCell);

        if (!_placer.TryPlace(resultData, resultCell, 0, out _))
            return false;

        _singleBattleService.RecordMerge(resultCell, partnerCell, resultData.Id);
        return true;
    }

    private bool CanAffordSummon()
    {
        var config = _gameplayService.Config;
        if (config == null)
            return false;

        int cost = _gameplayService.Info.SummonCost.Value;
        return config.IgnoreGoldCost || cost <= 0 || _gameplayService.Info.Gold.Value >= cost;
    }

    private bool TryDrawTower(out TowerData towerData, SlimeTD.Battle.BattleRandom random)
    {
        towerData = null;

        var catalog = _gameplayService?.Catalog;
        if (catalog == null)
        {
            Debug.Log("[TowerSpawner] TowerCatalog가 없어 타워를 뽑을 수 없습니다.", this);
            return false;
        }

        var config = _gameplayService.Config;
        int gradeId = config != null ? config.SummonGradeId : DefaultSummonGradeId;

        if (!catalog.TryDraw(gradeId, out towerData, IsAvailable, random))
        {
            Debug.Log($"[TowerSpawner] 등급 {gradeId}에서 뽑을 타워가 없습니다.", this);
            return false;
        }

        return true;
    }

    private bool IsAvailable(TowerData towerData)
    {
        return _accountService == null || _accountService.IsAvailable(towerData.Id);
    }
}
