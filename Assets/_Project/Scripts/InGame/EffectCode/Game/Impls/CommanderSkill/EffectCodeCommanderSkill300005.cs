using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;

//배치판 위의 아군과 적, 지형 지물 요소의 위치를 모두 랜덤하게 바꾼다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300005)]
    public class EffectCodeCommanderSkill300005 : EffectCodeGameBase
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

            SkillAction().Forget();
        }

        private async UniTaskVoid SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_06_01,
                inGameTile.View.CachedTr.position);

            if (inGameTile.OccupiedCharacter != null)
            {
                inGameTile.SetUnoccupied();

                await UniTask.Delay(TimeSpan.FromSeconds(0.5));

                var randomTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile();
                randomTile.SetOccupied(inGameTile.OccupiedCharacter);

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_06_02,
                    randomTile.View.CachedTr.position);
            }
        }
    }
}
