using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 즉시 타격 빔. 타격 프레임에 데미지를 바로 주고 빔은 연출만 한다.
public class BeamAttack : AttackBehaviourBase
{
    private const float BeamVisibleTime = 0.08f;
    private const float BeamWidth = 0.06f;
    private static readonly Color BeamColor = new Color(0.25f, 0.85f, 1f);

    private readonly CancellationTokenSource _disposeCancellation = new CancellationTokenSource();
    private ProjectileLauncher _launcher;
    private LineRenderer _beam;
    private Material _beamMaterial;
    private int _beamVersion;

    public override void Initialize(ITowerContext context)
    {
        base.Initialize(context);

        _launcher = context.Transform.GetComponent<ProjectileLauncher>();
        if (_launcher == null)
        {
            Debug.Log("[BeamAttack] ProjectileLauncher가 없어 빔을 쏠 위치를 알 수 없습니다.", context.Transform);
            return;
        }

        CreateBeam();
    }

    protected override void Apply(in TargetInfo target, in AttackSkillRoll roll)
    {
        Vector3 hitPoint = GetHitPoint(target);
        var info = ResolveDamage(roll);

        target.Slime.TakeDamage(info);

        if (roll.SplashRadius > 0f && target.Transform != null)
            SplashDamage.Apply(target.Transform.position, roll.SplashRadius, info, target.Transform.GetComponentInParent<BaseSlime>());

        ShowBeamAsync(hitPoint).Forget();
    }

    private static Vector3 GetHitPoint(in TargetInfo target)
    {
        if (target.Transform == null)
            return Vector3.zero;

        return target.Transform.TryGetComponent<Collider>(out var collider)
            ? collider.bounds.center
            : target.Transform.position;
    }

    // TODO: 빔 연출을 투사체 에셋으로 교체한다. 지금은 임시 LineRenderer다.
    private void CreateBeam()
    {
        var go = new GameObject("Beam");
        go.transform.SetParent(_launcher.FirePoint, false);

        _beamMaterial = new Material(Shader.Find("Sprites/Default"));
        _beam = go.AddComponent<LineRenderer>();
        _beam.sharedMaterial = _beamMaterial;
        _beam.useWorldSpace = true;
        _beam.positionCount = 2;
        _beam.startWidth = BeamWidth;
        _beam.endWidth = BeamWidth * 0.6f;
        _beam.startColor = BeamColor;
        _beam.endColor = BeamColor;
        _beam.enabled = false;
    }

    private async UniTaskVoid ShowBeamAsync(Vector3 hitPoint)
    {
        if (_beam == null)
            return;

        int version = ++_beamVersion;
        _beam.SetPosition(0, _launcher.FirePoint.position);
        _beam.SetPosition(1, hitPoint);
        _beam.enabled = true;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(BeamVisibleTime), cancellationToken: _disposeCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // 그사이 다음 빔이 나갔으면 그 빔이 끄도록 둔다.
        if (version == _beamVersion && _beam != null)
            _beam.enabled = false;
    }

    public override void Dispose()
    {
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();

        if (_beamMaterial != null)
            UnityEngine.Object.Destroy(_beamMaterial);

        base.Dispose();
    }
}
