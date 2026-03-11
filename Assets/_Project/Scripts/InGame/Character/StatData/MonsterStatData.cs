using System;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Google.Protobuf.Collections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 몬스터 전용 StatData 클래스
    /// CharacterStatData를 상속받아 몬스터에 맞게 동작을 변경
    /// </summary>
    public class MonsterStatData : CharacterStatData
    {
        public MonsterStatData(int characterId, int level, IEnumerable<EffectCodeInfo> globalEffectCodeInfos = null)
            : base(characterId, level, globalEffectCodeInfos)
        {
        }

        public MonsterStatData(int characterId, int level, float multiAd, float multiHp, IEnumerable<EffectCodeInfo> globalEffectCodeInfos = null)
            : base(characterId, level, multiAd, multiHp, globalEffectCodeInfos)
        {
        }

        /// <summary>
        /// 몬스터는 ElpisCoreLabs 보너스를 적용하지 않음
        /// </summary>
        protected override void InjectFixedValueByElpisCoreLabs()
        {
            // 몬스터는 ElpisCoreLabs 보너스 적용 안함
        }

        /// <summary>
        /// 몬스터 전용 레벨 보너스 계산
        /// 돌파/초월 없이 레벨 보너스만 적용
        /// </summary>
        protected override double CalculateLevelBonusRate(int level)
        {
            // 몬스터는 레벨 보너스만 적용 (돌파/초월 없음)
            var levelMultiplier = (1f + Spec.inc_lv_rate * (level - 1)) * (1f + Spec.inc_lv_bonus_rate * Mathf.FloorToInt((level - 1) * 0.1f));
            
            return levelMultiplier - 1f;
        }
    }
}
