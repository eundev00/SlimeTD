using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerCatalog", menuName = "SlimeTD/Tower Catalog", order = 62)]
public class TowerCatalog : ScriptableObject
{
    [SerializeField] private TowerData[] _towers;

    private readonly Dictionary<int, TowerData> _byId = new Dictionary<int, TowerData>();
    private readonly Dictionary<int, List<TowerData>> _byGrade = new Dictionary<int, List<TowerData>>();
    private bool _indexed;

    public TowerData[] Towers => _towers;

    private void OnEnable()
    {
        _indexed = false;
    }

    public bool TryGetById(int id, out TowerData data)
    {
        EnsureIndex();
        return _byId.TryGetValue(id, out data);
    }

    // 비활성이거나 프리팹이 없는 타워는 뽑기 후보에서 제외한다.
    public bool TryDraw(int gradeId, out TowerData data)
    {
        data = null;
        EnsureIndex();

        if (!_byGrade.TryGetValue(gradeId, out var candidates) || candidates.Count == 0)
            return false;

        data = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    public bool TryDrawNextGrade(TowerData source, out TowerData result)
    {
        result = null;

        if (source == null)
            return false;

        return TryDraw(source.GradeId + 1, out result);
    }

    private void EnsureIndex()
    {
        if (_indexed)
            return;

        _byId.Clear();
        _byGrade.Clear();
        _indexed = true;

        if (_towers == null)
            return;

        for (int i = 0; i < _towers.Length; i++)
        {
            var tower = _towers[i];
            if (tower == null)
                continue;

            if (_byId.ContainsKey(tower.Id))
            {
                Debug.Log($"[TowerCatalog] 타워 ID가 중복되었습니다: {tower.Id}", this);
                continue;
            }

            _byId.Add(tower.Id, tower);

            if (!tower.Enabled || tower.Prefab == null)
                continue;

            if (!_byGrade.TryGetValue(tower.GradeId, out var list))
            {
                list = new List<TowerData>();
                _byGrade.Add(tower.GradeId, list);
            }

            list.Add(tower);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _indexed = false;
    }
#endif
}
