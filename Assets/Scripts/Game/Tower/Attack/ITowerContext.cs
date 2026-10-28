using Services.PoolService;
using UnityEngine;

public interface ITowerContext
{
    int SpawnId { get; }
    Transform Transform { get; }
    Vector3 AimDirection { get; }
    TowerStats Stats { get; }
    IGameObjectPoolService Pool { get; }
    TowerAnimator Animator { get; }
    TowerSkillController Skills { get; }

    void AddJelly(int amount);
}
