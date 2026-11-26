using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

// 서버 개발 설정에서만 된다. 꺼져 있으면 NotAvailable이 온다
public interface ICheatService
{
    UniTask<ResultCode> RequestAsync(CheatType type);
}
