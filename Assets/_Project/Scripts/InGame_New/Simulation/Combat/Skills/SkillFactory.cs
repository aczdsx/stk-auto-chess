using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 팩토리. SkillId -> SimSkillBase 인스턴스 생성.
    /// Reflection 금지 원칙에 따라 수동 등록 + 스펙 기반 자동 등록.
    /// </summary>
    public static class SkillFactory
    {
        private static readonly Dictionary<int, System.Func<SimSkillBase>> _registry = new();
        private static readonly Dictionary<int, SkillParams> _paramsCache = new();
        private static readonly Dictionary<int, List<SkillActive>> _specListCache = new();
        private static bool _initialized;

        public static void Register(int skillId, System.Func<SimSkillBase> creator)
        {
            _registry[skillId] = creator;
        }

        public static SimSkillBase Create(int skillId)
        {
            if (_registry.TryGetValue(skillId, out var creator))
                return creator();
            return null;
        }

        /// <summary>캐시된 SkillParams 조회</summary>
        public static bool TryGetParams(int skillId, out SkillParams p)
        {
            return _paramsCache.TryGetValue(skillId, out p);
        }

        /// <summary>캐시된 SkillActive 스펙 리스트 조회 (커스텀 스킬 자체 파싱용)</summary>
        public static bool TryGetSpecList(int skillId, out List<SkillActive> specList)
        {
            return _specListCache.TryGetValue(skillId, out specList);
        }

        /// <summary>SkillActive 스펙 테이블 기반 자동 등록</summary>
        public static void Initialize(int tickRate)
        {
            if (_initialized) return;
            _initialized = true;

            // 커스텀 스킬 먼저 등록 (스펙 기반 등록에서 덮어쓰지 않도록)
            RegisterCustomSkills();

            var specManager = SpecDataManager.Instance;
            var allSkills = specManager?.SkillActive?.All;
            if (allSkills == null) return;

            for (int i = 0; i < allSkills.Count; i++)
            {
                var spec = allSkills[i];

                // PASSIVE, NONE 타입은 스킵
                if (spec.skill_type != SkillType.NORMAL &&
                    spec.skill_type != SkillType.WEAPON &&
                    spec.skill_type != SkillType.ACTIVE)
                    continue;

                int id = spec.skill_group_id;
                var archetype = SkillSpecAdapter.ClassifySkill(spec);

                // 같은 skill_group_id로 이미 등록된 경우 스킵 (성급별 중복 방지)
                if (_paramsCache.ContainsKey(id)) continue;

                var specList = specManager.GetSkillDataList(id);
                var skillParams = SkillSpecAdapter.BuildParams(spec, specList, tickRate);
                _paramsCache[id] = skillParams;
                if (specList != null)
                    _specListCache[id] = specList;

                // 커스텀 스킬이 이미 등록되어 있으면 스킵
                if (_registry.ContainsKey(id)) continue;

                // Custom 아키타입인데 아직 미등록이면 스킵
                if (archetype == SimSkillArchetype.Custom) continue;

                // Recipe가 있으면 SimSkillGeneric으로, 없으면 기존 아키타입 클래스로
                if (SkillRecipeRegistry.TryGetByArchetype(archetype, out var archetypeRecipe))
                {
                    var capturedRecipe = archetypeRecipe;
                    Register(id, () =>
                    {
                        var skill = new SimSkillGeneric();
                        skill.SetRecipe(capturedRecipe);
                        return skill;
                    });
                }
                else
                {
                    Register(id, () => SkillSpecAdapter.CreateFromArchetype(archetype));
                }
            }
        }

        private static void RegisterCustomSkills()
        {
            // ── 커스텀 실행 로직이 필요한 스킬 (전용 클래스 유지) ──
            // 각 스킬의 ParamSlots는 SkillRecipeRegistry에 정의되어 있으나,
            // Execute/OnChannelTick 로직이 복잡하여 SimSkillGeneric으로 대체 불가.
            Register(217433302, () => new SimSkillMinoProjectile());  // 미노: 순차 미사일 + 개별 도착 타이머
            Register(217363204, () => new SimSkillVeinBounce());      // 베인: 바운스 투사체 + 히트 추적
            Register(217413301, () => new SimSkillTetoraKnockback()); // 테토라: 넉백 + 벽 충돌 + 착지 AoE
            Register(217563405, () => new SimSkillMarieAssassin());   // 마리에: 텔레포트 + 순차 히트
            Register(217653505, () => new SimSkillEnkiWaveHeal());    // 엔키: 보드 스윕 투사체
            Register(217333202, () => new SimSkillAprilBarrage());    // 에이프릴: 확장 콘 + 거리별 배율
            Register(217613501, () => new SimSkillOdetteStrike());    // 오데트: 2단계 텔레포트 + 다른 범위
            Register(217523403, () => new SimSkillAdriaExpand());     // 아드리아: 3단계 확장 + 비트마스크
            Register(217663506, () => new SimSkillShirayukiAssassin()); // 시라유키: 순차 텔레포트 암살
            Register(217263103, () => new SimSkillRukidaFoxfire());   // 루키다: 마커 카운트 기반 동적 버프
            Register(217353203, () => new SimSkillRakiyuDebuff());    // 라키유: 투사체 도착 후 범위 디버프

            // ── Recipe 기반 스킬 (SimSkillGeneric으로 완전 대체) ──
            // Actions 데이터만으로 Execute/OnChannelTick 로직을 표현 가능한 스킬.
            RegisterRecipeSkills();
        }

        /// <summary>
        /// SkillRecipeRegistry에 등록된 스킬 중, 커스텀 클래스 미등록인 것만
        /// SimSkillGeneric으로 등록. Recipe의 Actions로 실행 로직을 데이터 기반 처리.
        /// </summary>
        private static void RegisterRecipeSkills()
        {
            int[] recipeSkillIds = {
                215532401, // 필리아: DelayedApply, Damage + 3단계 VFX + 마커
                217433303, // 하티: DelayedApply, Damage + Knockback + 3단계 VFX
                215252102, // 유니: DelayedApply, 최저HP 3명 Heal + RemoveDebuffs
                215422301, // 멘샤: DelayedApply, 같은 행 아군 Shield
                217323201, // 미사: DelayedApply, 최고공격력 적 Stun + 마커
                217553404, // 클레이: Channeling, Zone 힐+데미지+디버프
                215642501, // 엘리스: Channeling, 2단계 Diamond AoE
                230101005, // 몬스터 SingleProjectile
                230202004, // 몬스터 SingleProjectile
                215322201, // 메이: Plus AoE + 넉백 + 방어 버프
                250108001, // 보스탱커: 전방 10칸 순차 타격
            };

            for (int i = 0; i < recipeSkillIds.Length; i++)
            {
                int id = recipeSkillIds[i];
                if (_registry.ContainsKey(id)) continue;
                if (!SkillRecipeRegistry.TryGet(id, out var recipe)) continue;

                var capturedRecipe = recipe;
                Register(id, () =>
                {
                    var skill = new SimSkillGeneric();
                    skill.SetRecipe(capturedRecipe);
                    return skill;
                });
            }
        }

        /// <summary>팩토리 등록 해제 (테스트용)</summary>
        public static void Clear()
        {
            _registry.Clear();
            _paramsCache.Clear();
            _specListCache.Clear();
            _initialized = false;
        }
    }
}
