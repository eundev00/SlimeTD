using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

public interface IHunterCardGachaService
{
    UniTask<HunterCardGachaConfig> GetConfigAsync();

    // 서버가 확률을 굴리고 지급까지 끝낸다. 실패하면 결과는 null이다
    UniTask<(ResultCode Result, HunterCardGachaResult Gacha)> DrawAsync(int packId);
}