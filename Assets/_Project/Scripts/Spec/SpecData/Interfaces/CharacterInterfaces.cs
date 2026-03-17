namespace CookApps.AutoBattler
{
    /// <summary>
    /// CharacterInfo와 MonsterInfo가 공통으로 구현하는 인터페이스
    /// 두 클래스의 모든 공통 필드를 프로퍼티로 제공하여, CharacterInfo와 MonsterInfo를 동일한 방식으로 접근할 수 있게 합니다.
    /// </summary>
    public interface ISpecCharacterInfo
    {
        /// <summary>
        /// #SheetIndex
        /// </summary>
        int id { get; }

        /// <summary>
        /// 프리팹 ID
        /// </summary>
        int prefab_id { get; }

        /// <summary>
        /// 전체 리스트 출력 순서
        /// </summary>
        int seq { get; }

        /// <summary>
        /// 캐릭터 이름 토큰
        /// </summary>
        string name_token { get; }

        /// <summary>
        /// 캐릭터 설명 토큰
        /// </summary>
        string desc_token { get; }

        /// <summary>
        /// 개체 타입
        /// </summary>
        CharacterType character_type { get; }

        /// <summary>
        /// 속성 시너지
        /// </summary>
        SynergyType character_element_type { get; }

        /// <summary>
        /// 포지션 타입
        /// </summary>
        CharacterPositionType character_position_type { get; }

        /// <summary>
        /// 일반 공격 AP AD 판정 유무
        /// </summary>
        AtkType atk_type { get; }

        /// <summary>
        /// 사이즈
        /// </summary>
        int size { get; }

        /// <summary>
        /// 체력
        /// </summary>
        int stat_hp { get; }

        /// <summary>
        /// 공격력
        /// </summary>
        int stat_atk { get; }

        /// <summary>
        /// 체력, 공격력 1레벨 당 성장율
        /// </summary>
        float inc_lv_rate { get; }

        /// <summary>
        /// 체력, 공격력 10레벨 당 성장율
        /// </summary>
        float inc_lv_bonus_rate { get; }

        /// <summary>
        /// 물리 방어력
        /// </summary>
        int stat_def { get; }

        /// <summary>
        /// 물리 저항력
        /// </summary>
        float ad_reduce { get; }

        /// <summary>
        /// 마법 방어력
        /// </summary>
        float ap_reduce { get; }

        /// <summary>
        /// 물리 관통력
        /// </summary>
        float stat_atk_pierce { get; }

        /// <summary>
        /// 마법 관통력
        /// </summary>
        float stat_res_pierce { get; }

        /// <summary>
        /// 치명률
        /// </summary>
        float crit_rate { get; }

        /// <summary>
        /// 치명타 피해량
        /// </summary>
        float crit_power { get; }

        /// <summary>
        /// 공격 속도
        /// </summary>
        float atk_speed { get; }

        /// <summary>
        /// 이동 속도
        /// </summary>
        float move_speed { get; }

        /// <summary>
        /// 공격 범위
        /// </summary>
        int atk_range { get; }

        /// <summary>
        /// 힐파워
        /// </summary>
        float heal_power { get; }

        /// <summary>
        /// 이뮨 타입
        /// </summary>
        ImmuneType immune_type { get; }

        /// <summary>
        /// 높이
        /// </summary>
        float height { get; }

        /// <summary>
        /// 공격 범위 형태
        /// </summary>
        InGameVfxNameType projectile_vfx_name_type { get; }

        /// <summary>
        /// 일반 스킬 id
        /// </summary>
        int[] skill_ids { get; }

        /// <summary>
        /// 패시브 스킬 id
        /// </summary>
        int passive_skill_id { get; }

        /// <summary>
        /// 스킬 범위 이미지 이름 배열 (CharacterInfo 전용, 그 외 빈 배열 반환)
        /// 최대 2장 (예: ["skill_self_R2", "skill_square_R1_1"])
        /// </summary>
        string[] skill_range { get; }

        //===========================================================================
        /// <summary>
        /// 성군 시너지 타입 (CharacterInfo 전용, MonsterInfo는 NONE 반환)
        /// </summary>
        SynergyType character_stella_type { get; }
    }

    /// <summary>
    /// CharacterInfo의 ISpecCharacterInfo 인터페이스 구현
    /// CharacterInfo의 모든 공통 필드를 ISpecCharacterInfo 인터페이스를 통해 접근할 수 있도록 구현합니다.
    /// </summary>
    public partial class CharacterInfo : ISpecCharacterInfo
    {
        // ========================================
        // 공통 필드 (CharacterInfo, MonsterInfo 모두 존재)
        // ========================================
        int ISpecCharacterInfo.id => id;
        int ISpecCharacterInfo.prefab_id => prefab_id;
        int ISpecCharacterInfo.seq => seq;
        string ISpecCharacterInfo.name_token => name_token;
        string ISpecCharacterInfo.desc_token => desc_token;
        CharacterType ISpecCharacterInfo.character_type => character_type;
        SynergyType ISpecCharacterInfo.character_element_type => character_element_type;
        SynergyType ISpecCharacterInfo.character_stella_type => character_stella_type;
        CharacterPositionType ISpecCharacterInfo.character_position_type => character_position_type;
        AtkType ISpecCharacterInfo.atk_type => atk_type;
        int ISpecCharacterInfo.size => size;
        int ISpecCharacterInfo.stat_hp => stat_hp;
        int ISpecCharacterInfo.stat_atk => stat_atk;
        float ISpecCharacterInfo.inc_lv_rate => inc_lv_rate;
        float ISpecCharacterInfo.inc_lv_bonus_rate => inc_lv_bonus_rate;
        int ISpecCharacterInfo.stat_def => stat_def;
        float ISpecCharacterInfo.ad_reduce => ad_reduce;
        float ISpecCharacterInfo.ap_reduce => ap_reduce;
        float ISpecCharacterInfo.stat_atk_pierce => stat_atk_pierce;
        float ISpecCharacterInfo.stat_res_pierce => stat_res_pierce;
        float ISpecCharacterInfo.crit_rate => crit_rate;
        float ISpecCharacterInfo.crit_power => crit_power;
        float ISpecCharacterInfo.atk_speed => atk_speed;
        float ISpecCharacterInfo.move_speed => move_speed;
        int ISpecCharacterInfo.atk_range => atk_range;
        float ISpecCharacterInfo.heal_power => heal_power;
        ImmuneType ISpecCharacterInfo.immune_type => immune_type;
        float ISpecCharacterInfo.height => height;
        InGameVfxNameType ISpecCharacterInfo.projectile_vfx_name_type => projectile_vfx_name_type;
        int[] ISpecCharacterInfo.skill_ids => skill_ids;
        int ISpecCharacterInfo.passive_skill_id => passive_skill_id;
        string[] ISpecCharacterInfo.skill_range => skill_range;

        // ========================================
        // CharacterInfo 전용 필드 (인터페이스에 포함되지 않음)
        // ========================================
        // public int character_id; (GetId()로 처리됨)
        // public GradeType grade_type;
        // public int need_piece;
        // public SynergyType character_stella_type; (인터페이스에 포함됨)
        // public int init_star;
        // public int max_star;
        // public float inc_trancendence;
        // public float inc_exceed;
        // public int equipment_id;
        // public float weight;
    }

    /// <summary>
    /// MonsterInfo의 ISpecCharacterInfo 인터페이스 구현
    /// MonsterInfo의 모든 공통 필드를 ISpecCharacterInfo 인터페이스를 통해 접근할 수 있도록 구현합니다.
    /// </summary>
    public partial class MonsterInfo : ISpecCharacterInfo
    {
        // ========================================
        // 공통 필드 (CharacterInfo, MonsterInfo 모두 존재)
        // ========================================
        int ISpecCharacterInfo.id => id;
        int ISpecCharacterInfo.prefab_id => prefab_id;
        int ISpecCharacterInfo.seq => seq;
        string ISpecCharacterInfo.name_token => name_token;
        string ISpecCharacterInfo.desc_token => desc_token;
        CharacterType ISpecCharacterInfo.character_type => character_type;
        SynergyType ISpecCharacterInfo.character_element_type => character_element_type;
        SynergyType ISpecCharacterInfo.character_stella_type => SynergyType.NONE;
        CharacterPositionType ISpecCharacterInfo.character_position_type => character_position_type;
        AtkType ISpecCharacterInfo.atk_type => atk_type;
        int ISpecCharacterInfo.size => size;
        int ISpecCharacterInfo.stat_hp => stat_hp;
        int ISpecCharacterInfo.stat_atk => stat_atk;
        float ISpecCharacterInfo.inc_lv_rate => inc_lv_rate;
        float ISpecCharacterInfo.inc_lv_bonus_rate => inc_lv_bonus_rate;
        int ISpecCharacterInfo.stat_def => stat_def;
        float ISpecCharacterInfo.ad_reduce => ad_reduce;
        float ISpecCharacterInfo.ap_reduce => ap_reduce;
        float ISpecCharacterInfo.stat_atk_pierce => stat_atk_pierce;
        float ISpecCharacterInfo.stat_res_pierce => stat_res_pierce;
        float ISpecCharacterInfo.crit_rate => crit_rate;
        float ISpecCharacterInfo.crit_power => crit_power;
        float ISpecCharacterInfo.atk_speed => atk_speed;
        float ISpecCharacterInfo.move_speed => move_speed;
        int ISpecCharacterInfo.atk_range => atk_range;
        float ISpecCharacterInfo.heal_power => heal_power;
        ImmuneType ISpecCharacterInfo.immune_type => immune_type;
        float ISpecCharacterInfo.height => height;
        InGameVfxNameType ISpecCharacterInfo.projectile_vfx_name_type => projectile_vfx_name_type;
        int[] ISpecCharacterInfo.skill_ids => skill_ids;
        int ISpecCharacterInfo.passive_skill_id => passive_skill_id;
        string[] ISpecCharacterInfo.skill_range => System.Array.Empty<string>();

        // ========================================
        // MonsterInfo 전용 필드 (인터페이스에 포함되지 않음)
        // ========================================
        // public int monster_id; (GetId()로 처리됨)
        // public int stat_res;
        // public bool is_knock_back;
        // public bool is_taken_cc;
    }

    /// <summary>
    /// BattleItem의 ISpecCharacterInfo 인터페이스 구현
    /// BattleItem은 대부분의 필드를 기본값으로 반환합니다.
    /// </summary>
    public partial class BattleItem : ISpecCharacterInfo
    {
        int ISpecCharacterInfo.id => id;
        int ISpecCharacterInfo.prefab_id => prefab_id;
        int ISpecCharacterInfo.seq => 0;
        string ISpecCharacterInfo.name_token => name_token;
        string ISpecCharacterInfo.desc_token => desc_token;
        CharacterType ISpecCharacterInfo.character_type => character_type;
        SynergyType ISpecCharacterInfo.character_element_type => SynergyType.NONE;
        SynergyType ISpecCharacterInfo.character_stella_type => SynergyType.NONE;
        CharacterPositionType ISpecCharacterInfo.character_position_type => CharacterPositionType.NONE;
        AtkType ISpecCharacterInfo.atk_type => AtkType.NONE;
        int ISpecCharacterInfo.size => 0;
        int ISpecCharacterInfo.stat_hp => 0;
        int ISpecCharacterInfo.stat_atk => 0;
        float ISpecCharacterInfo.inc_lv_rate => 0;
        float ISpecCharacterInfo.inc_lv_bonus_rate => 0;
        int ISpecCharacterInfo.stat_def => 0;
        float ISpecCharacterInfo.ad_reduce => 0;
        float ISpecCharacterInfo.ap_reduce => 0;
        float ISpecCharacterInfo.stat_atk_pierce => 0;
        float ISpecCharacterInfo.stat_res_pierce => 0;
        float ISpecCharacterInfo.crit_rate => 0;
        float ISpecCharacterInfo.crit_power => 0;
        float ISpecCharacterInfo.atk_speed => 0;
        float ISpecCharacterInfo.move_speed => 0;
        int ISpecCharacterInfo.atk_range => 0;
        float ISpecCharacterInfo.heal_power => 0;
        ImmuneType ISpecCharacterInfo.immune_type => ImmuneType.NORMAL;
        float ISpecCharacterInfo.height => 0;
        InGameVfxNameType ISpecCharacterInfo.projectile_vfx_name_type => InGameVfxNameType.NONE;
        int[] ISpecCharacterInfo.skill_ids => new int[0];
        int ISpecCharacterInfo.passive_skill_id => 0;
        string[] ISpecCharacterInfo.skill_range => System.Array.Empty<string>();
    }
}
