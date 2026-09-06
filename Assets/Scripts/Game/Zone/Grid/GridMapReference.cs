using UnityEngine;

public class GridMapReference : MonoBehaviour
{
    [SerializeField] private GridMapData _gridMapData;
    [SerializeField] private bool _showGizmos = true;

    public GridMapData GridMapData => _gridMapData;

    private void OnDrawGizmos()
    {
        if (!_showGizmos || _gridMapData == null)
            return;

        Gizmos.color = Color.white;
        for (int y = 0; y < _gridMapData.Height; y++)
        {
            for (int x = 0; x < _gridMapData.Width; x++)
            {
                Vector3 cellCenter = GridUtility.GridToWorld(x, y, _gridMapData);

                GridCellState state = _gridMapData.GetCellState(x, y);
                switch (state)
                {
                    case GridCellState.Placeable:
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        break;
                    case GridCellState.Blocked:
                        Gizmos.color = new Color(1, 0, 0, 0.3f);
                        break;
                    default:
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                        break;
                }

                Gizmos.DrawCube(cellCenter, Vector3.one * 0.2f);

                Gizmos.color = Color.yellow;
                float halfSize = _gridMapData.CellSize * 0.5f;
                Vector3 p1 = cellCenter + new Vector3(-halfSize, 0, -halfSize);
                Vector3 p2 = cellCenter + new Vector3(halfSize, 0, -halfSize);
                Vector3 p3 = cellCenter + new Vector3(halfSize, 0, halfSize);
                Vector3 p4 = cellCenter + new Vector3(-halfSize, 0, halfSize);

                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p4);
                Gizmos.DrawLine(p4, p1);
            }
        }
    }
}
