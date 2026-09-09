using Services.PoolService;
using UnityEngine;

public interface ITowerContext
{
    Transform Transform { get; }
    Vector3 AimDirection { get; }
    TowerStats Stats { get; }
    IGameObjectPoolService Pool { get; }
    TowerAnimator Animator { get; }
}
