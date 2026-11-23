using System.Collections.Generic;
using UnityEngine;

public class TowerCells
{
    private readonly Dictionary<Vector2Int, ITowerInteractionHandler> _towers =
        new Dictionary<Vector2Int, ITowerInteractionHandler>();

    private readonly List<Vector2Int> _freeCellBuffer = new List<Vector2Int>();
    private readonly GridMapData _gridMapData;

    public GridMapData GridMapData => _gridMapData;
    public IReadOnlyDictionary<Vector2Int, ITowerInteractionHandler> Towers => _towers;

    public TowerCells(GridMapData gridMapData)
    {
        _gridMapData = gridMapData;

        if (_gridMapData == null)
        {
            Debug.Log("[TowerCells] GridMapData를 찾을 수 없습니다.");
        }
    }

    public bool TryGetTower(Vector2Int cell, out ITowerInteractionHandler tower)
    {
        return _towers.TryGetValue(cell, out tower);
    }

    public bool IsFree(Vector2Int cell)
    {
        if (_gridMapData == null || !_gridMapData.IsValidCoordinate(cell.x, cell.y))
            return false;

        if (_gridMapData.GetCellState(cell.x, cell.y) != GridCellState.Placeable)
            return false;

        return !_towers.ContainsKey(cell);
    }

    public bool TryGetRandomFreeCell(out Vector2Int cell)
    {
        cell = default;

        if (_gridMapData == null)
            return false;

        _freeCellBuffer.Clear();

        for (int y = 0; y < _gridMapData.Height; y++)
        {
            for (int x = 0; x < _gridMapData.Width; x++)
            {
                var candidate = new Vector2Int(x, y);
                if (IsFree(candidate))
                    _freeCellBuffer.Add(candidate);
            }
        }

        if (_freeCellBuffer.Count == 0)
            return false;

        cell = _freeCellBuffer[Random.Range(0, _freeCellBuffer.Count)];
        return true;
    }

    public void Register(Vector2Int cell, ITowerInteractionHandler tower)
    {
        if (tower == null)
            return;

        if (_gridMapData != null && !_gridMapData.IsValidCoordinate(cell.x, cell.y))
            return;

        _towers[cell] = tower;
    }

    public void Unregister(Vector2Int cell)
    {
        _towers.Remove(cell);
    }
}
