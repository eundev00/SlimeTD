using UnityEngine;

public interface ISlime
{
    SlimeStats Stats { get; }
    Vector3 Position { get; }
    void TakeDamage(int damage);
}
