namespace CookApps.AutoChess
{
    /// <summary>
    /// ActionExecutor — 버프/디버프/힐/CC.
    /// Heal, CC, Knockback, Buff, Debuff, Shield, RemoveDebuffs, AddMarker, ModifyStat, ApplyStatusEffect.
    /// </summary>
    public static partial class ActionExecutor
    {
        private static void ExecuteHeal(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int healAmount = ctx.GetCaster().Attack * power / 100;

            if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    SkillDamageHelper.Heal(ctx.State, ref ctx.State.Units[idx], healAmount);
            }
            else if (action.TargetFilter == SkillTargetFilter.AlliesInArea)
            {
                int centerCol, centerRow;
                GetAreaCenter(ctx, out centerCol, out centerRow);

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex != ctx.CasterTeam) continue;
                    if (!SkillAreaHelper.IsInArea(action.AreaShape, centerCol, centerRow, action.AreaRange, ref unit))
                        continue;

                    SkillDamageHelper.Heal(ctx.State, ref unit, healAmount);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.LowestHpAllies)
            {
                int count = action.AreaRange > 0 ? action.AreaRange : 1;
                if (count > LowestHpBuffer.Length) count = LowestHpBuffer.Length;
                int found = SkillAreaHelper.FindLowestHPAllies(ctx.State, ctx.CasterTeam, count, LowestHpBuffer);
                for (int i = 0; i < found; i++)
                {
                    int idx = ctx.State.FindUnitIndex(LowestHpBuffer[i]);
                    if (idx >= 0)
                        SkillDamageHelper.Heal(ctx.State, ref ctx.State.Units[idx], healAmount);
                }
            }
        }

        private static void ExecuteCC(ref SkillAction action, SkillExecuteContext ctx)
        {
            int durationFrames = ctx.GetParamValue(action.SecondaryParamIndex);
            if (durationFrames <= 0) durationFrames = 60;

            var ccType = action.CCType;

            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx < 0) return;
            SkillCCHelper.ApplyCC(ctx.State, ref ctx.State.Units[idx], ccType, durationFrames);
        }

        private static void ExecuteKnockback(ref SkillAction action, SkillExecuteContext ctx)
        {
            int distance = ctx.GetParamValue(action.SecondaryParamIndex);
            if (distance <= 0) distance = 2;

            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx < 0) return;
            ref var target = ref ctx.State.Units[idx];

            ref var caster = ref ctx.GetCaster();
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (dirCol == 0 && dirRow == 0)
                dirCol = caster.TeamIndex == 0 ? 1 : -1;
            else
            {
                dirCol = dirCol > 0 ? 1 : (dirCol < 0 ? -1 : 0);
                dirRow = dirRow > 0 ? 1 : (dirRow < 0 ? -1 : 0);
            }

            SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, distance, ctx.WorldTickRate);
        }

        private static void ExecuteBuff(ref SkillAction action, SkillExecuteContext ctx)
        {
            int value = ctx.GetParamValue(action.ParamIndex);
            if (action.ScaleByHitCount && ctx.BounceCount > 0)
                value *= ctx.BounceCount;
            int duration = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;

            if (action.TargetFilter == SkillTargetFilter.Self)
            {
                int casterIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
                if (casterIdx >= 0)
                    SkillBuffHelper.ApplyTimedBuff(ctx.State, casterIdx, action.BuffStat, value, duration);
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    SkillBuffHelper.ApplyTimedBuff(ctx.State, idx, action.BuffStat, value, duration);
            }
        }

        private static void ExecuteDebuff(ref SkillAction action, SkillExecuteContext ctx)
        {
            int value = ctx.GetParamValue(action.ParamIndex);
            int duration = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;

            bool isStatusEffect = action.StatusEffect != (StatusEffectType)0;

            if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                {
                    if (isStatusEffect)
                        StatusEffectSystem.AddEffect(ctx.State, idx, action.StatusEffect, value, duration);
                    else
                        SkillBuffHelper.ApplyTimedDebuff(ctx.State, idx, action.BuffStat, value, duration, sourceSkillId: ctx.SkillSpecId);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.EnemiesInArea)
            {
                var statusEffect = action.StatusEffect;
                var buffStat = action.BuffStat;

                // Rect 전용: 방향 사전 계산
                int rectDirCol = 0, rectDirRow = 0;
                int rectCasterCol = 0, rectCasterRow = 0;
                if (action.AreaShape == SkillAreaShape.Rect)
                {
                    int cIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
                    if (cIdx >= 0)
                    {
                        rectCasterCol = ctx.State.Units[cIdx].GridCol;
                        rectCasterRow = ctx.State.Units[cIdx].GridRow;
                        int tIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                        if (tIdx >= 0)
                        {
                            rectDirCol = ctx.State.Units[tIdx].GridCol - rectCasterCol;
                            rectDirRow = ctx.State.Units[tIdx].GridRow - rectCasterRow;
                            rectDirCol = rectDirCol > 0 ? 1 : rectDirCol < 0 ? -1 : 0;
                            rectDirRow = rectDirRow > 0 ? 1 : rectDirRow < 0 ? -1 : 0;
                        }
                        if (rectDirCol == 0 && rectDirRow == 0)
                            rectDirRow = ctx.CasterTeam == 0 ? 1 : -1;
                    }
                }

                int areaCol, areaRow;
                GetAreaCenter(ctx, out areaCol, out areaRow);

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex == ctx.CasterTeam) continue;

                    if (action.AreaShape == SkillAreaShape.Rect)
                    {
                        int hw = action.AreaRange;
                        int dp = action.RectDepth > 0 ? action.RectDepth : 1;
                        bool rowDom = rectDirRow != 0;
                        bool inR;
                        if (rowDom)
                        {
                            int dc2 = unit.GridCol - rectCasterCol;
                            if (dc2 < -hw || dc2 > hw) continue;
                            int rd = (unit.GridRow - rectCasterRow) * rectDirRow;
                            inR = rd >= 0 && rd <= dp;
                        }
                        else
                        {
                            int dr2 = unit.GridRow - rectCasterRow;
                            if (dr2 < -hw || dr2 > hw) continue;
                            int cd = (unit.GridCol - rectCasterCol) * rectDirCol;
                            inR = cd >= 0 && cd <= dp;
                        }
                        if (!inR) continue;
                    }
                    else
                    {
                        if (!SkillAreaHelper.IsInArea(action.AreaShape, areaCol, areaRow, action.AreaRange, ref unit))
                            continue;
                    }

                    if (isStatusEffect)
                        StatusEffectSystem.AddEffect(ctx.State, i, statusEffect, value, duration);
                    else
                        SkillBuffHelper.ApplyTimedDebuff(ctx.State, i, buffStat, value, duration, sourceSkillId: ctx.SkillSpecId);
                }
            }
        }

        private static void ExecuteShield(ref SkillAction action, SkillExecuteContext ctx)
        {
            int shieldPercent = ctx.GetParamValue(action.ParamIndex);
            int duration = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;

            if (action.TargetFilter == SkillTargetFilter.SameRowAllies)
            {
                ref var caster = ref ctx.GetCaster();
                int row = caster.GridRow;
                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex != ctx.CasterTeam) continue;
                    if (unit.GridRow != row) continue;

                    int shieldAmount = unit.MaxHP * shieldPercent / 100;
                    SkillBuffHelper.AddShield(ctx.State, i, shieldAmount, duration);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                {
                    ref var target = ref ctx.State.Units[idx];
                    int shieldAmount = target.MaxHP * shieldPercent / 100;
                    SkillBuffHelper.AddShield(ctx.State, idx, shieldAmount, duration);
                }
            }
        }

        private static void ExecuteRemoveDebuffs(ref SkillAction action, SkillExecuteContext ctx)
        {
            if (action.TargetFilter == SkillTargetFilter.AlliesInArea)
            {
                int centerCol, centerRow;
                GetAreaCenter(ctx, out centerCol, out centerRow);

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex != ctx.CasterTeam) continue;
                    if (!SkillAreaHelper.IsInArea(action.AreaShape, centerCol, centerRow, action.AreaRange, ref unit))
                        continue;

                    StatusEffectSystem.RemoveAllDebuffs(ctx.State, i);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.LowestHpAllies)
            {
                int count = action.AreaRange > 0 ? action.AreaRange : 1;
                if (count > LowestHpBuffer.Length) count = LowestHpBuffer.Length;
                int found = SkillAreaHelper.FindLowestHPAllies(ctx.State, ctx.CasterTeam, count, LowestHpBuffer);
                for (int i = 0; i < found; i++)
                {
                    int idx = ctx.State.FindUnitIndex(LowestHpBuffer[i]);
                    if (idx >= 0)
                        StatusEffectSystem.RemoveAllDebuffs(ctx.State, idx);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    StatusEffectSystem.RemoveAllDebuffs(ctx.State, idx);
            }
        }

        private static void ExecuteAddMarker(ref SkillAction action, SkillExecuteContext ctx)
        {
            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx >= 0)
            {
                StatusEffectSystem.AddEffect(ctx.State, idx,
                    StatusEffectType.SkillMarker, action.MarkerType, 1);
            }
        }

        private static void ExecuteModifyStat(ref SkillAction action, SkillExecuteContext ctx)
        {
            int value = ctx.GetParamValue(action.ParamIndex);
            var stat = action.BuffStat;

            if (action.TargetFilter == SkillTargetFilter.Self)
            {
                int idx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
                if (idx >= 0)
                    SkillBuffHelper.ModifyStat(ref ctx.State.Units[idx], stat, value);
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    SkillBuffHelper.ModifyStat(ref ctx.State.Units[idx], stat,
                        action.Effect == SkillEffectType.ModifyStat ? value : -value);
            }
        }

        private static void ExecuteApplyStatusEffect(ref SkillAction action, SkillExecuteContext ctx)
        {
            int duration = action.SecondaryParamIndex >= 0 ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;
            int value = action.ParamIndex >= 0 ? ctx.GetParamValue(action.ParamIndex) : 0;

            int targetIdx;
            if (action.TargetFilter == SkillTargetFilter.Self)
                targetIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
            else
                targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);

            if (targetIdx >= 0)
                StatusEffectSystem.AddEffect(ctx.State, targetIdx, action.StatusEffect, value, duration);
        }
    }
}
