using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

[UseEffectCodeIds((int)EffectCodeNameType.VIEW_SCALE_UP)]
public partial class EffectCodeStatIncreaseViewScale : EffectCodeCharacterBase
{
    private ObfuscatorFloat _increment;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _increment = codeInfo.GetCodeStatToFloat(0);
        owner.AddViewScaleFactor(_increment);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        //일반적인 게임에서는 로그 형태로 스케일이 커질 필요가 있지만 현재는 덮어쓰는것으로 처리.
        base.Merge(codeInfo, source);
        float addValue = codeInfo.GetCodeStatToFloat(0);
        owner.AddViewScaleFactor(addValue);
        _increment += addValue;
    }

    public override void OnPreRemoved()
    {
        owner.AddViewScaleFactor(-_increment);
        base.OnPreRemoved();
    }
}
