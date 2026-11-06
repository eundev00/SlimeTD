using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// 저장 데이터를 건드리지 않고 HunterCardGachaRoller만 굴려 확률을 확인한다. 시작 해금(startUnlocked) 상태의 새 계정 기준이다.
public static class HunterCardGachaSimulator
{
    private const string LogTag = "HunterCardGachaSimulator";
    private const string ConfigPath = "Assets/Datas/HunterCardGachaConfig.asset";
    private const string CatalogPath = "Assets/Datas/TowerCatalog.asset";
    private const int TotalCards = 100000;

    [MenuItem("SlimeTD/Hunter Card Gacha/Simulate 100k Cards (Single)")]
    public static void SimulateSingle()
    {
        Simulate(1001);
    }

    [MenuItem("SlimeTD/Hunter Card Gacha/Simulate 100k Cards (Bundle)")]
    public static void SimulateBundle()
    {
        Simulate(1002);
    }

    // 전설 헌터가 아직 없어 천장이 동작하지 않으므로, 가짜 전설 하나를 넣어 천장 로직을 확인한다.
    [MenuItem("SlimeTD/Hunter Card Gacha/Simulate 100k Cards (Bundle + Test Legend)")]
    public static void SimulateWithTestLegend()
    {
        Simulate(1002, true);
    }

    private static void Simulate(int packId, bool addTestLegend = false)
    {
        var config = AssetDatabase.LoadAssetAtPath<HunterCardGachaConfig>(ConfigPath);
        var catalog = AssetDatabase.LoadAssetAtPath<TowerCatalog>(CatalogPath);
        if (config == null || catalog == null || !config.TryGetPack(packId, out var pack))
        {
            Debug.Log($"[{LogTag}] HunterCardGachaConfig, TowerCatalog 또는 팩 {packId}를 찾을 수 없습니다.");
            return;
        }

        var owned = new List<HunterCardGachaCandidate>();
        var locked = new List<HunterCardGachaCandidate>();
        foreach (var tower in catalog.Towers.Where(t => t != null && t.Enabled && t.InGacha))
        {
            var candidate = new HunterCardGachaCandidate(tower.Id, tower.GradeId);
            if (tower.StartUnlocked)
                owned.Add(candidate);
            else
                locked.Add(candidate);
        }

        if (addTestLegend)
            owned.Add(new HunterCardGachaCandidate(401, 4));

        var roller = new HunterCardGachaRoller(config, new System.Random(12345));
        var byGrade = new Dictionary<int, int>();
        var byTower = new Dictionary<int, int>();
        int lockedCards = 0;
        int pityHits = 0;
        int firstUnlockAt = -1;
        int cards = 0;
        int pity = 0;

        while (cards < TotalCards)
        {
            var picks = roller.Roll(pack, owned, locked, ref pity);
            if (picks.Count == 0)
                break;

            foreach (var pick in picks)
            {
                cards++;
                if (pick.IsLocked)
                {
                    lockedCards++;
                    if (firstUnlockAt < 0)
                        firstUnlockAt = cards;
                    continue;
                }

                if (pick.IsPity)
                    pityHits++;

                byGrade[pick.GradeId] = byGrade.TryGetValue(pick.GradeId, out int g) ? g + 1 : 1;
                byTower[pick.TowerId] = byTower.TryGetValue(pick.TowerId, out int t) ? t + 1 : 1;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[{LogTag}] 팩 {packId} ({pack.CardCount}장){(addTestLegend ? " + 가짜 전설" : "")} 로 {cards}장");
        foreach (var pair in byGrade.OrderBy(p => p.Key))
            sb.AppendLine($"  등급 {pair.Key}: {pair.Value} ({pair.Value * 100.0 / cards:0.00}%)");
        sb.AppendLine($"  잠긴 카드: {lockedCards} (첫 해금 {firstUnlockAt}장째)");
        sb.AppendLine($"  천장 발동: {pityHits}");
        foreach (var pair in byTower.OrderBy(p => p.Key))
            sb.AppendLine($"  타워 {pair.Key}: {pair.Value * 100.0 / cards:0.00}%");

        Debug.Log(sb.ToString());
    }
}
