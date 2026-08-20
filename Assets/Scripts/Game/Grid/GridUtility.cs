using UnityEngine;

public static class GridUtility
{
    public static (int x, int y) WorldToGrid(Vector3 worldPos, GridMapData grid)
    {
        if (grid == null)
            return (-1, -1);

        Vector3 local = worldPos - grid.Origin;
        int x = Mathf.FloorToInt(local.x / grid.CellSize);
        int y = Mathf.FloorToInt(local.z / grid.CellSize);
        return (x, y);
    }

    public static Vector3 GridToWorld(int x, int y, GridMapData grid)
    {
        if (grid == null)
            return Vector3.zero;

        return grid.Origin + new Vector3(
            (x + 0.5f) * grid.CellSize,
            0f,
            (y + 0.5f) * grid.CellSize);
    }
}
