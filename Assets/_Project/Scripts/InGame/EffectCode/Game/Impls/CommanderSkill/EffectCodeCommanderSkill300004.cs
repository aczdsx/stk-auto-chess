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
        private ObfuscatorFloat _damageRate;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _damageRate = codeInfo.GetCodeStatToFloat(1);

            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _damageRate = codeInfo.GetCodeStatToFloat(1);

            SkillAction();
        }

        private void SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(inGameTile);
            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_01,
                    tile.View.CachedTr.position);

                if (tile.OccupiedCharacter != null)
                {
                    if(tile.OccupiedCharacter.AllianceType == AllianceType.Enemy)
                    {
                        var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
                        if (playerCharacterList != null && playerCharacterList.Count > 0)
                        {
                            var strongestCharacter = playerCharacterList.OrderByDescending(c => c.AD).First();

                            CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                            damageInfo.damageAmount = strongestCharacter.AD * _damageRate;

                            tile.OccupiedCharacter.GetHealed(damageInfo.damageAmount, null, codeId);
                        }
                    }
                }
            }
        }
    }
}
