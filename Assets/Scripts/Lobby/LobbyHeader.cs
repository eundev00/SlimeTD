using UnityEngine;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private CurrencyChip _goldChip;
    [NotNull][SerializeField] private CurrencyChip _gemChip;

    public CurrencyChip GoldChip => _goldChip;
    public CurrencyChip GemChip => _gemChip;
}
