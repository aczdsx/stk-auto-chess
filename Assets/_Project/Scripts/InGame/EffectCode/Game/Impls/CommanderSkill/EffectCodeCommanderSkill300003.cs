using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

//선택한 적 1명을 {0}초 동안 에어본한다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300003)]
    public class EffectCodeCommanderSkill300003 : EffectCodeGameBase
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

            if (inGameTile.OccupiedCharacter != null)
            {
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = codeId;
                    eccStats[1] = _time;
                    eccStats[2] = 1.0f;
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.TARGET_IMPOSSIBLE, inGameTile.OccupiedCharacter, eccStats, source);
                }
                
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = _time;
                    eccStats[1] = 4.0f;
                    eccStats[2] = inGameTile.View.ID;
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AIRBORNE, inGameTile.OccupiedCharacter, eccStats, source);
                }
                
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = codeId;
                    eccStats[1] = _time;
                    eccStats[2] = 0;
                    
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AIRBORNE, inGameTile.OccupiedCharacter, eccStats, source);
                }
            }
        }
    }
}
