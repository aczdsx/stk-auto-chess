using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300002)]
    public class EffectCodeCommanderSkill300003 : EffectCodeGameBase
    {
        private ObfuscatorInt _tileID;
        private ObfuscatorFloat _damageRate;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _tileID = codeInfo.GetCodeStatToInt(0);
            _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;

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
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(inGameTile, 1);
            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_02,
                    tile.View.CachedTr.position);

                if (tile.OccupiedCharacter != null)
                {
                    if (tile.OccupiedCharacter.AllianceType == AllianceType.Player)
                    {
                        var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
                        if (playerCharacterList != null && playerCharacterList.Count > 0)
                        {
                            var strongestCharacter = playerCharacterList.OrderByDescending(c => c.AD).First();

                            CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                            damageInfo.damageAmount = damageInfo.damageAmount =
                                (int) Math.Ceiling(strongestCharacter.AD * _damageRate);

                            tile.OccupiedCharacter.GetHealed(damageInfo.damageAmount, null, codeId);
                        }
                    }
                }
            }
        }
    }
}
