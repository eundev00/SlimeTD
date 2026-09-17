using System;
using MessagePipe;
using Services.UpdateService;
using UniRx;
using UnityEngine;
using VContainer.Unity;

public class TowerActionPresenter : IStartable, ILateUpdatable, IDisposable
{
    private readonly TowerActionView _view;
    private readonly Zone _zone;
    private readonly IUpdateSubscriptionService _updateService;
    private readonly ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly SerialDisposable _dragSubscription = new SerialDisposable();

    private TowerInputHandler _inputHandler;
    private ITowerInteractionHandler _selected;
    private Camera _camera;
    private bool _gameEnded;

    public TowerActionPresenter(
        TowerActionView view,
        Zone zone,
        IUpdateSubscriptionService updateService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _view = view;
        _zone = zone;
        _updateService = updateService;
        _gameProgressSubscriber = gameProgressSubscriber;
    }

    public void Start()
    {
        _camera = Camera.main;

        _view.MergeButtonClicked += HandleMergeButtonClicked;
        _view.Hide();

        _inputHandler = _zone != null ? _zone.InputHandler : null;
        if (_inputHandler == null)
        {
            Debug.Log("[TowerActionPresenter] TowerInputHandler를 찾을 수 없어 타워 액션 UI가 비활성화됩니다.");
            return;
        }

        _inputHandler.SelectionChanged += HandleSelectionChanged;
        _updateService.RegisterLateUpdatable(this);

        _gameProgressSubscriber.Subscribe(HandleGameProgress).AddTo(_disposables);
    }

    public void ManagedLateUpdate()
    {
        if (_selected == null)
            return;

        if (_selected is not MonoBehaviour behaviour || behaviour == null)
        {
            _selected = null;
            _dragSubscription.Disposable = null;
            _view.Hide();
            return;
        }

        UpdateScreenPosition(behaviour.transform.position);
    }

    private void HandleSelectionChanged(ITowerInteractionHandler tower)
    {
        _selected = tower;

        if (_gameEnded || tower is not BaseTower selectedTower || selectedTower == null)
        {
            _selected = null;
            _dragSubscription.Disposable = null;
            _view.Hide();
            return;
        }

        _dragSubscription.Disposable = tower.IsDragging.Subscribe(HandleDraggingChanged);
        _view.Show();

        UpdateScreenPosition(selectedTower.transform.position);
    }

    private void HandleDraggingChanged(bool dragging)
    {
        if (_selected == null)
            return;

        _view.SetMergeButtonInteractable(!dragging && _zone.CanMerge(_selected));
    }

    private void UpdateScreenPosition(Vector3 towerPosition)
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
            return;

        if (!_zone.TryGetCellCenter(towerPosition, out var anchorPosition))
            anchorPosition = towerPosition;

        var screenPosition = _camera.WorldToScreenPoint(anchorPosition);
        if (screenPosition.z < 0f)
        {
            _view.Hide();
            return;
        }

        _view.Show();
        _view.SetScreenPosition(screenPosition);
    }

    private void HandleMergeButtonClicked()
    {
        if (_selected == null)
            return;

        if (_zone.TryMerge(_selected))
        {
            _inputHandler.ClearSelectedTower();
            return;
        }

        _view.SetMergeButtonInteractable(false);
    }

    private void HandleGameProgress(GameProgressEvent e)
    {
        if (e.EventType != GameProgressType.GameOver && e.EventType != GameProgressType.StageCleared)
            return;

        _gameEnded = true;
        _selected = null;
        _dragSubscription.Disposable = null;
        _view.Hide();
    }

    public void Dispose()
    {
        if (_inputHandler != null)
        {
            _inputHandler.SelectionChanged -= HandleSelectionChanged;
            _updateService.UnregisterLateUpdatable(this);
        }

        if (_view != null)
        {
            _view.MergeButtonClicked -= HandleMergeButtonClicked;
        }

        _dragSubscription.Dispose();
        _disposables.Dispose();
    }
}
