using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerInputHandler : MonoBehaviour
{
    public event Action<ITowerInteractionHandler> SelectionChanged;
    public event Action<Vector2Int> EmptyCellTapped;

    [SerializeField] private float _maxRayDistance = 200f;

    private Camera _camera;
    private TowerCells _towerCells;
    private GridMapData _gridMapData;

    private InputAction _pressAction;
    private LayerMask _towerLayer;
    private ITowerInteractionHandler _selected;
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

    public void Initialize(TowerCells towerCells)
    {
        if (towerCells == null || towerCells.GridMapData == null)
        {
            Debug.Log("[TowerInputHandler] 배치 그리드가 없어 빈 칸 소환이 비활성화됩니다.", this);
            return;
        }

        _towerCells = towerCells;
        _gridMapData = towerCells.GridMapData;
    }

    private void Awake()
    {
        _towerLayer = LayerMask.GetMask(GameTags.TowerLayer);

        _pressAction = new InputAction(type: InputActionType.Button);
        _pressAction.AddBinding("<Mouse>/leftButton");
        _pressAction.AddBinding("<Touchscreen>/primaryTouch/press");
        _pressAction.performed += OnPressed;
        _pressAction.Enable();
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void OnEnable()
    {
        _pressAction?.Enable();
    }

    private void OnDisable()
    {
        _pressAction?.Disable();
    }

    private void OnDestroy()
    {
        if (_pressAction != null)
        {
            _pressAction.performed -= OnPressed;
            _pressAction.Dispose();
            _pressAction = null;
        }

        SelectionChanged = null;
        EmptyCellTapped = null;
    }

    private void OnPressed(InputAction.CallbackContext context)
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
            return;

        var pointer = Pointer.current;
        if (pointer == null)
            return;

        Vector2 screenPosition = pointer.position.ReadValue();
        if (IsPointerOverUI(screenPosition))
            return;

        var ray = _camera.ScreenPointToRay(screenPosition);

        // 타워 몸통이 윗칸까지 덮어서, 그리드 위에서는 콜라이더보다 바닥 칸으로 정한다
        if (TryGetGridCell(ray, out var tappedCell))
        {
            if (_towerCells.TryGetTower(tappedCell, out var cellTower))
            {
                SelectTower(cellTower);
                return;
            }

            ClearSelection();
            if (_gridMapData.GetCellState(tappedCell.x, tappedCell.y) == GridCellState.Placeable)
                EmptyCellTapped?.Invoke(tappedCell);
            return;
        }

        if (Physics.Raycast(ray, out var hit, _maxRayDistance, _towerLayer))
        {
            var tower = hit.collider.GetComponentInParent<ITowerInteractionHandler>();
            if (tower != null)
            {
                SelectTower(tower);
                return;
            }
        }

        ClearSelection();
    }

    private bool TryGetGridCell(Ray ray, out Vector2Int cell)
    {
        cell = default;

        if (_gridMapData == null || _towerCells == null)
            return false;

        var plane = new Plane(Vector3.up, _gridMapData.Origin);
        if (!plane.Raycast(ray, out float distance))
            return false;

        (int x, int y) = GridUtility.WorldToGrid(ray.GetPoint(distance), _gridMapData);
        if (!_gridMapData.IsValidCoordinate(x, y))
            return false;

        cell = new Vector2Int(x, y);
        return true;
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
        return _uiRaycastResults.Count > 0;
    }

    public void ClearSelectedTower()
    {
        ClearSelection();
    }

    private void SelectTower(ITowerInteractionHandler tower)
    {
        if (_selected == tower)
            return;

        Deselect();

        _selected = tower;
        _selected.Select();

        SelectionChanged?.Invoke(_selected);
    }

    private void ClearSelection()
    {
        if (_selected == null)
            return;

        Deselect();

        SelectionChanged?.Invoke(null);
    }

    private void Deselect()
    {
        if (_selected == null)
            return;

        // 인터페이스 참조는 파괴된 오브젝트를 null로 보지 않으므로 MonoBehaviour로 확인한다.
        if (_selected is MonoBehaviour behaviour && behaviour != null)
        {
            _selected.Deselect();
        }

        _selected = null;
    }
}
