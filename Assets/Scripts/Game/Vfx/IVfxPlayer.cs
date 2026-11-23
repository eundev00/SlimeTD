using UnityEngine;

public interface IVfxPlayer
{
    void Play(GameObject prefab, Vector3 position, float scale = 1f);
}