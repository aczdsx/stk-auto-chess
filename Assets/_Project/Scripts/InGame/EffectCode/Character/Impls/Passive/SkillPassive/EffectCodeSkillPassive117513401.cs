using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 아트레시아 패시브
    /// 범위: 자기 자신
    /// 스킬 사용 시 {0}회 적의 공격을 무시하는 보호막을 획득합니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117513401 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117513401;
        private int _shieldCount;
        private SkillPassive _specSkill;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _shieldCount = codeInfo.GetCodeStatToInt(0);
            _specSkill = base.GetSpecSkillPassive(CodeId);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _shieldCount = codeInfo.GetCodeStatToInt(0);
        }

        public override void OnSkill(EffectCodeBase skillEffectCode)
        {
            base.OnSkill(skillEffectCode);
            InjectNoDamageShield();
        }

        public void InjectNoDamageShield()
        {
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.passive_skill_vfxs[0], owner.SkillMiddleFXTransformFollowable);
            Span<double> buffStats = stackalloc double[3];
            buffStats.Clear();
            buffStats[0] = codeId;
            buffStats[1] = 999f;
            buffStats[2] = 3;
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SPECIAL_NO_DAMAGE_SHIELD, owner, buffStats, source);
        }

    }//117563405
}
