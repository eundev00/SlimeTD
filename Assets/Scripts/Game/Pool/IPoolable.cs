/// 오브젝트 풀에서 꺼내거나 반환할 때 호출되는 인터페이스
public interface IPoolable
{
    void OnGetFromPool();
    void OnReturnToPool();
}
