using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerInputHandler : MonoBehaviour
{
    [SerializeField] private float _maxRayDistance = 200f;
    // 모바일 최소 터치 타겟(1080 너비 기준 약 130px)의 1/8. 손가락 떨림과 의도한 이동을 가르는 값.
    [SerializeField] private float _dragThresholdPixels = 16f;

    private Camera _camera;
    private TowerCells _towerCells;
    private IGroundHeightSampler _groundHeightSampler;

    private InputAction _pressAction;
    private InputAction _pointerPositionAction;
    private LayerMask _towerLayer;
    private ITowerInteractionHandler _selected;

    private GridMapData _gridMapData;
    private Vector2 _pressScreenPosition;
    private bool _dragging;
    private bool _lastDragValid;
    private Vector3 _lastSnappedPosition;
    private Vector2Int _dragOriginCell;
    private Vector2Int _dragTargetCell;

    public void Initialize(TowerCells towerCells, IGroundHeightSampler groundHeightSampler)
    {
        if (towerCells == null)
        {
            Debug.Log("[TowerInputHandler] TowerCells가 없어 드래그 이동이 비활성화됩니다.", this);
            return;
        }

        if (towerCells.GridMapData == null)
        {
            Debug.Log("[TowerInputHandler] GridMapData를 찾을 수 없어 드래그 이동이 비활성화됩니다.", this);
            return;
        }

        _towerCells = towerCells;
        _gridMapData = towerCells.GridMapData;
        _groundHeightSampler = groundHeightSampler;
    }

    private void Awake()
    {
        _towerLayer = LayerMask.GetMask(GameTags.TowerLayer);

        _pressAction = new InputAction(type: InputActionType.Button);
        _pressAction.AddBinding("<Mouse>/leftButton");
        _pressAction.AddBinding("<Touchscreen>/primaryTouch/press");
        _pressAction.performed += OnPressed;
        _pressAction.canceled += OnReleased;

        _pointerPositionAction = new InputAction(type: InputActionType.Value);
        _pointerPositionAction.AddBinding("<Mouse>/position");
        _pointerPositionAction.AddBinding("<Touchscreen>/primaryTouch/position");
        _pointerPositionAction.performed += OnPointerMoved;

        _pressAction.Enable();
        _pointerPositionAction.Enable();
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void OnEnable()
    {
        _pressAction?.Enable();
        _pointerPositionAction?.Enable();
    }

    private void OnDisable()
    {
        _pressAction?.Disable();
        _pointerPositionAction?.Disable();
    }

    private void OnDestroy()
    {
        if (_pressAction != null)
        {
            _pressAction.performed -= OnPressed;
            _pressAction.canceled -= OnReleased;
            _pressAction.Dispose();
            _pressAction = null;
        }

        if (_pointerPositionAction != null)
        {
            _pointerPositionAction.performed -= OnPointerMoved;
            _pointerPositionAction.Dispose();
            _pointerPositionAction = null;
        }
    }

    private bool TryGetCell(Vector3 worldPosition, out Vector2Int cell)
    {
        cell = default;

        (int x, int y) = GridUtility.WorldToGrid(worldPosition, _gridMapData);
        if (!_gridMapData.IsValidCoordinate(x, y))
            return false;

        cell = new Vector2Int(x, y);
        return true;
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

        if (IsPointerOverUI())
            return;

        Vector2 screenPosition = pointer.position.ReadValue();
        var ray = _camera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out var hit, _maxRayDistance, _towerLayer))
            return;

        var tower = hit.collider.GetComponentInParent<ITowerInteractionHandler>();
        if (tower == null)
            return;

        _pressScreenPosition = screenPosition;
        _dragging = false;
        SelectTower(tower);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        var pointer = Pointer.current;
        if (pointer == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointer.position.ReadValue()
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }

    private void OnPointerMoved(InputAction.CallbackContext context)
    {
        if (_selected == null || _gridMapData == null || _camera == null)
            return;

        Vector2 screenPosition = context.ReadValue<Vector2>();

        if (!_dragging)
        {
            float threshold = _dragThresholdPixels * _dragThresholdPixels;
            if ((screenPosition - _pressScreenPosition).sqrMagnitude < threshold)
                return;

            if (!TryGetCell(((MonoBehaviour)_selected).transform.position, out _dragOriginCell))
            {
                _dragOriginCell = new Vector2Int(int.MinValue, int.MinValue);
            }

            _dragging = true;
            _dragTargetCell = _dragOriginCell;
            _lastDragValid = false;
            _lastSnappedPosition = ((MonoBehaviour)_selected).transform.position;
            _selected.BeginDrag();
        }

        var ray = _camera.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.up, _gridMapData.Origin);

        if (!plane.Raycast(ray, out float distance))
            return;

        (int x, int y) = GridUtility.WorldToGrid(ray.GetPoint(distance), _gridMapData);

        if (_gridMapData.IsValidCoordinate(x, y))
        {
            _dragTargetCell = new Vector2Int(x, y);
            _lastDragValid = IsPlaceable(x, y);
            _lastSnappedPosition = SnapToGround(GridUtility.GridToWorld(x, y, _gridMapData));
        }
        else
        {
            _lastDragValid = false;
            _lastSnappedPosition = SnapToGround(ray.GetPoint(distance));
        }

        _selected.UpdateDragPosition(_lastSnappedPosition, _lastDragValid);
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        return _groundHeightSampler != null ? _groundHeightSampler.SnapToGround(position) : position;
    }

    private bool IsPlaceable(int x, int y)
    {
        if (_gridMapData.GetCellState(x, y) != GridCellState.Placeable)
            return false;

        if (!_towerCells.TryGetTower(new Vector2Int(x, y), out var occupant))
            return true;

        return ReferenceEquals(occupant, _selected);
    }

    private void OnReleased(InputAction.CallbackContext context)
    {
        if (_dragging && _selected != null)
        {
            // 터치는 떼는 순간 사라져 포인터 위치를 다시 읽을 수 없으므로 마지막 판정을 쓴다.
            if (_lastDragValid)
            {
                _towerCells.Move(_dragOriginCell, _dragTargetCell, _selected);
                _selected.EndDrag(_lastSnappedPosition);
            }
            else
            {
                _selected.CancelDrag();
            }
        }

        _dragging = false;
        ClearSelection();
    }

    private void SelectTower(ITowerInteractionHandler tower)
    {
        if (_selected == tower)
            return;

        ClearSelection();

        _selected = tower;
        _selected.Select();
    }

    private void ClearSelection()
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
