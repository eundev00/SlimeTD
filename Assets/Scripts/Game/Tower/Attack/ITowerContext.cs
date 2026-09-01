using Services.PoolService;
using UnityEngine;

public interface ITowerContext
{
    Transform Transform { get; }
    TowerStats Stats { get; }
    IGameObjectPoolService Pool { get; }
    TowerAnimator Animator { get; }
}
