public interface ITowerLevelService
{
    int GetLevel(int towerId);
    void SetLevel(int towerId, int level);
    void SetDefaultLevel(int level);
}