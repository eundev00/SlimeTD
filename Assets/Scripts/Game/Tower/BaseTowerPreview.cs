using UniRx;
using UnityEngine;

public class BaseTowerPreview : MonoBehaviour, ITowerInteractionHandler
{
    [SerializeField] private TowerRangeIndicator _rangeIndicator;
    [SerializeField] private float _previewRange = 5f;
    [SerializeField] private Transform _towerBody;
    [SerializeField] private float _liftHeight = 0.35f;

    private readonly ReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<bool> _isDragging = new ReactiveProperty<bool>(false);

    private Vector3 _originPosition;
    private Vector3 _towerBodyLocalPosition;

    public IReadOnlyReactiveProperty<bool> IsSelected => _isSelected;
    public IReadOnlyReactiveProperty<bool> IsDragging => _isDragging;

    private void Start()
    {
        if (_towerBody != null)
        {
            _towerBodyLocalPosition = _towerBody.localPosition;
        }

        if (_rangeIndicator == null)
        {
            _rangeIndicator = GetComponentInChildren<TowerRangeIndicator>();
        }

        if (_rangeIndicator == null)
        {
            Debug.Log("[BaseTowerPreview] TowerRangeIndicator를 찾을 수 없습니다.", this);
            return;
        }

        _rangeIndicator.UpdateRangeVisual(_previewRange);
        ApplySelection();
    }

    private void OnDestroy()
    {
        _isSelected.Dispose();
        _isDragging.Dispose();
    }

    public void Select()
    {
        if (_isSelected.Value)
            return;

        _isSelected.Value = true;
        ApplySelection();
    }

    public void Deselect()
    {
        if (!_isSelected.Value)
            return;

        _isSelected.Value = false;
        ApplySelection();
    }

    public void BeginDrag()
    {
        if (_isDragging.Value)
            return;

        _originPosition = transform.position;
        _isDragging.Value = true;
    }

    public void UpdateDragPosition(Vector3 worldPosition, bool isValid)
    {
        if (!_isDragging.Value)
            return;

        transform.position = worldPosition;

        if (_rangeIndicator != null)
        {
            _rangeIndicator.SetValid(isValid);
        }
    }

    public void EndDrag(Vector3 snappedWorldPosition)
    {
        if (!_isDragging.Value)
            return;

        transform.position = snappedWorldPosition;
        FinishDrag();
    }

    public void CancelDrag()
    {
        if (!_isDragging.Value)
            return;

        transform.position = _originPosition;
        FinishDrag();
    }

    private void FinishDrag()
    {
        _isDragging.Value = false;

        if (_rangeIndicator != null)
        {
            _rangeIndicator.ResetColor();
        }
    }

    private void ApplyLift(bool lifted)
    {
        if (_towerBody == null)
            return;

        _towerBody.localPosition = lifted
            ? _towerBodyLocalPosition + Vector3.up * _liftHeight
            : _towerBodyLocalPosition;
    }

    private void ApplySelection()
    {
        ApplyLift(_isSelected.Value);

        if (_rangeIndicator == null)
            return;

        if (_isSelected.Value)
        {
            _rangeIndicator.Show();
        }
        else
        {
            _rangeIndicator.Hide();
        }
    }
}
