using UnityEngine;
using UnityEngine.Splines;

public class MapLane : MonoBehaviour
{
    [NotNull][SerializeField] private GridMapReference _gridMapReference;
    [NotNull][SerializeField] private SplineContainer _path;

    public GridMapData GridMapData => _gridMapReference != null ? _gridMapReference.GridMapData : null;
    public SplineContainer Path => _path;
}
