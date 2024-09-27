

[UseEffectCodeIds(1)]
public partial class Test1 : CookApps.BattleSystem.EffectCodeStatBase
{
    public override void Test22()
    {
        base.Test22();
        
    }
}


[UseEffectCodeIds(2)]
public partial class Test111 : EffectCodeCharBase
{
    public override void Test22()
    {
        base.Test22();
        
    }

    public override void Test33()
    {
        base.Test33();
    }
}


namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(3)]
    public partial class Test111 : EffectCodeCharBase
    {
        public override void Test22()
        {
            base.Test22();
            
        }

        public override void Test33()
        {
            base.Test33();
        }
    }
}
