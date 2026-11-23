using UniRx;
using UnityEngine;

public class BaseTowerPreview : MonoBehaviour, ITowerInteractionHandler
{
    [SerializeField] private TowerRangeIndicator _rangeIndicator;
    [SerializeField] private float _previewRange = 5f;
    [NotNull][SerializeField] private Transform _liftTarget;
    [SerializeField] private float _liftHeight = 0.35f;

    private readonly ReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);

    private Vector3 _liftTargetLocalPosition;

    public IReadOnlyReactiveProperty<bool> IsSelected => _isSelected;

    private void Start()
    {
        if (_liftTarget != null)
        {
            _liftTargetLocalPosition = _liftTarget.localPosition;
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

    private void ApplyLift(bool lifted)
    {
        if (_liftTarget == null)
            return;

        _liftTarget.localPosition = lifted
            ? _liftTargetLocalPosition + Vector3.up * _liftHeight
            : _liftTargetLocalPosition;
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
