using System.Collections.Generic;
using UnityEngine;

// 감속 중첩 규칙은 Doc/슬라임TD_버프디버프중첩규칙.md를 따른다.
public class SlimeSlow
{
    private struct Entry
    {
        public int SourceSpawnId;
        public float Percent;
        public float Remaining;
    }

    private readonly List<Entry> _entries = new List<Entry>();

    public bool IsActive => _entries.Count > 0;
    public float SpeedRatio { get; private set; } = 1f;

    // 같은 타워가 다시 맞히면 지속 시간만 갱신하고, 다른 타워면 따로 쌓는다.
    public void Apply(int sourceSpawnId, float percent, float duration, float minSpeedRatio)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].SourceSpawnId != sourceSpawnId)
                continue;

            var entry = _entries[i];
            entry.Percent = percent;
            entry.Remaining = duration;
            _entries[i] = entry;
            Recalculate(minSpeedRatio);
            return;
        }

        _entries.Add(new Entry { SourceSpawnId = sourceSpawnId, Percent = percent, Remaining = duration });
        Recalculate(minSpeedRatio);
    }

    // 속도 비율이 바뀌었으면 true를 돌려준다.
    public bool Tick(float deltaTime, float minSpeedRatio)
    {
        bool expired = false;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            entry.Remaining -= deltaTime;

            if (entry.Remaining <= 0f)
            {
                _entries.RemoveAt(i);
                expired = true;
                continue;
            }

            _entries[i] = entry;
        }

        if (!expired)
            return false;

        Recalculate(minSpeedRatio);
        return true;
    }

    public void Clear()
    {
        _entries.Clear();
        SpeedRatio = 1f;
    }

    private void Recalculate(float minSpeedRatio)
    {
        float ratio = 1f;
        for (int i = 0; i < _entries.Count; i++)
            ratio *= 1f - Mathf.Clamp01(_entries[i].Percent / 100f);

        SpeedRatio = Mathf.Max(minSpeedRatio, ratio);
    }
}
