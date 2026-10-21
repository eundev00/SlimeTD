using System;
using UniRx;

public class CurrencyChipPresenter : IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public CurrencyChipPresenter(CurrencyChip chip, IPlayerDataService playerDataService)
    {
        GetSource(chip.Type, playerDataService)
            .Subscribe(chip.SetValue)
            .AddTo(_disposables);

        // TODO: 상점이 생기면 Gold/Gem 클릭 시 상점 열기
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private static IReadOnlyReactiveProperty<int> GetSource(CurrencyType type, IPlayerDataService service)
    {
        switch (type)
        {
            case CurrencyType.Gold: return service.Gold;
            case CurrencyType.Gem: return service.Gem;
            default: return service.Cards;
        }
    }
}
