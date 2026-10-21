using MessagePipe;
using UnityEngine;
using VContainer;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyChip _goldChip;
    [NotNull][SerializeField] private CurrencyChip _gemChip;

    [Inject]
    public void Construct(IPlayerDataService playerDataService, IPublisher<CurrencyChipClickedEvent> clickedPublisher)
    {
        _goldChip.Initialize(playerDataService, clickedPublisher);
        _gemChip.Initialize(playerDataService, clickedPublisher);
    }
}
