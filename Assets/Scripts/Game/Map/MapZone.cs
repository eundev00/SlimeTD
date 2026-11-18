using UnityEngine;

// slot 0이 나(아래), 1이 팀원(위)이다
public class MapZone : MonoBehaviour
{
    [SerializeField] private int _slot;
    [NotNull][SerializeField] private GridMapReference _gridMapReference;

    public int Slot => _slot;
    public GridMapData GridMapData => _gridMapReference != null ? _gridMapReference.GridMapData : null;
}