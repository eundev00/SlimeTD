using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SlimeTD.Protocol;
using VContainer.Unity;

// TODO: 개발용. 에너지 충전 팝업이 생기면 에너지 버튼은 그쪽으로 연결한다
public class EnergyCheatHandler : IStartable, IDisposable
{
    private readonly ISubscriber<CurrencyViewClickedEvent> _clickedSubscriber;
    private readonly ICheatService _cheatService;
    private IDisposable _subscription;
    private bool _requesting;

    public EnergyCheatHandler(ISubscriber<CurrencyViewClickedEvent> clickedSubscriber, ICheatService cheatService)
    {
        _clickedSubscriber = clickedSubscriber;
        _cheatService = cheatService;
    }

    public void Start()
    {
        _subscription = _clickedSubscriber.Subscribe(HandleClicked);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private void HandleClicked(CurrencyViewClickedEvent e)
    {
        if (e.Type == CurrencyType.Energy)
            FillEnergyAsync().Forget();
    }

    private async UniTaskVoid FillEnergyAsync()
    {
        if (_requesting)
            return;

        _requesting = true;
        try
        {
            await _cheatService.RequestAsync(CheatType.FillEnergy);
        }
        finally
        {
            _requesting = false;
        }
    }
}
