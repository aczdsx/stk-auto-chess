
namespace CookApps.BattleSystem
{
    public class EffectCodeBase
    {
    }

public abstract class EffectCodeStatBase :EffectCodeBase
{
    public virtual EffectCodeInheritFlag GetFlag()
    {
        return EffectCodeInheritFlag.None;
    }
    
    [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAD)]
    public virtual void Test22()
    {
        
    }
}
}


public class EffectCodeCharBase : CookApps.BattleSystem.EffectCodeStatBase
{
    [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackSpeed)]
    public virtual void Test33()
    {
    }
}
