using System.Collections.Generic;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 팩토리. SkillId → SimSkillBase 인스턴스 생성.
    /// Reflection 금지 원칙에 따라 수동 등록.
    /// </summary>
    public static class SkillFactory
    {
        private static readonly Dictionary<int, System.Func<SimSkillBase>> _registry = new();
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

        /// <summary>수동 등록 초기화. 실제 스킬 ID는 SkillActive 테이블 기반으로 추후 매핑.</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // 패턴별 기본 클래스 등록 (스킬 ID 범위는 추후 SkillActive 테이블에서 결정)
            // 예시: Register(1001, () => new SimSkillSingleDamage());
        }

        /// <summary>팩토리 등록 해제 (테스트용)</summary>
        public static void Clear()
        {
            _registry.Clear();
            _initialized = false;
        }
    }
}
