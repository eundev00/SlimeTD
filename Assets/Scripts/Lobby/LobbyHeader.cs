using MessagePipe;
using UnityEngine;
using VContainer;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyView _coinCurrency;
    [NotNull][SerializeField] private CurrencyView _diamondCurrency;

    [Inject]
    public void Construct(IPlayerDataService playerDataService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _coinCurrency.Initialize(playerDataService, clickedPublisher);
        _diamondCurrency.Initialize(playerDataService, clickedPublisher);
    }
}
