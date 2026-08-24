using UnityEngine;

public interface ITowerGridService
{
    GridMapData GridMapData { get; }

    bool TryGetTower(Vector2Int cell, out ITowerInteractionHandler tower);
    bool TryGetRandomFreeCell(out Vector2Int cell);

    void Register(Vector2Int cell, ITowerInteractionHandler tower);
    void Unregister(Vector2Int cell);
    void Move(Vector2Int from, Vector2Int to, ITowerInteractionHandler tower);
}
