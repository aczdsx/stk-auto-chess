using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

// 십자범위 내 적에게 아군의 최고 공격력의 {0}%만큼의 대미지를 준다
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(300001)]
    public class EffectCodeCommanderSkill300001 : EffectCodeGameBase
    {
        private ObfuscatorInt _tileID;
        private ObfuscatorFloat _damageRate;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
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
            _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;

            SkillAction();
        }

        private void SkillAction()
        {
            InGameCommanderManager.Instance.InGameCamera.ShakeCamera(0.4f, 0.15f);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeXInRange(inGameTile, 2);

            List<int> targetCharacterList = new();
            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_01,
                    tile.View.CachedTr.position);

                if (tile.CheckValidTile(AllianceType.Player, false))
                {
                    if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                    {
                        targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                        var playerCharacterList = InGameObjectManager.Instance.GetCharacterListSortedByADDescending(AllianceType.Player, true);
                        if (playerCharacterList.Count > 0)
                        {
                            var strongestCharacter = playerCharacterList.First();

                            CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                            damageInfo.damageAmount = damageInfo.damageAmount =
                                (int) Math.Ceiling(strongestCharacter.AD * _damageRate);

                            tile.OccupiedCharacter.GetDamaged(damageInfo, null);
                        }
                    }
                }
            }
        }
    }
}
