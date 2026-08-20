using UnityEngine;

public class GridMapReference : MonoBehaviour
{
    [SerializeField] private GridMapData _gridMapData;

    public GridMapData GridMapData => _gridMapData;
}
