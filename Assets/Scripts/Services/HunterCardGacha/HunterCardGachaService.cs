using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class HunterCardGachaService : IHunterCardGachaService
{
    private readonly IPlayerDataService _playerDataService;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly System.Random _random = new System.Random();

    private HunterCardGachaConfig _config;
    private HunterCardGachaRoller _roller;

    public HunterCardGachaService(IPlayerDataService playerDataService, IResourceLoadService resourceLoadService)
    {
        _playerDataService = playerDataService;
        _resourceLoadService = resourceLoadService;
    }

    public async UniTask<HunterCardGachaConfig> GetConfigAsync()
    {
        if (_config != null)
            return _config;

        _config = await _resourceLoadService.LoadAsync<HunterCardGachaConfig>(DataKeys.HunterCardGachaConfig);
        if (_config == null)
        {
            Debug.Log($"[HunterCardGachaService] {DataKeys.HunterCardGachaConfig} 로드에 실패했습니다.");
            return null;
        }

        _roller = new HunterCardGachaRoller(_config, _random);
        return _config;
    }

    public async UniTask<HunterCardGachaResult> DrawAsync(int packId)
    {
        var config = await GetConfigAsync();
        if (config == null || !_playerDataService.IsInitialized)
            return null;

        if (!config.TryGetPack(packId, out var pack))
        {
            Debug.Log($"[HunterCardGachaService] 팩을 찾을 수 없습니다: {packId}");
            return null;
        }

        if (_playerDataService.Gold.Value < pack.Price)
            return null;

        var owned = new List<HunterCardGachaCandidate>();
        var locked = new List<HunterCardGachaCandidate>();
        CollectCandidates(owned, locked);

        int pity = _playerDataService.HunterCardGachaPity;
        var picks = _roller.Roll(pack, owned, locked, ref pity);
        if (picks.Count == 0)
        {
            Debug.Log("[HunterCardGachaService] 뽑을 수 있는 헌터가 없습니다.");
            return null;
        }

        var cards = new List<HunterCardGachaCard>(picks.Count);
        for (int i = 0; i < picks.Count; i++)
            cards.Add(ToCard(picks[i], config));

        var result = new HunterCardGachaResult(pack.Id, pack.Price, cards, pity);
        return _playerDataService.TryApplyHunterCardGachaResult(result) ? result : null;
    }

    private void CollectCandidates(List<HunterCardGachaCandidate> owned, List<HunterCardGachaCandidate> locked)
    {
        var hunters = _playerDataService.Hunters;
        for (int i = 0; i < hunters.Count; i++)
        {
            var data = hunters[i].Data;
            if (data == null || !data.Enabled || !data.InGacha)
                continue;

            var candidate = new HunterCardGachaCandidate(data.Id, data.GradeId);
            if (hunters[i].Unlocked.Value)
                owned.Add(candidate);
            else
                locked.Add(candidate);
        }
    }

    // 최대 레벨 헌터의 카드는 카드 대신 골드로 바꿔 준다. 같은 팩 안에서는 레벨이 오르지 않으므로 지금 레벨로 판단한다.
    private HunterCardGachaCard ToCard(HunterCardGachaPick pick, HunterCardGachaConfig config)
    {
        if (pick.IsLocked)
            return new HunterCardGachaCard(pick.TowerId, pick.GradeId, HunterCardGachaCardKind.Unlock);

        if (_playerDataService.TryGetHunter(pick.TowerId, out var hunter) && hunter.Level.Value >= TowerLevelService.MaxLevel)
            return new HunterCardGachaCard(pick.TowerId, pick.GradeId, HunterCardGachaCardKind.Refund, config.MaxLevelRefundGold);

        return new HunterCardGachaCard(pick.TowerId, pick.GradeId, HunterCardGachaCardKind.Card);
    }
}
