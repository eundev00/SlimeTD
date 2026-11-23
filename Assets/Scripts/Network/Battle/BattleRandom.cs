namespace SlimeTD.Battle
{
    /// <summary>
    /// 전투 난수. 서버가 전투 시작 때 준 시드 하나로 끝없는 숫자 목록을 만든다. 같은 시드면 어느 플랫폼에서도 같은 목록이 나온다
    /// (64비트 정수 연산만 쓴다: SplitMix64). 클라이언트는 이 파일을 그대로 복사해 쓴다. 계산을 바꾸면 서버와 클라이언트를 같이 바꾼다.
    ///
    /// 용도마다 목록을 따로 둔다 (ForSummon, ForMerge). 그래서 합성을 언제 하든 소환 결과 순서는 바뀌지 않는다.
    /// </summary>
    public sealed class BattleRandom
    {
        private const ulong SummonStream = 0x53554D4D4F4E0001;   // "SUMMON"
        private const ulong MergeStream = 0x4D45524745000002;    // "MERGE"

        private ulong _state;

        public BattleRandom(ulong seed) => _state = seed;

        /// <summary>소환할 때 어떤 헌터가 나올지 뽑는 목록.</summary>
        public static BattleRandom ForSummon(ulong seed) => new BattleRandom(seed ^ SummonStream);

        /// <summary>합성할 때 다음 등급 중 어떤 헌터가 나올지 뽑는 목록.</summary>
        public static BattleRandom ForMerge(ulong seed) => new BattleRandom(seed ^ MergeStream);

        /// <summary>다음 64비트 숫자.</summary>
        public ulong NextULong()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
                return z ^ (z >> 31);
            }
        }

        /// <summary>0 이상 count 미만의 정수. 위 32비트에 count를 곱해 위 32비트를 쓴다.</summary>
        public int NextInt(int count)
        {
            if (count <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(count));
            return (int)(((NextULong() >> 32) * (ulong)count) >> 32);
        }

        /// <summary>지금 위치를 기억해 둔다. 뽑은 결과를 쓰지 않을 때(입력 거절) Restore로 되돌린다.</summary>
        public ulong Save() => _state;

        public void Restore(ulong saved) => _state = saved;
    }
}
