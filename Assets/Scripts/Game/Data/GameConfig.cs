using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "SlimeTD/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("플레이어")]
    [SerializeField] private int _startingLife = 20;
    [SerializeField] private int _startingGold = 0;

    [Header("디버그")]
    [SerializeField] private bool _ignoreGoldCost;
    [SerializeField] private bool _autoStartWave = true;

    public int StartingLife => _startingLife;
    public int StartingGold => _startingGold;
    public bool IgnoreGoldCost => _ignoreGoldCost;
    public bool AutoStartWave => _autoStartWave;
}
