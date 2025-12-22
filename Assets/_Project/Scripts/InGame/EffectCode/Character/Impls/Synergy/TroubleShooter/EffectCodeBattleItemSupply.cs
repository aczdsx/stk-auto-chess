using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Google.Protobuf.WellKnownTypes;
using Unity.Mathematics;
using UnityEditor.Localization.Plugins.XLIFF.V20;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]

    /// <summary>
    /// 배틀 아이템 공급
    /// 트러블슈터 시너지 사용 시 배틀아이템을 일정 시간마다 배포합니다.
    /// 
    /// </summary>
    public partial class EffectCodeBattleItemSupply : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.BATTLE_ITEM_SUPPLY;
        private float _duration;
        private float _elapsedTime;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _elapsedTime = 0f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _elapsedTime = 0f;
        }

        public override void OnUpdate(float dt)
        {
            _elapsedTime += dt;
            if (_elapsedTime >= _duration)
            {
                _elapsedTime = 0f;
                SupplyBattleItem();
            }
        }

        private void SupplyBattleItem()
        {
            var randomValue = InGameRandomManager.GetUniversalRandomValue(0, 4);
            int itemPrefabId = -1;
            switch (randomValue)
            {
                // case 0:
                //     itemPrefabId = (int)BattleItemSupplyType.EMP_BOMB;
                //     break;
                // case 1:
                //     itemPrefabId = (int)BattleItemSupplyType.ENERGY_DRINK;
                //     break;
                // case 2:
                //     itemPrefabId = (int)BattleItemSupplyType.VITAMIN;
                //     break;
                // case 3:
                //     itemPrefabId = (int)BattleItemSupplyType.CHOCOBAR;
                //     break;
                // case 4:
                //     itemPrefabId = (int)BattleItemSupplyType.EMERGENCY_ARMOR;
                //     break;
                default:
                    break;
            }
            var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(allianceType: AllianceType.Player);

            //1. 해당 캐릭터 ecc에 부여 후 2초 후 해당 캐릭터 ecc에서 해당 이펙트코드 제거
            //2. 여기서 2초 기다린 뒤에(여기서 배틀아이템 떨어뜨리기) 
            foreach (var character in playerCharacterList)
            {
                if (character.GetCharacterStat().Spec.character_stella_type == SynergyType.TROUBLESHOOTER)
                {
                    // character.GetEffectCodeContainer().AddEffectCode(new EffectCodeInfo((long)EffectCodeNameType.BATTLE_ITEM_SUPPLY, 0, new double[0]));
                }
            }




        }


    }
}
