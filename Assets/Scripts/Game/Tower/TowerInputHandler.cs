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

        if (TryGetEmptyCell(ray, out var cell))
            EmptyCellTapped?.Invoke(cell);
    }

    private bool TryGetEmptyCell(Ray ray, out Vector2Int cell)
    {
        cell = default;

        if (_gridMapData == null)
            return false;

        var plane = new Plane(Vector3.up, _gridMapData.Origin);
        if (!plane.Raycast(ray, out float distance))
            return false;

        (int x, int y) = GridUtility.WorldToGrid(ray.GetPoint(distance), _gridMapData);
        if (!_gridMapData.IsValidCoordinate(x, y))
            return false;

        if (_gridMapData.GetCellState(x, y) != GridCellState.Placeable)
            return false;

        cell = new Vector2Int(x, y);
        return !_towerCells.TryGetTower(cell, out _);
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
