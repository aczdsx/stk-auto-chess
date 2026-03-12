namespace CookApps.AutoChess.View
{
    // ── 리플레이 전용 (디버그 빌드 전용) ──
    public partial class AutoChessViewBridge
    {
        /// <summary>리플레이용 View 리셋 (투사체/VFX 정리, 유닛 View 유지)</summary>
        public void ResetForReplay()
        {
            _combatViewManager?.ResetForReplay();
            _combatVfxManager?.OnCombatEnd();
        }

        /// <summary>
        /// 리플레이 후 View 동기화.
        /// 유닛 위치/HP 동기화 → 마지막 틱 이벤트 디스패치 → 활성 투사체 즉시 스폰 → 상태이펙트 VFX 복원.
        /// </summary>
        public void SyncForReplay(GameWorld world)
        {
            var matchState = world.CombatMatchStates[0];

            // 1. 유닛 위치/HP 동기화 (이벤트 처리 전에 먼저 동기화)
            SyncCombatViews(world);

            // 2. 마지막 틱 이벤트 디스패치
            if (matchState?.EventQueue != null)
            {
                for (int i = 0; i < matchState.EventQueue.Count; i++)
                {
                    ref var evt = ref matchState.EventQueue.Events[i];
                    DispatchEvent(ref evt, world);
                }
                matchState.EventQueue.Clear();
            }

            // 3. 활성 투사체 즉시 스폰
            if (matchState != null)
            {
                for (int i = 0; i < matchState.ProjectileCount; i++)
                {
                    ref var p = ref matchState.Projectiles[i];
                    if (!p.IsActive) continue;

                    int champSpecId = 0;
                    for (int u = 0; u < matchState.UnitCount; u++)
                    {
                        if (matchState.Units[u].CombatId == p.SourceCombatId)
                        {
                            champSpecId = matchState.Units[u].ChampionSpecId;
                            break;
                        }
                    }

                    _combatViewManager?.SpawnProjectileImmediate(
                        p.SourceCombatId, p.TargetCombatId, p.Type,
                        p.CurrentCol, p.CurrentRow, p.DirCol, p.DirRow,
                        champSpecId, p.ProjectileId, p.SkillSpecId,
                        p.SkillVfxIndex, p.MoveInterval);
                }
            }

            // 4. 상태이펙트 VFX 복원 (쉴드/버프/디버프 루프 VFX)
            SyncStatusEffectVfx(matchState);
        }

        /// <summary>
        /// CombatMatchState의 활성 StatusEffect들을 순회하여 VFX를 복원.
        /// ResetForReplay()에서 CombatVfxManager.OnCombatEnd()로 전부 정리했으므로 다시 적용.
        /// </summary>
        private void SyncStatusEffectVfx(CombatMatchState matchState)
        {
            if (matchState == null || _combatVfxManager == null) return;

            for (int i = 0; i < matchState.StatusEffectCount; i++)
            {
                ref var se = ref matchState.StatusEffects[i];
                if (!se.IsActive) continue;

                var vfxType = StatusEffectSystem.ToVfxType(se.Type, se.StatType);
                if (vfxType == CombatVfxType.None) continue;

                // 유닛 인덱스 → CombatId
                if (se.OwnerUnitIndex < 0 || se.OwnerUnitIndex >= matchState.UnitCount) continue;
                int combatId = matchState.Units[se.OwnerUnitIndex].CombatId;

                _combatVfxManager.OnEffectAdded(combatId, vfxType);
            }

            // CC 효과 VFX 복원
            for (int i = 0; i < matchState.UnitCount; i++)
            {
                ref var unit = ref matchState.Units[i];
                if (!unit.IsAlive || unit.ActiveCC == CrowdControlType.None) continue;

                var ccVfxType = CCToVfxType(unit.ActiveCC);
                if (ccVfxType != CombatVfxType.None)
                    _combatVfxManager.OnEffectAdded(unit.CombatId, ccVfxType);
            }
        }

        private static CombatVfxType CCToVfxType(CrowdControlType cc)
        {
            return cc switch
            {
                CrowdControlType.Stun => CombatVfxType.CC_Stun,
                CrowdControlType.Freeze => CombatVfxType.CC_Freeze,
                CrowdControlType.Silence => CombatVfxType.CC_Silence,
                CrowdControlType.Taunt => CombatVfxType.CC_Taunt,
                CrowdControlType.Slow => CombatVfxType.CC_Slow,
                CrowdControlType.Airborne => CombatVfxType.CC_Airborne,
                CrowdControlType.Knockback => CombatVfxType.CC_KnockBack,
                _ => CombatVfxType.None,
            };
        }
    }
}
