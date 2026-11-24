using MessagePipe;
using Services.UpdateService;
using UnityEngine;
using VContainer;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyView _energyCurrency;
    [NotNull][SerializeField] private CurrencyView _coinCurrency;
    [NotNull][SerializeField] private CurrencyView _diamondCurrency;
    [NotNull][SerializeField] private EnergyTimerView _energyTimer;

    [Inject]
    public void Construct(IAccountService accountService, IPublisher<CurrencyViewClickedEvent> clickedPublisher, IUpdateSubscriptionService updateService)
    {
        _energyCurrency.Initialize(accountService, clickedPublisher);
        _coinCurrency.Initialize(accountService, clickedPublisher);
        _diamondCurrency.Initialize(accountService, clickedPublisher);
        _energyTimer.Initialize(accountService, updateService);
    }
}
