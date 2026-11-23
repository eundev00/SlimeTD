using MessagePipe;
using UnityEngine;
using VContainer;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyView _coinCurrency;
    [NotNull][SerializeField] private CurrencyView _diamondCurrency;

    [Inject]
    public void Construct(IAccountService accountService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _coinCurrency.Initialize(accountService, clickedPublisher);
        _diamondCurrency.Initialize(accountService, clickedPublisher);
    }
}
