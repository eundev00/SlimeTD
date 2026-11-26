using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TowerSkillVfx
{
    private readonly Dictionary<int, ParticleSystem> _effects = new Dictionary<int, ParticleSystem>();
    private readonly Dictionary<int, GameObject> _targetPrefabs = new Dictionary<int, GameObject>();

    // 타워가 스폰될 때 활성 스킬의 연출만 로드해 앵커 밑에 꺼 둔다. 같은 키는 ResourceLoadService가 캐시한다.
    // Target 앵커 연출은 맞은 슬라임 위치에 VfxPlayer 풀로 띄우므로 프리팹만 들고 있는다.
    public async UniTaskVoid LoadAsync(
        IReadOnlyList<TowerSkillData> skills,
        IResourceLoadService loader,
        TowerVfxAnchors anchors,
        Transform fallback,
        CancellationToken token)
    {
        var pending = new List<TowerSkillData>();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].HasVfx)
                pending.Add(skills[i]);
        }

        for (int i = 0; i < pending.Count; i++)
        {
            var skill = pending[i];
            var prefab = await loader.LoadAsync<GameObject>(skill.VfxKey);
            if (token.IsCancellationRequested || fallback == null)
                return;

            if (prefab == null)
            {
                Debug.Log($"[TowerSkillVfx] 연출 프리팹을 로드하지 못했습니다. 스킬: {skill.Id}, 키: {skill.VfxKey}");
                continue;
            }

            if (skill.VfxAnchor == VfxAnchor.Target)
            {
                _targetPrefabs[skill.Id] = prefab;
                continue;
            }

            var parent = anchors != null ? anchors.Get(skill.VfxAnchor) : fallback;
            var instance = Object.Instantiate(prefab, parent, false);

            var effect = instance.GetComponentInChildren<ParticleSystem>(true);
            if (effect == null)
            {
                Debug.Log($"[TowerSkillVfx] 연출 프리팹에 ParticleSystem이 없습니다: {skill.VfxKey}", instance);
                Object.Destroy(instance);
                continue;
            }

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effects[skill.Id] = effect;
        }
    }

    // 로드가 끝나기 전에 발동하면 그 한 번은 연출 없이 지나간다.
    public void Play(IReadOnlyList<TowerSkillData> skills)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (!_effects.TryGetValue(skills[i].Id, out var effect) || effect == null)
                continue;

            effect.Clear(true);
            effect.Play(true);
        }
    }

    public void PlayAt(IReadOnlyList<TowerSkillData> skills, Vector3 position, IVfxPlayer player)
    {
        if (player == null)
            return;

        for (int i = 0; i < skills.Count; i++)
        {
            if (_targetPrefabs.TryGetValue(skills[i].Id, out var prefab))
                player.Play(prefab, position);
        }
    }
}
