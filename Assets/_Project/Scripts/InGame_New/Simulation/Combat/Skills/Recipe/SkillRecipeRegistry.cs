using System.Collections.Generic;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 ID → SkillRecipe 매핑 레지스트리.
    /// 모든 Recipe는 static readonly — deserialize 비용 0.
    /// SkillFactory에서 참조하여 SimSkillGeneric을 생성.
    ///
    /// partial class로 분리:
    /// - SkillRecipeRegistry.cs — 코어 + Builder 헬퍼
    /// - SkillRecipeRegistry.Archetypes.cs — 아키타입 Recipe
    /// - SkillRecipeRegistry.Character.cs — 플레이어 스킬 Recipe
    /// - SkillRecipeRegistry.Monster.cs — 몬스터 스킬 Recipe
    /// </summary>
    public static partial class SkillRecipeRegistry
    {
        private static readonly Dictionary<int, SkillRecipe> _recipes = new();

        /// <summary>스킬 ID로 Recipe 조회</summary>
        public static bool TryGet(int skillGroupId, out SkillRecipe recipe)
        {
            return _recipes.TryGetValue(skillGroupId, out recipe);
        }

        // 아키타입별 기본 Recipe
        private static readonly Dictionary<SimSkillArchetype, SkillRecipe> _archetypeRecipes = new();

        /// <summary>아키타입으로 Recipe 조회</summary>
        public static bool TryGetByArchetype(SimSkillArchetype archetype, out SkillRecipe recipe)
        {
            return _archetypeRecipes.TryGetValue(archetype, out recipe);
        }

        static SkillRecipeRegistry()
        {
            RegisterArchetypeRecipes();
            RegisterPlayerRecipes();
            RegisterMonsterRecipes();
        }

        // ── Builder 헬퍼 ──

        /// <summary>스킬 Recipe Builder 시작. Register()로 _recipes에 자동 등록.</summary>
        private static SkillRecipeBuilder Skill(int skillId, SkillExecutionType exec, SkillTargetType target)
            => new SkillRecipeBuilder(_recipes, skillId, exec, target);

        /// <summary>아키타입 Recipe 등록</summary>
        private static void DefineArchetype(SimSkillArchetype archetype, SkillRecipe recipe)
            => _archetypeRecipes[archetype] = recipe;

        /// <summary>아키타입 Recipe Builder. Build()로 SkillRecipe 반환, DefineArchetype()으로 등록.</summary>
        private static SkillRecipeBuilder ArchetypeBuilder(SkillExecutionType exec, SkillTargetType target)
            => new SkillRecipeBuilder(null, 0, exec, target);
    }
}
