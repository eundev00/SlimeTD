using UnityEngine;
using UnityEngine.Splines;

public class MapPath : MonoBehaviour
{
    [SerializeField] private int _entrance;
    [NotNull][SerializeField] private SplineContainer _spline;

    public int Entrance => _entrance;
    public SplineContainer Spline => _spline;
}