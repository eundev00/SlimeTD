using MessagePipe;
using UnityEngine;
using VContainer;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyView _goldCurrency;
    [NotNull][SerializeField] private CurrencyView _gemCurrency;

    [Inject]
    public void Construct(IAccountService accountService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _goldCurrency.Initialize(accountService, clickedPublisher);
        _gemCurrency.Initialize(accountService, clickedPublisher);
    }
}
