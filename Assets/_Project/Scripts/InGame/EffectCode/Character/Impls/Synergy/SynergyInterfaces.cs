namespace CookApps.AutoBattler
{
    public interface ISpecSynergyData
    {
        /// #SheetIndex
        int Index { get; }
        /// 시너지 그룹 ID
        int synergy_group_id { get; }
        /// 시너지 타입
        SynergyType synergy_type { get; }
        /// 시너지 네임 토큰
        string name_token { get; }
        /// 시너지 효과토큰
        string desc_token_1 { get; }
        /// 시너지 효과토큰
        string desc_token_2 { get; }
        /// 시너지 단계
        int grade { get; }
        /// 대상 인원
        int min_int { get; }
        /// 최대 인원
        int max_int { get; }
        /// 이펙트 트리거
        string effect_triger { get; }
        /// 시너지 적용 범위
        SynergyCoverType synergy_cover_type { get; }

        /// 효과 타입
        SkillValueType effect_value_type_1 { get; }
        /// 스킬밸류
        int effect_stat_value_1 { get; }
        /// 효과 타입
        SkillValueType effect_value_type_2 { get; }
        /// 밸류
        int effect_stat_value_2 { get; }

        SkillValueType effect_value_type_3 { get; }
        /// 스킬밸류3
        int effect_stat_value_3 { get; }


    }

    public partial class SynergyElemental : ISpecSynergyData
    {
        int ISpecSynergyData.Index => Index;
        int ISpecSynergyData.synergy_group_id => synergy_group_id;
        SynergyType ISpecSynergyData.synergy_type => synergy_type;
        string ISpecSynergyData.name_token => name_token;
        string ISpecSynergyData.desc_token_1 => desc_token_1;
        string ISpecSynergyData.desc_token_2 => desc_token_2;
        int ISpecSynergyData.grade => grade;
        int ISpecSynergyData.min_int => min_int;
        int ISpecSynergyData.max_int => max_int;
        string ISpecSynergyData.effect_triger => effect_triger;
        SynergyCoverType ISpecSynergyData.synergy_cover_type => synergy_cover_type;

        SkillValueType ISpecSynergyData.effect_value_type_1 => effect_value_type01;
        int ISpecSynergyData.effect_stat_value_1 => effect_stat_value01;

        SkillValueType ISpecSynergyData.effect_value_type_2 => effect_value_type02;
        int ISpecSynergyData.effect_stat_value_2 => effect_stat_value02;
        SkillValueType ISpecSynergyData.effect_value_type_3 => 0;
        int ISpecSynergyData.effect_stat_value_3 => 0;
    }

    public partial class SynergyStarAsterism : ISpecSynergyData
    {
        int ISpecSynergyData.Index => Index;
        int ISpecSynergyData.synergy_group_id => synergy_group_id;
        SynergyType ISpecSynergyData.synergy_type => synergy_type;
        string ISpecSynergyData.name_token => name_token;
        string ISpecSynergyData.desc_token_1 => desc_token_1;
        string ISpecSynergyData.desc_token_2 => desc_token_2;
        int ISpecSynergyData.grade => grade;
        int ISpecSynergyData.min_int => min_int;
        int ISpecSynergyData.max_int => max_int;
        string ISpecSynergyData.effect_triger => effect_triger;
        SynergyCoverType ISpecSynergyData.synergy_cover_type => synergy_cover_type;
        SkillValueType ISpecSynergyData.effect_value_type_1 => effect_value_type;
        int ISpecSynergyData.effect_stat_value_1 => effect_stat_value;

        SkillValueType ISpecSynergyData.effect_value_type_2 => effect_value_type_2;
        int ISpecSynergyData.effect_stat_value_2 => effect_stat_value_2;
        
        SkillValueType ISpecSynergyData.effect_value_type_3 => effect_value_type_3;
        int ISpecSynergyData.effect_stat_value_3 => effect_stat_value_3;
    }
}