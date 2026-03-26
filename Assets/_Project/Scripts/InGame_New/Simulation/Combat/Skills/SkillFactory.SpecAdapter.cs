using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillActive 스펙 → SkillParams 변환.
    /// 공통 파라미터 추출(PowerPercent, DamageType, CooldownSeconds, SkillHitTimes, FaceTarget)만 담당.
    /// 아키타입 분류/기본값 설정은 제거됨 — 모든 스킬이 개별 Recipe에서 구조를 정의.
    /// </summary>
    public static partial class SkillFactory
    {
        // ──────────────────────────────────────────────
        // BuildParams — 스펙 → SkillParams 변환
        // ──────────────────────────────────────────────

        /// <summary>SkillActive 스펙에서 SkillParams 생성 (단일 스펙)</summary>
        private static SkillParams BuildParams(SkillActive spec, int tickRate)
        {
            return BuildParams(spec, null, tickRate);
        }

        /// <summary>SkillActive 스펙에서 SkillParams 생성 (전체 스펙 리스트)</summary>
        private static SkillParams BuildParams(SkillActive spec, List<SkillActive> specList, int tickRate)
        {
            var dmgType = spec.atk_type == AtkType.AP ? DamageType.Magical : DamageType.Physical;

            // specList[0]은 쿨타임 — PERCENT row를 찾아 데미지 배율로 사용
            int powerPercent = 0;
            if (specList != null)
            {
                for (int i = 1; i < specList.Count; i++)
                {
                    if (specList[i].skill_value_type == SkillValueType.PERCENT)
                    {
                        powerPercent = Mathf.RoundToInt(specList[i].base_rate);
                        break;
                    }
                }
            }

            var p = new SkillParams
            {
                SkillId = spec.skill_group_id,
                PowerPercent = powerPercent > 0 ? powerPercent : 200,
                DamageType = dmgType,
                CastFrames = 0,
                TargetCount = 1,
                HitCount = 1,
                TargetType = SkillTargetType.NearestEnemy,
                WorldTickRate = tickRate,
                CooldownSeconds = specList != null && specList.Count > 0
                    ? specList[0].base_rate : 0f,
            };

            // SKL 클립에서 Execute 이벤트 타이밍 자동 추출
            ExtractSkillHitTimes(ref p, spec.prefab_id, tickRate);

            // Recipe의 TargetRule로 TargetType/FaceTarget 확정
            if (TryGetRecipe(spec.skill_group_id, out var recipe))
            {
                p.TargetType = recipe.TargetRule;
                p.FaceTarget = recipe.TargetRule != SkillTargetType.Self
                    && recipe.TargetRule != SkillTargetType.LowestHPAlly;
            }

            return p;
        }

        // ──────────────────────────────────────────────
        // SKL 애니메이션 키프레임 추출
        // ──────────────────────────────────────────────

        private static void ExtractSkillHitTimes(ref SkillParams p, int prefabId, int tickRate)
        {
            if (prefabId <= 0) return;

            // Back_SKL 기준 (시뮬레이션은 방향 무관, Back 사용)
            int sklKey = AnimKeyframeData.MakeKey(prefabId, false, AnimClipType.SKL);

            if (AnimKeyframeData.ClipEvents.TryGetValue(sklKey, out var events))
            {
                // Execute 이벤트만 필터링
                int count = 0;
                for (int i = 0; i < events.Length; i++)
                    if (events[i].key >= AnimationEventKey.Execute1Per1 &&
                        events[i].key <= AnimationEventKey.Execute1Per12)
                        count++;

                if (count > 0)
                {
                    p.SkillHitFrames = new int[count];
                    int idx = 0;
                    for (int i = 0; i < events.Length; i++)
                        if (events[i].key >= AnimationEventKey.Execute1Per1 &&
                            events[i].key <= AnimationEventKey.Execute1Per12)
                            p.SkillHitFrames[idx++] = SecondsToFrames(events[i].time, tickRate);
                }
            }

            if (AnimKeyframeData.ClipLengths.TryGetValue(sklKey, out float len))
                p.SkillClipFrames = SecondsToFrames(len, tickRate);
        }

        // ──────────────────────────────────────────────
        // 유틸리티
        // ──────────────────────────────────────────────

        private static int SecondsToFrames(float seconds, int tickRate)
        {
            return (int)(seconds * tickRate + 0.5f);
        }

        private static float GetSpecRate(List<SkillActive> specList, int index, float fallback = 0f)
        {
            if (specList != null && index < specList.Count)
                return specList[index].base_rate;
            return fallback;
        }
    }
}
