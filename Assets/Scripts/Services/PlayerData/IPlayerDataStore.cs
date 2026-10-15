using Cysharp.Threading.Tasks;

// 저장 위치(로컬, 서버)를 숨기는 경계. 서버로 바꿀 때 이 인터페이스의 구현체만 교체한다.
public interface IPlayerDataStore
{
    UniTask<PlayerData> LoadAsync();
    UniTask SaveAsync(PlayerData data);
}