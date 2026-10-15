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

        if (!TryResolveMergeResult(sourceTower.Data, out _))
            return false;

        if (!TryFindCell(source, out sourceCell))
            return false;

        int nearestDistance = int.MaxValue;

        foreach (var entry in _towerCells.Towers)
        {
            if (ReferenceEquals(entry.Value, source))
                continue;

            if (entry.Value is not BaseTower candidate || candidate == null)
                continue;

            if (candidate.Data == null || candidate.Data.Id != sourceTower.Data.Id)
                continue;

            int distance = (entry.Key - sourceCell).sqrMagnitude;
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            partnerCell = entry.Key;
            partner = entry.Value;
        }

        return partner != null;
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

    public bool TryResolveMergeResult(TowerData sourceData, out TowerData resultData)
    {
        resultData = null;

        if (_catalog == null || sourceData == null)
            return false;

        return _catalog.TryDrawNextGrade(sourceData, out resultData, _resultFilter);
    }
}
