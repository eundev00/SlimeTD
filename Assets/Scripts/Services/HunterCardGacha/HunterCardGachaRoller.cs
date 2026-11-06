using System.Collections.Generic;

public readonly struct HunterCardGachaCandidate
{
    public readonly int TowerId;
    public readonly int GradeId;

    public HunterCardGachaCandidate(int towerId, int gradeId)
    {
        TowerId = towerId;
        GradeId = gradeId;
    }
}

public readonly struct HunterCardGachaPick
{
    public readonly int TowerId;
    public readonly int GradeId;
    public readonly bool IsLocked;
    public readonly bool IsPity;

    public HunterCardGachaPick(int towerId, int gradeId, bool isLocked, bool isPity)
    {
        TowerId = towerId;
        GradeId = gradeId;
        IsLocked = isLocked;
        IsPity = isPity;
    }
}

// 저장이나 지급을 모르는 순수 계산이다. 서비스와 에디터 시뮬레이션이 같이 쓴다.
public class HunterCardGachaRoller
{
    private readonly HunterCardGachaConfig _config;
    private readonly System.Random _random;
    private readonly List<HunterCardGachaCandidate> _gradeBuffer = new List<HunterCardGachaCandidate>();

    public HunterCardGachaRoller(HunterCardGachaConfig config, System.Random random)
    {
        _config = config;
        _random = random;
    }

    // owned는 등급 풀(보유 헌터), locked는 잠긴 카드 대상이다. 잠긴 카드로 해금된 헌터는 같은 팩의 다음 장부터 owned에 들어간다.
    public List<HunterCardGachaPick> Roll(HunterCardGachaPack pack, List<HunterCardGachaCandidate> owned, List<HunterCardGachaCandidate> locked, ref int pity)
    {
        var picks = new List<HunterCardGachaPick>(pack.CardCount);
        int pityBeforeLast = pity;

        for (int i = 0; i < pack.CardCount; i++)
        {
            pityBeforeLast = pity;
            if (TryRollOne(owned, locked, 0, ref pity, out var pick))
                picks.Add(pick);
        }

        if (pack.HasGuarantee && picks.Count > 0 && !MeetsGuarantee(picks, pack.GuaranteeGradeId))
        {
            // 보장 등급이 없으면 마지막 장을 보장 등급 이상에서 다시 뽑는다. 천장 카운터는 그 장 이전으로 되돌린다.
            int pityRetry = pityBeforeLast;
            if (TryRollOne(owned, null, pack.GuaranteeGradeId, ref pityRetry, out var guaranteed))
            {
                picks[picks.Count - 1] = guaranteed;
                pity = pityRetry;
            }
        }

        return picks;
    }

    private bool TryRollOne(List<HunterCardGachaCandidate> owned, List<HunterCardGachaCandidate> locked, int minGrade, ref int pity, out HunterCardGachaPick pick)
    {
        pick = default;

        // 잠긴 카드는 등급 판정보다 먼저 굴리고 천장 카운터에 넣지 않는다.
        if (locked != null && locked.Count > 0 && _random.NextDouble() * 100.0 < _config.LockedCardChance)
        {
            int index = _random.Next(locked.Count);
            var target = locked[index];
            locked.RemoveAt(index);
            owned.Add(target);
            pick = new HunterCardGachaPick(target.TowerId, target.GradeId, true, false);
            return true;
        }

        int pityGrade = _config.PityGradeId;
        bool pityReady = _config.PityCount > 0 && pity >= _config.PityCount && pityGrade >= minGrade && HasGrade(owned, pityGrade);

        int grade;
        if (pityReady)
            grade = pityGrade;
        else if (!TryRollGrade(owned, minGrade, out grade))
            return false;

        _gradeBuffer.Clear();
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i].GradeId == grade)
                _gradeBuffer.Add(owned[i]);
        }

        var chosen = _gradeBuffer[_random.Next(_gradeBuffer.Count)];
        pity = grade == pityGrade ? 0 : pity + 1;
        pick = new HunterCardGachaPick(chosen.TowerId, grade, false, pityReady);
        return true;
    }

    // 보유 헌터가 없는 등급은 빼고 남은 가중치끼리 비율을 다시 계산한다.
    private bool TryRollGrade(List<HunterCardGachaCandidate> owned, int minGrade, out int grade)
    {
        grade = 0;
        var weights = _config.GradeWeights;

        double total = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            if (IsEligible(weights[i], owned, minGrade))
                total += weights[i].Weight;
        }

        if (total <= 0)
            return false;

        double roll = _random.NextDouble() * total;
        for (int i = 0; i < weights.Count; i++)
        {
            if (!IsEligible(weights[i], owned, minGrade))
                continue;

            grade = weights[i].GradeId;
            roll -= weights[i].Weight;
            if (roll < 0)
                return true;
        }

        return true;
    }

    private static bool IsEligible(HunterCardGachaGradeWeight weight, List<HunterCardGachaCandidate> owned, int minGrade)
    {
        return weight.Weight > 0f && weight.GradeId >= minGrade && HasGrade(owned, weight.GradeId);
    }

    private static bool HasGrade(List<HunterCardGachaCandidate> owned, int gradeId)
    {
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i].GradeId == gradeId)
                return true;
        }

        return false;
    }

    private static bool MeetsGuarantee(List<HunterCardGachaPick> picks, int gradeId)
    {
        for (int i = 0; i < picks.Count; i++)
        {
            if (picks[i].IsLocked || picks[i].GradeId >= gradeId)
                return true;
        }

        return false;
    }
}
