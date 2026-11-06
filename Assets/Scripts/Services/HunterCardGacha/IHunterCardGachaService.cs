using Cysharp.Threading.Tasks;

public interface IHunterCardGachaService
{
    UniTask<HunterCardGachaConfig> GetConfigAsync();

    // 골드가 부족하거나 뽑을 헌터가 없으면 null이다. 결과는 반환 전에 지급과 저장까지 끝난다.
    UniTask<HunterCardGachaResult> DrawAsync(int packId);
}
