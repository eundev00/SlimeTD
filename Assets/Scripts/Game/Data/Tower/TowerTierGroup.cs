using UnityEngine;

[CreateAssetMenu(fileName = "TowerTier_00", menuName = "SlimeTD/Tower Tier Group", order = 63)]
public class TowerTierGroup : ScriptableObject
{
    [SerializeField, Min(1)] private int _tier = 1;
    [SerializeField] private TowerData[] _towers;

    public int Tier => _tier;
    public TowerData[] Towers => _towers;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_towers == null)
            return;

        for (int i = 0; i < _towers.Length; i++)
        {
            var tower = _towers[i];
            if (tower == null)
            {
                Debug.Log($"[TowerTierGroup] {i}번 칸이 비어 있습니다.", this);
                continue;
            }

            if (tower.Prefab == null)
                Debug.Log($"[TowerTierGroup] {tower.name}에 프리팹이 없습니다.", this);
        }
    }
#endif
}
