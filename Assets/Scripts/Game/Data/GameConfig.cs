using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "SlimeTD/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [SerializeField] private int _startingLife = 20;
    [SerializeField] private int _startingGold = 0;

    public int StartingLife => _startingLife;
    public int StartingGold => _startingGold;
}
