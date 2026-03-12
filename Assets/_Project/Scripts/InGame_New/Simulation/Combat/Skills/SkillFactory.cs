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

                // 커스텀 스킬이 이미 등록되어 있으면 스킵
                if (_registry.ContainsKey(id)) continue;

                // Custom 아키타입인데 아직 미등록이면 스킵
                if (archetype == SimSkillArchetype.Custom) continue;

                Register(id, () => SkillSpecAdapter.CreateFromArchetype(archetype));
            }
        }

        private static void RegisterCustomSkills()
        {
            Register(215252102, () => new SimSkillYuniHeal());
            Register(217433302, () => new SimSkillMinoProjectile());
            Register(217363204, () => new SimSkillVeinBounce());
            Register(217413301, () => new SimSkillTetoraKnockback());
            Register(215422301, () => new SimSkillMenshaShield());
            Register(217553404, () => new SimSkillClayChannel());
            Register(217563405, () => new SimSkillMarieAssassin());
            Register(217653505, () => new SimSkillEnkiWaveHeal());
            Register(217333202, () => new SimSkillAprilBarrage());
            Register(217613501, () => new SimSkillOdetteStrike());
            Register(217263103, () => new SimSkillRukidaFoxfire());
        }

        /// <summary>팩토리 등록 해제 (테스트용)</summary>
        public static void Clear()
        {
            _registry.Clear();
            _paramsCache.Clear();
            _initialized = false;
        }
    }
}
