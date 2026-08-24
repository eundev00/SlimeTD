using System;

public class GameplayHudPresenter : IDisposable
{
    private readonly GameplayHudView _view;
    private readonly ITowerSpawner _towerSpawner;

    public GameplayHudPresenter(GameplayHudView view, ITowerSpawner towerSpawner)
    {
        _view = view;
        _towerSpawner = towerSpawner;

        _view.SummonButtonClicked += HandleSummonButtonClicked;
    }

    private void HandleSummonButtonClicked()
    {
        _towerSpawner.TrySpawnRandom();
    }

    public void Dispose()
    {
        if (_view != null)
            _view.SummonButtonClicked -= HandleSummonButtonClicked;
    }
}
