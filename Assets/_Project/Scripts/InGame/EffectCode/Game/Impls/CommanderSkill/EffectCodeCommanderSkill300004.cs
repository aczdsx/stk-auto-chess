using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

// 3*3 범위 내에 위치한 적의 체력 아군 평균 공격력의 {0}%만큼 회복시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300004)]
    public class EffectCodeCommanderSkill300004 : EffectCodeGameBase
    {
        private ObfuscatorInt _tileID;
        private ObfuscatorFloat _time;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _time = codeInfo.GetCodeStatToFloat(1);

            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _time = codeInfo.GetCodeStatToFloat(1);

            SkillAction();
        }

        private void SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_04,
                inGameTile.View.CachedTr.position);

            if (inGameTile.OccupiedCharacter != null)
            {
                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = 2.0f;
                eccStats[1] = _time;
                eccStats[2] = inGameTile.View.ID;

                long effectCodeID = (long) EffectCodeNameType.BOUND;
                var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
                inGameTile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);
            }
        }
    }
}
