using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 암살자 타입 캐릭터 스테이지 시작시 뒤로 이동
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodePassiveAmbush : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_AMBUSH;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
        }

        public override void OnCombatStart()
        {
            base.OnCombatStart();
            if (InGameManager.Instance.IsBlockAmbush)
            {
                return;
            }
            owner.AddNextState<CharacterStateAssassinFirstMove>(null);

        }
    }
}
