using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;

public class TowerInputHandler : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float _maxRayDistance = 200f;
    // 모바일 최소 터치 타겟(1080 너비 기준 약 130px)의 1/8. 손가락 떨림과 의도한 이동을 가르는 값.
    [SerializeField] private float _dragThresholdPixels = 16f;

    private ITowerGridService _gridService;

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

    [Inject]
    public void Construct(ITowerGridService gridService)
    {
        _gridService = gridService;
    }

    private void Awake()
    {
        _towerLayer = LayerMask.GetMask(GameTags.TowerLayer);

        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _pressAction = new InputAction(binding: "<Pointer>/press", type: InputActionType.Button);
        _pressAction.performed += OnPressed;
        _pressAction.canceled += OnReleased;

        _pointerPositionAction = new InputAction(binding: "<Pointer>/position", type: InputActionType.Value);
        _pointerPositionAction.performed += OnPointerMoved;
    }

    private void Start()
    {
        if (_gridService == null)
        {
            Debug.LogError("[TowerInputHandler] ITowerGridService가 주입되지 않아 드래그 이동이 비활성화됩니다.", this);
            return;
        }

        if (_gridService.GridMapData == null)
        {
            Debug.LogError("[TowerInputHandler] GridMapData를 찾을 수 없어 드래그 이동이 비활성화됩니다.", this);
            return;
        }

        _gridMapData = _gridService.GridMapData;
        RegisterExistingTowers();
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

    private void RegisterExistingTowers()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var behaviour in behaviours)
        {
            if (behaviour is not ITowerInteractionHandler tower)
                continue;

            if (!TryGetCell(behaviour.transform.position, out var cell))
                continue;

            _gridService.Register(cell, tower);
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
        {
            Debug.LogError("[TowerInputHandler] _camera가 없어 클릭 위치를 계산할 수 없습니다.", this);
            return;
        }

        var pointer = Pointer.current;
        if (pointer == null)
            return;

        // 이 핸들러는 EventSystem을 거치지 않으므로 UI가 입력을 막아주지 않는다. 직접 확인한다.
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

        // 터치는 pointerId를 넘겨야 판정된다. 인자 없는 호출은 마우스만 본다.
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return EventSystem.current.IsPointerOverGameObject(touchId);
        }

        return EventSystem.current.IsPointerOverGameObject();
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

            // 그리드 밖 타워는 원래 칸이 없어 해제 대상도 없다. 새 칸 점유만 하면 된다.
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
            _lastSnappedPosition = GridUtility.GridToWorld(x, y, _gridMapData);
        }
        else
        {
            _lastDragValid = false;
            _lastSnappedPosition = ray.GetPoint(distance);
        }

        _selected.UpdateDragPosition(_lastSnappedPosition, _lastDragValid);
    }

    private bool IsPlaceable(int x, int y)
    {
        if (_gridMapData.GetCellState(x, y) != GridCellState.Placeable)
            return false;

        if (!_gridService.TryGetTower(new Vector2Int(x, y), out var occupant))
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
                _gridService.Move(_dragOriginCell, _dragTargetCell, _selected);
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
