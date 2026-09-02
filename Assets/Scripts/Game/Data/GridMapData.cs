using UnityEngine;

[CreateAssetMenu(fileName = "GridMapData", menuName = "SlimeTD/Grid Map Data", order = 1)]
public class GridMapData : ScriptableObject
{
    private const float FixedCellSize = 1f;

    [SerializeField] private Vector3 _centerPosition = Vector3.zero;
    [SerializeField] private Vector3 _origin;
    [SerializeField] private int _width = 8;
    [SerializeField] private int _height = 15;
    [SerializeField, HideInInspector] private GridCellState[] _cellStates;
    [SerializeField, HideInInspector] private int _cellStatesWidth;
    [SerializeField, HideInInspector] private int _cellStatesHeight;

    public Vector3 CenterPosition => _centerPosition;
    public Vector3 Origin => _origin;
    public float CellSize => FixedCellSize;
    public int Width => _width;
    public int Height => _height;

    public void RecalculateOriginFromCenter()
    {
        _origin = new Vector3(
            _centerPosition.x - _width * FixedCellSize * 0.5f,
            _centerPosition.y,
            _centerPosition.z - _height * FixedCellSize * 0.5f);
    }

    public void SetCenterPosition(Vector3 centerPosition)
    {
        _centerPosition = centerPosition;
        RecalculateOriginFromCenter();
    }

    public bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public GridCellState GetCellState(int x, int y)
    {
        if (!IsValidCoordinate(x, y))
        {
            return GridCellState.Blocked;
        }

        EnsureCellStates();
        return _cellStates[y * _width + x];
    }

    public void SetCellState(int x, int y, GridCellState state)
    {
        if (!IsValidCoordinate(x, y))
        {
            return;
        }

        EnsureCellStates();
        _cellStates[y * _width + x] = state;
    }

    private void OnEnable()
    {
        Revalidate();
    }

    private void OnValidate()
    {
        Revalidate();
    }

    private void Revalidate()
    {
        _width = Mathf.Max(1, _width);
        _height = Mathf.Max(1, _height);

        RecalculateOriginFromCenter();
        EnsureCellStates();
    }

    private void EnsureCellStates()
    {
        if (_cellStates != null
            && _cellStatesWidth == _width
            && _cellStatesHeight == _height
            && _cellStates.Length == _width * _height)
        {
            return;
        }

        ResizePreservingCells();
    }

    private void ResizePreservingCells()
    {
        GridCellState[] resized = new GridCellState[_width * _height];

        if (_cellStates != null
            && _cellStatesWidth > 0
            && _cellStatesHeight > 0
            && _cellStates.Length == _cellStatesWidth * _cellStatesHeight)
        {
            int copyWidth = Mathf.Min(_cellStatesWidth, _width);
            int copyHeight = Mathf.Min(_cellStatesHeight, _height);

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    resized[y * _width + x] = _cellStates[y * _cellStatesWidth + x];
                }
            }
        }

        _cellStates = resized;
        _cellStatesWidth = _width;
        _cellStatesHeight = _height;
    }
}
