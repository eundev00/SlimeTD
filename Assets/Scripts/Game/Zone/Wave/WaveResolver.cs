using System;
using System.Collections.Generic;
using UnityEngine;

public static class WaveResolver
{
    private const int AutoWaveStart = 11;

    private static readonly List<SlimeData> Candidates = new();
    private static readonly List<SlimeData> Minions = new();
    private static readonly List<int> Counts = new();
    private static readonly List<double> Remainders = new();
    private static readonly List<int> Order = new();

    public static void Resolve(WaveTableData table, int waveNumber, List<SpawnPlan> results, System.Random shuffleRng, bool logIssues)
    {
        results.Clear();
        if (table == null)
            return;

        float interval = table.SpawnInterval;

        var fixedWave = FindFixedWave(table, waveNumber);
        if (fixedWave != null && fixedWave.Slimes != null && fixedWave.Slimes.Length > 0)
        {
            Minions.Clear();
            foreach (var slime in fixedWave.Slimes)
            {
                if (slime != null && slime.Prefab != null)
                    Minions.Add(slime);
            }
        }
        else
        {
            if (logIssues && waveNumber < AutoWaveStart)
                Debug.Log($"[WaveResolver] 웨이브 {waveNumber}가 고정 웨이브에 없어 자동 생성으로 넘어갑니다. 데이터 누락일 수 있습니다.");

            ApportionAuto(table.AutoWaveSetting, waveNumber, logIssues);
            Shuffle(Minions, shuffleRng);
        }

        foreach (var slime in Minions)
        {
            int health = ScaleHealth(slime.BaseHealth, table.HpGrowth, waveNumber);
            int gold = Mathf.Max(1, Mathf.RoundToInt(health * table.GoldRatio));
            results.Add(new SpawnPlan(slime, health, gold, interval));
        }

        var bossEntries = table.BossEntries;
        if (bossEntries == null)
            return;

        for (int i = 0; i < bossEntries.Count; i++)
        {
            var entry = bossEntries[i];
            if (entry == null || entry.WaveNumber != waveNumber)
                continue;

            var boss = entry.BossData;
            if (boss == null || boss.Prefab == null)
            {
                if (logIssues)
                    Debug.Log($"[WaveResolver] 웨이브 {waveNumber} 보스 데이터 또는 프리팹이 없어 건너뜁니다.");
                continue;
            }

            results.Insert(0, new SpawnPlan(boss, boss.BaseHealth, boss.GoldReward, interval));
        }
    }

    public static float StartDelayFor(WaveTableData table, int waveNumber)
    {
        if (table == null)
            return 0f;

        float delay = table.InterWaveDelay;
        var bossEntries = table.BossEntries;
        if (bossEntries == null)
            return delay;

        for (int i = 0; i < bossEntries.Count; i++)
        {
            var entry = bossEntries[i];
            if (entry != null && entry.WaveNumber == waveNumber && entry.BossData != null)
            {
                delay += entry.ExtraStartDelay;
                break;
            }
        }

        return delay;
    }

    public static int ScaleHealth(int baseHealth, float hpGrowth, int waveNumber)
    {
        double multiplier = Math.Pow(1.0 + hpGrowth, Math.Max(0, waveNumber - 1));
        return Mathf.Max(1, (int)Math.Round(baseHealth * multiplier, MidpointRounding.AwayFromZero));
    }

    private static FixedWaveData FindFixedWave(WaveTableData table, int waveNumber)
    {
        var fixedWaves = table.FixedWaves;
        if (fixedWaves == null)
            return null;

        for (int i = 0; i < fixedWaves.Length; i++)
        {
            if (fixedWaves[i] != null && fixedWaves[i].WaveNumber == waveNumber)
                return fixedWaves[i];
        }

        return null;
    }

    private static void ApportionAuto(AutoWaveSetting auto, int waveNumber, bool logIssues)
    {
        Minions.Clear();
        if (auto == null || auto.Slimes == null)
            return;

        Candidates.Clear();
        long weightSum = 0;
        foreach (var slime in auto.Slimes)
        {
            if (slime == null || slime.Prefab == null)
                continue;

            if (slime.AppearFromWave <= waveNumber && slime.SpawnWeight > 0)
            {
                Candidates.Add(slime);
                weightSum += slime.SpawnWeight;
            }
        }

        if (weightSum <= 0)
        {
            var fallback = FindEarliestSlime(auto);
            if (fallback == null)
            {
                if (logIssues)
                    Debug.Log($"[WaveResolver] 웨이브 {waveNumber}에 사용할 슬라임이 없습니다. autoWave 슬라임 목록을 확인하세요.");
                return;
            }

            if (logIssues)
                Debug.Log($"[WaveResolver] 웨이브 {waveNumber} 후보가 없어 {fallback.name}으로 폴백합니다. appearFromWave/spawnWeight를 확인하세요.");

            Candidates.Add(fallback);
            weightSum = Mathf.Max(1, fallback.SpawnWeight);
        }

        int exponent = Mathf.Max(0, waveNumber - AutoWaveStart);
        double raw = auto.BaseCount * Math.Pow(1.0 + auto.CountGrowth, exponent);
        int total = Mathf.Clamp((int)Math.Floor(raw), 1, Mathf.Max(1, auto.MaxCount));

        Counts.Clear();
        Remainders.Clear();
        int assigned = 0;

        for (int i = 0; i < Candidates.Count; i++)
        {
            double exact = (double)total * Candidates[i].SpawnWeight / weightSum;
            int floor = (int)Math.Floor(exact);
            Counts.Add(floor);
            Remainders.Add(exact - floor);
            assigned += floor;
        }

        Order.Clear();
        for (int i = 0; i < Candidates.Count; i++)
            Order.Add(i);

        Order.Sort((a, b) =>
        {
            int cmp = Remainders[b].CompareTo(Remainders[a]);
            return cmp != 0 ? cmp : a.CompareTo(b);
        });

        for (int k = 0; assigned < total; k++, assigned++)
            Counts[Order[k % Order.Count]]++;

        for (int i = 0; i < Candidates.Count; i++)
        {
            for (int c = 0; c < Counts[i]; c++)
                Minions.Add(Candidates[i]);
        }
    }

    private static SlimeData FindEarliestSlime(AutoWaveSetting auto)
    {
        SlimeData earliest = null;
        foreach (var slime in auto.Slimes)
        {
            if (slime == null || slime.Prefab == null)
                continue;

            if (earliest == null || slime.AppearFromWave < earliest.AppearFromWave)
                earliest = slime;
        }

        return earliest;
    }

    private static void Shuffle(List<SlimeData> list, System.Random rng)
    {
        if (rng == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
