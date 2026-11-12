using System.Collections.Generic;
using UnityEngine;

public class GameMap : MonoBehaviour
{
    [SerializeField] private List<MapLane> _lanes = new List<MapLane>();

    public IReadOnlyList<MapLane> Lanes => _lanes;
}
