using System;
using System.Collections.Generic;
using UnityEngine;

public static class WaveResolver
{
    public static void Resolve(StageData stage, int waveNumber, List<SpawnPlan> results, bool logIssues)
    {
        results.Clear();
        if (stage == null)
            return;

        var wave = stage.GetWave(waveNumber);
        if (wave == null)
        {
            if (logIssues)
                Debug.Log($"[WaveResolver] 웨이브 {waveNumber} 데이터가 없습니다.");
            return;
        }

        float interval = stage.SpawnInterval;

        foreach (var group in wave.Groups)
        {
            var slime = group?.Slime;
            if (slime == null || slime.Prefab == null)
            {
                if (logIssues)
                    Debug.Log($"[WaveResolver] 웨이브 {waveNumber} 무리의 슬라임 데이터 또는 프리팹이 없습니다.");
                continue;
            }

            // 보스는 성장률을 적용하지 않고 기본 HP를 그대로 쓴다
            int health = slime.Kind == SlimeKind.Boss
                ? slime.BaseHealth
                : ScaleHealth(slime.BaseHealth, stage.NormalHpGrowth, waveNumber);

            for (int i = 0; i < group.Count; i++)
                results.Add(new SpawnPlan(slime, health, slime.JellyReward, i == 0 ? group.Delay : 0f, interval));
        }
    }

    public static int ScaleHealth(int baseHealth, float hpGrowth, int waveNumber)
    {
        double multiplier = Math.Pow(1.0 + hpGrowth, Math.Max(0, waveNumber - 1));
        return Mathf.Max(1, (int)Math.Round(baseHealth * multiplier, MidpointRounding.AwayFromZero));
    }
}
