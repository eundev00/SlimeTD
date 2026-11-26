using System;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UnityEngine;

public class CheatService : ICheatService
{
    private readonly IGameServerConnection _connection;
    private readonly IAccountService _accountService;

    public CheatService(IGameServerConnection connection, IAccountService accountService)
    {
        _connection = connection;
        _accountService = accountService;
    }

    public async UniTask<ResultCode> RequestAsync(CheatType type)
    {
        if (!_accountService.IsInitialized)
            return ResultCode.NotLoggedIn;

        try
        {
            var res = await _connection.RequestAsync(MsgId.CheatReq, new CheatReq { Type = type }, MsgId.CheatRes, CheatRes.Parser);
            if (res.Result == ResultCode.Ok)
            {
                _accountService.ApplyItems(res.Items);
                _accountService.SetEnergyRecoverAt(res.EnergyRecoverAtMs);
            }
            else
            {
                Debug.Log($"[CheatService] 치트 실패: {type}, {res.Result}");
            }

            return res.Result;
        }
        catch (Exception e)
        {
            Debug.Log($"[CheatService] 치트 요청 실패: {e.Message}");
            return ResultCode.ServerError;
        }
    }
}
