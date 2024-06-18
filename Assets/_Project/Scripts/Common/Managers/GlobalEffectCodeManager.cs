using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using UnityEngine.Pool;

public enum GlobalEffectCodeSource
{
}

public class GlobalEffectCodeManager : Singleton<GlobalEffectCodeManager>, IEffectCodeSource
{
    public void Initialize()
    {
        eccForGame = new EffectCodeContainer(this);
    }

    public void Clear()
    {
        eccForGame.Clear();
        eccForGame = null;
    }

    /// <summary>
    /// Action의 두번째 인자는 effectCodeId
    /// </summary>
    public static event Action<GlobalEffectCodeSource, long> OnEffectCodeChanged;

    /// <summary>
    /// 전역으로 등록되는 EffectCode들
    /// </summary>
    private Dictionary<(GlobalEffectCodeSource src, long codeId), EffectCodeInfo> globalEffectCodes = new ();

    /// <summary>
    /// 인게임 안에서 사용되는 Type이 EffectCodeType.Game인 특수한 effectCode들
    /// </summary>
    private EffectCodeContainer eccForGame;

    public List<EffectCodeBase> GetGameEffectCodes()
    {
        return eccForGame.GetEffectCodesByType(EffectCodeType.Game);
    }

    /// <summary>
    /// 전역으로 동작하는 effectCode를 추가하거나 업데이트한다.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="codeInfo"></param>
    public void AddOrUpdateEffectCode(GlobalEffectCodeSource source, EffectCodeInfo codeInfo)
    {
        var effectCode = eccForGame.AddOrMergeEffectCode(codeInfo, this);
        if (effectCode.Type == EffectCodeType.Game)
        {
            return;
        }

        eccForGame.RemoveEffectCode(effectCode);
        globalEffectCodes[(source, codeInfo.CodeId)] = codeInfo;
        OnEffectCodeChanged?.Invoke(source, codeInfo.CodeId);
    }

    public void RemoveEffectCode(GlobalEffectCodeSource source, int codeId)
    {
        if (!eccForGame.RemoveEffectCode(codeId, out var effectCode))
            return;

        if (effectCode?.Type == EffectCodeType.Game)
            return;

        if (!globalEffectCodes.Remove((source, codeId)))
            return;

        OnEffectCodeChanged?.Invoke(source, codeId);
    }

    public IEnumerable<EffectCodeInfo> GetAllGlobalEffectCodes() => globalEffectCodes.Values;

    public IEnumerable<EffectCodeInfo> GetAllGlobalEffectCodes(GlobalEffectCodeSource source)
    {
        foreach (var code in globalEffectCodes)
        {
            if (code.Key.Item1 == source)
                yield return code.Value;
        }
    }

    public EffectCodeInfo GetGlobalEffectCode(GlobalEffectCodeSource source, int codeId)
    {
        return globalEffectCodes.GetValueOrDefault((source, codeId));
    }
}
