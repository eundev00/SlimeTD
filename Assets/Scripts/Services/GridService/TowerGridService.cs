using System.Collections.Generic;
using UnityEngine;

public class TowerGridService : ITowerGridService
{
    private readonly Dictionary<Vector2Int, ITowerInteractionHandler> _towers =
        new Dictionary<Vector2Int, ITowerInteractionHandler>();

    private readonly List<Vector2Int> _freeCellBuffer = new List<Vector2Int>();
    private readonly GridMapData _gridMapData;

    public GridMapData GridMapData => _gridMapData;

    public TowerGridService(GridMapReference gridMapReference)
    {
        _gridMapData = gridMapReference != null ? gridMapReference.GridMapData : null;

        if (_gridMapData == null)
        {
            Debug.LogError("[TowerGridService] GridMapData를 찾을 수 없습니다.");
        }
    }

    public bool TryGetTower(Vector2Int cell, out ITowerInteractionHandler tower)
    {
        return _towers.TryGetValue(cell, out tower);
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
                if (_gridMapData.GetCellState(x, y) != GridCellState.Placeable)
                    continue;

                var candidate = new Vector2Int(x, y);
                if (_towers.ContainsKey(candidate))
                    continue;

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

    public void Move(Vector2Int from, Vector2Int to, ITowerInteractionHandler tower)
    {
        _towers.Remove(from);
        Register(to, tower);
    }
}
