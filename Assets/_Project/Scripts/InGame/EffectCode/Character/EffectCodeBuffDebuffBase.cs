using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    public abstract class EffectCodeBuffDebuffBase : EffectCodeCharacterBase
    {
        public virtual bool Removable => true;
        public override EffectCodeType Type => EffectCodeType.Character;

        public virtual bool IsNeedToShowIcon()
        {
            // 영구적으로 걸리는 버프/디버프는 아이콘을 표시하지 않아야한다면 주석을 해제해주세요.
            // if (!Removable)
            //     return false;

            var src = Source as CharacterController;
            if (src != null)
            {
                return true;
            }

            // src가 CharacterController가 아닌 경우(버프나 디버프를 주는게 캐릭터가 아닌 경우)가 있고, 이 경우에도 아이콘을 표시해야한다면 true를 반환하도록 구현해주세요.

            return false;
        }

        // public virtual string GetBuffIconName()
        // {
        //     var src = Source as CharacterController;
        //     if (src != null)
        //     {
        //         var specCharacter = SpecDataManager.Instance.GetCharacterData(src.CharacterId);
        //         // 상건님 여기서 아이콘 이름을 반환하도록 구현해주세요~
        //
        //         return string.Empty;
        //     }
        //
        //     // src가 CharacterController가 아닌 경우(버프나 디버프를 주는게 캐릭터가 아닌 경우)가 있고, 이 경우에도 아이콘을 표시해야한다면 아이콘 이름을 반환하도록 구현해주세요.
        //
        //     return string.Empty;
        // }
    }


    public abstract class EffectCodeBuffBase : EffectCodeBuffDebuffBase
    {
        public override EffectCodeType Type => EffectCodeType.Buff;
    }

    public abstract class EffectCodeDebuffBase : EffectCodeBuffDebuffBase
    {
        public override EffectCodeType Type => EffectCodeType.Debuff;
    }
}
