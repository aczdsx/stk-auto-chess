using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.BattleSystem;

public class GlobalEffectCodeManager : Singleton<GlobalEffectCodeManager>, IEffectCodeSource
{
    public void Initialize()
    {
    }

    public void Clear()
    {
        globalEffectCodes = new();
    }

    /// <summary>
    /// Action의 두번째 인자는 effectCodeId
    /// </summary>
    public static event Action<GlobalEffectProviderType, long> OnEffectCodeChanged;

    /// <summary>
    /// 전역으로 등록되는 EffectCode들
    /// </summary>
    private Dictionary<(GlobalEffectProviderType src, long codeId), EffectCodeInfo> globalEffectCodes = new ();

    /// <summary>
    /// 전역으로 동작하는 effectCode를 추가하거나 업데이트한다.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="codeInfo"></param>
    public void AddOrUpdateEffectCode(GlobalEffectProviderType source, EffectCodeInfo codeInfo)
    {
        globalEffectCodes[(source, codeInfo.CodeId)] = codeInfo;
        OnEffectCodeChanged?.Invoke(source, codeInfo.CodeId);
    }

    public void RemoveEffectCode(GlobalEffectProviderType source, int codeId)
    {
        if (!globalEffectCodes.Remove((source, codeId)))
            return;

        OnEffectCodeChanged?.Invoke(source, codeId);
    }

    public IEnumerable<EffectCodeInfo> GetAllGlobalEffectCodes() => globalEffectCodes.Values;

    public IEnumerable<EffectCodeInfo> GetAllGlobalEffectCodes(GlobalEffectProviderType source)
    {
        foreach (var code in globalEffectCodes)
        {
            if (code.Key.Item1 == source)
                yield return code.Value;
        }
    }

    public EffectCodeInfo GetGlobalEffectCode(GlobalEffectProviderType source, int codeId)
    {
        return globalEffectCodes.GetValueOrDefault((source, codeId));
    }
}
