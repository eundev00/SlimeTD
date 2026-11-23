using System;
using UnityEngine;

public class TowerMergeFinder
{
    private readonly TowerCells _towerCells;
    private readonly TowerCatalog _catalog;
    private readonly Func<TowerData, bool> _resultFilter;

    public TowerMergeFinder(TowerCells towerCells, TowerCatalog catalog, Func<TowerData, bool> resultFilter = null)
    {
        _towerCells = towerCells;
        _catalog = catalog;
        _resultFilter = resultFilter;

        if (_towerCells == null)
        {
            Debug.Log("[TowerMergeFinder] TowerCells가 없어 합성이 비활성화됩니다.");
        }

        if (_catalog == null)
        {
            Debug.Log("[TowerMergeFinder] TowerCatalog가 없어 합성이 비활성화됩니다.");
        }
    }

    public bool TryFindMergePartner(
        ITowerInteractionHandler source,
        out Vector2Int sourceCell,
        out Vector2Int partnerCell,
        out ITowerInteractionHandler partner)
    {
        sourceCell = default;
        partnerCell = default;
        partner = null;

        if (_towerCells == null || source == null)
            return false;

        if (source is not BaseTower sourceTower || sourceTower == null)
            return false;

        if (sourceTower.Data == null)
            return false;

        if (_catalog == null || !_catalog.HasNextGrade(sourceTower.Data, _resultFilter))
            return false;

        if (!TryFindCell(source, out sourceCell))
            return false;

        var grid = _towerCells.GridMapData;
        int maxRing = grid != null ? Mathf.Max(grid.Width, grid.Height) : 0;

        // 서버 검증과 같은 규칙: 가까운 고리부터, 고리 안에서는 바로 위 칸부터 시계 방향
        for (int ring = 1; ring <= maxRing; ring++)
        {
            int count = ring * 8;
            for (int i = 0; i < count; i++)
            {
                var cell = sourceCell + RingOffset(ring, i);
                if (!_towerCells.TryGetTower(cell, out var candidate))
                    continue;

                if (candidate is not BaseTower candidateTower || candidateTower == null)
                    continue;

                if (candidateTower.Data == null || candidateTower.Data.Id != sourceTower.Data.Id)
                    continue;

                partnerCell = cell;
                partner = candidate;
                return true;
            }
        }

        return false;
    }

    // (0, r)에서 시작해 오른쪽 위 → 오른쪽 아래 → 왼쪽 아래 → 왼쪽 위 → (-1, r)로 끝난다
    private static Vector2Int RingOffset(int r, int index)
    {
        if (index <= r)
            return new Vector2Int(index, r);

        index -= r;
        if (index <= 2 * r)
            return new Vector2Int(r, r - index);

        index -= 2 * r;
        if (index <= 2 * r)
            return new Vector2Int(r - index, -r);

        index -= 2 * r;
        if (index <= 2 * r)
            return new Vector2Int(-r, -r + index);

        index -= 2 * r;
        return new Vector2Int(-r + index, r);
    }

    private bool TryFindCell(ITowerInteractionHandler tower, out Vector2Int cell)
    {
        cell = default;

        foreach (var entry in _towerCells.Towers)
        {
            if (!ReferenceEquals(entry.Value, tower))
                continue;

            cell = entry.Key;
            return true;
        }

        return false;
    }

    public bool TryResolveMergeResult(TowerData sourceData, out TowerData resultData, SlimeTD.Battle.BattleRandom random = null)
    {
        resultData = null;

        if (_catalog == null || sourceData == null)
            return false;

        return _catalog.TryDrawNextGrade(sourceData, out resultData, _resultFilter, random);
    }
}
