using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UnityEngine;

public class HunterCardGachaService : IHunterCardGachaService
{
    private readonly IAccountService _accountService;
    private readonly IResourceLoadService _resourceLoadService;

    private HunterCardGachaConfig _config;

    public HunterCardGachaService(IAccountService accountService, IResourceLoadService resourceLoadService)
    {
        _accountService = accountService;
        _resourceLoadService = resourceLoadService;
    }

    public async UniTask<HunterCardGachaConfig> GetConfigAsync()
    {
        if (_config != null)
            return _config;

        _config = await _resourceLoadService.LoadAsync<HunterCardGachaConfig>(DataKeys.HunterCardGachaConfig);
        if (_config == null)
            Debug.Log($"[HunterCardGachaService] {DataKeys.HunterCardGachaConfig} 로드에 실패했습니다.");

        return _config;
    }

    public async UniTask<(ResultCode Result, HunterCardGachaResult Gacha)> DrawAsync(int packId)
    {
        var config = await GetConfigAsync();
        int price = config != null && config.TryGetPack(packId, out var pack) ? pack.Price : 0;

        var (result, cards) = await _accountService.DrawHunterCardGachaAsync(packId);
        if (result != ResultCode.Ok)
            return (result, null);

        return (result, new HunterCardGachaResult(packId, price, cards));
    }
}