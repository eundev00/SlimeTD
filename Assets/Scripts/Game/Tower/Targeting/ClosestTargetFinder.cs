public class ClosestTargetFinder : TargetFinderBase
{
    protected override float GetDistance(in TargetInfo candidate)
    {
        return candidate.SqrDistance;
    }
}
