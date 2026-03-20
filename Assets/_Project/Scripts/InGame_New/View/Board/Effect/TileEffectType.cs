namespace CookApps.AutoChess.View
{
    public enum TileEffectType
    {
        // 배치용
        Placement,          // fx_common_area_plan
        AttackRange,        // fx_common_area_plan_02
        CanPlacement,       // fx_common_area_plan_03
        SkillRange,         // fx_common_area_commander_01

        // 전투용 — 원소별 영역
        FireArea,           // fx_common_area_fire
        WindArea,           // fx_common_area_wind
        LightningArea,      // fx_common_area_light
        EarthArea,          // fx_common_area_earth
        WaterArea,          // fx_common_area_water

        // 전투용 — 시전 이펙트
        FireCast,           // fx_common_cast_fire
        WindCast,           // fx_common_cast_wind
        LightningCast,      // fx_common_cast_light
        EarthCast,          // fx_common_cast_earth
        WaterCast,          // fx_common_cast_water
    }
}