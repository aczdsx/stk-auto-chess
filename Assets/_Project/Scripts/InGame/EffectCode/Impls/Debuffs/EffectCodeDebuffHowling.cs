using CookApps.Obfuscator;
using CookApps.BattleSystem;

[UseEffectCodeIds(101021)]
public class EffectCodeDebuffHowling : EffectCodeCharacterBase
{
    private ObfuscatorFloat duration;
    private ObfuscatorFloat decreaseRate;

    private float elapsedTime;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(1);
        decreaseRate = codeInfo.GetCodeStatToFloat(2); // 이미 0.01f 곱해져서 들어옴
        elapsedTime = 0f;
        var stackCount = owner.AddBuffDebuffType(BuffDebuffType.DefenceDown);
        if (stackCount == 1)
        {
            var effect = InGameEffectManager.Instance.Get(BuffDebuffType.DefenceDown, owner.GetCharacterView().CachedTr);
            if (!ReferenceEquals(effect, null))
                owner.AddBuffDebuffEffectView(BuffDebuffType.DefenceDown, effect);
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        // 덮어 씌울 경우
        duration = codeInfo.GetCodeStatToFloat(1);
        decreaseRate = codeInfo.GetCodeStatToFloat(2); // 이미 0.01f 곱해져서 들어옴
        elapsedTime = 0f;
        // 더할 경우
        // duration += codeInfo.GetCodeStatToFloat(1);
        // decreaseRate = Mathf.Max(decreaseRate, codeInfo.GetCodeStatToFloat(2));
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (elapsedTime >= duration)
        {
            elapsedTime = 0f;
            RemoveFromContainer();
        }
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
        var (stackCount, effectView) = owner.RemoveBuffDebuffType(BuffDebuffType.DefenceDown);
        if (stackCount != 0)
            return;

        if (effectView == null)
            return;

        InGameEffectManager.Instance.RemoveInGameEffect(effectView as InGameEffectBase);
    }

    public override double GetIncrementPercentDEF()
    {
        return -decreaseRate;
    }
}
