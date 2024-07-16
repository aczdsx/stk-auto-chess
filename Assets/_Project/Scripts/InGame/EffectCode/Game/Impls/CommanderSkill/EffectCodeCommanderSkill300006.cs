using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;

//3*3범위 내의 적의 공격력을 {0}초 동안 {1}% 감소 시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300006)]
    public class EffectCodeCommanderSkill300006 : EffectCodeGameBase
    {
        private ObfuscatorInt _tileID;
        private ObfuscatorFloat _powerRate;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;

            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;

            SkillAction();
        }

        private void SkillAction()
        {
            InGameCommanderManager.Instance.InGameCamera.ShakeCamera(0.4f, 0.15f);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(inGameTile);

            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_05,
                    tile.View.CachedTr.position);

                if (tile.OccupiedCharacter != null)
                {
                    if (tile.OccupiedCharacter.AllianceType == AllianceType.Enemy)
                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = codeId;
                        eccStats[1] = 3;
                        eccStats[2] = _powerRate;

                        long effectCodeID = (long)EffectCodeNameType.BUFF_AD_PERCENT_UP;
                        var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
                        tile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);
                    }
                }
            }
        }
    }
}
