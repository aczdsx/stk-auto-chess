namespace CookApps.AutoChess
{
    /// <summary>스킬로 적 처치 시 마나 즉시 충전 (스킬 재시전 가능). 마커 타입으로 스킬 킬 판별.</summary>
    public class SkillKillManaResetTrait : CombatTraitBase
    {
        private readonly int _markerType;

        public SkillKillManaResetTrait(SkillMarkerType markerType)
        {
            _markerType = (int)markerType;
        }

        public override void OnKill(CombatMatchState state, ref CombatUnit killer, ref CombatUnit victim)
        {
            if (killer.MaxMana <= 0 || !killer.IsAlive) return;

            int killerIdx = state.FindUnitIndex(killer.CombatId);
            if (killerIdx < 0) return;

            if (StatusEffectSystem.CountMarkers(state, killerIdx, _markerType) > 0)
            {
                killer.CurrentMana = killer.MaxMana;

                StatusEffectSystem.RemoveOldestMarker(state, killerIdx, _markerType);
            }
        }
    }
}
