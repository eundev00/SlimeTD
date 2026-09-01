public class ClosestTargetFinder : TargetFinderBase
{
    protected override float GetScore(in TargetInfo candidate)
    {
        return candidate.SqrDistance;
    }
}
