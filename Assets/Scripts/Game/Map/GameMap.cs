using System.Collections.Generic;
using UnityEngine;

public class GameMap : MonoBehaviour
{
    [SerializeField] private List<MapPath> _paths = new List<MapPath>();
    [SerializeField] private List<MapZone> _zones = new List<MapZone>();

    public IReadOnlyList<MapPath> Paths => _paths;
    public IReadOnlyList<MapZone> Zones => _zones;

    public MapZone GetZone(int slot)
    {
        foreach (var zone in _zones)
        {
            if (zone != null && zone.Slot == slot)
                return zone;
        }

        return null;
    }
}