using System;
using System.Collections.Generic;
using UnityEngine;

public static class WaveResolver
{
    public static void Resolve(WaveTableData table, int waveNumber, List<SpawnPlan> results, bool logIssues)
    {
        results.Clear();
        if (table == null)
            return;

        var zone = table.GetZone(WaveTableData.ZoneIndexOf(waveNumber));
        if (zone == null)
        {
            if (logIssues)
                Debug.Log($"[WaveResolver] 웨이브 {waveNumber}에 해당하는 구간이 없습니다.");
            return;
        }

        float interval = table.SpawnInterval;

        if (WaveTableData.IsBossWave(waveNumber))
        {
            var boss = zone.Boss;
            if (boss == null || boss.Prefab == null)
            {
                if (logIssues)
                    Debug.Log($"[WaveResolver] 웨이브 {waveNumber} 보스 데이터 또는 프리팹이 없습니다.");
                return;
            }

            results.Add(new SpawnPlan(boss, boss.BaseHealth, boss.GoldReward, interval));
            return;
        }

        var slime = zone.Slime;
        if (slime == null || slime.Prefab == null)
        {
            if (logIssues)
                Debug.Log($"[WaveResolver] 웨이브 {waveNumber} 슬라임 데이터 또는 프리팹이 없습니다.");
            return;
        }

        int health = ScaleHealth(slime.BaseHealth, table.NormalHpGrowth, waveNumber);
        for (int i = 0; i < zone.Count; i++)
            results.Add(new SpawnPlan(slime, health, slime.GoldReward, interval));
    }

    public static float StartDelayFor(WaveTableData table, int waveNumber)
    {
        if (table == null)
            return 0f;

        float delay = table.InterWaveDelay;
        if (!WaveTableData.IsBossWave(waveNumber))
            return delay;

        var zone = table.GetZone(WaveTableData.ZoneIndexOf(waveNumber));
        if (zone != null && zone.Boss != null)
            delay += zone.BossExtraStartDelay;

        return delay;
    }

    public static int ScaleHealth(int baseHealth, float hpGrowth, int waveNumber)
    {
        double multiplier = Math.Pow(1.0 + hpGrowth, Math.Max(0, waveNumber - 1));
        return Mathf.Max(1, (int)Math.Round(baseHealth * multiplier, MidpointRounding.AwayFromZero));
    }
}
