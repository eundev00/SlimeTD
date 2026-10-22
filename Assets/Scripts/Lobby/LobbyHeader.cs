using MessagePipe;
using UnityEngine;
using VContainer;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyView _goldCurrency;
    [NotNull][SerializeField] private CurrencyView _gemCurrency;

    [Inject]
    public void Construct(IPlayerDataService playerDataService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _goldCurrency.Initialize(playerDataService, clickedPublisher);
        _gemCurrency.Initialize(playerDataService, clickedPublisher);
    }
}
