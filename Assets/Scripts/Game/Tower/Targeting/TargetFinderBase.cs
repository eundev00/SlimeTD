using UnityEngine;

public abstract class TargetFinderBase : ITargetFinder
{
    private const int BufferSize = 32;

    private readonly Collider[] _hitBuffer = new Collider[BufferSize];
    private readonly LayerMask _slimeLayer;

    protected TargetFinderBase()
    {
        _slimeLayer = LayerMask.GetMask(GameTags.SlimeLayer);
    }

    public bool TryFind(Vector3 origin, float range, out TargetInfo target)
    {
        target = default;

        int count = Physics.OverlapSphereNonAlloc(origin, range, _hitBuffer, _slimeLayer);
        if (count == 0)
            return false;

        float bestScore = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            var collider = _hitBuffer[i];
            if (!collider.gameObject.activeInHierarchy)
                continue;

            var slime = collider.GetComponentInParent<BaseSlime>();
            if (slime == null)
                continue;

            var candidateTransform = slime.transform;
            float sqrDistance = (candidateTransform.position - origin).sqrMagnitude;
            var candidate = new TargetInfo(candidateTransform, slime, sqrDistance);

            float score = GetScore(candidate);
            if (score >= bestScore)
                continue;

            bestScore = score;
            target = candidate;
            found = true;
        }

        return found;
    }

    protected abstract float GetScore(in TargetInfo candidate);
}
