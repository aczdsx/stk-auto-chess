using System;
using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    public static class InGameEnumExtensions
    {
        private static Dictionary<int, string> cachedAnimKeyStringMap = new();
        private static Dictionary<string, int> cachedAnimStringKeyMap = new();

        public static string ToAnimationName(this AnimationKey key)
        {
            var index = (int)key;
            if (!cachedAnimKeyStringMap.ContainsKey(index)) cachedAnimKeyStringMap.Add(index, key.ToString());

            return cachedAnimKeyStringMap[index];
        }

        public static AnimationKey ToAnimationKey(this string name)
        {
            if (!cachedAnimStringKeyMap.ContainsKey(name))
                cachedAnimStringKeyMap.Add(name, (int)Enum.Parse(typeof(AnimationKey), name));

            return (AnimationKey)cachedAnimStringKeyMap[name];
        }

        private static Dictionary<int, string> cachedInGameVfxAnimKeyStringMap = new();

        public static string ToAnimationName(this InGameVfxAnimationKey key)
        {
            var index = (int)key;
            if (!cachedInGameVfxAnimKeyStringMap.ContainsKey(index))
                cachedInGameVfxAnimKeyStringMap.Add(index, key.ToString());

            return cachedInGameVfxAnimKeyStringMap[index];
        }

        public static InGameVfxNameType GetOneShotVfxName(this BuffDebuffType type)
        {
            return type switch
            {
                // BuffDebuffType.Meditation => "",
                // BuffDebuffType.Shield => InGameVfxNameType.fx_common_buff_shield_01,
                // BuffDebuffType.Bleeding => "",
                // BuffDebuffType.Poison => "",
                // BuffDebuffType.Burn => "",
                BuffDebuffType.AttackUp => InGameVfxNameType.fx_common_buff_atkup_01,
                BuffDebuffType.AttackDown => InGameVfxNameType.fx_common_debuff_atkdown_01,
                BuffDebuffType.AttackSpeedUp => InGameVfxNameType.fx_common_buff_spdup_01,
                BuffDebuffType.CoolTimeUp => InGameVfxNameType.fx_common_buff_ctdown_01,
                BuffDebuffType.AbilityPowerUp => InGameVfxNameType.fx_common_buff_apup,
                BuffDebuffType.DefenceUp => InGameVfxNameType.fx_common_buff_dfup,
                // BuffDebuffType.ResistanceUp => "",
                // BuffDebuffType.AttackDown => "",
                // BuffDebuffType.DefenceDown => "",
                // BuffDebuffType.ResistanceDown => "",
                // BuffDebuffType.AttackSpeedUp => "",
                BuffDebuffType.AttackSpeedDown => InGameVfxNameType.fx_common_debuff_spddown_01,
                BuffDebuffType.CoolTimeDown => InGameVfxNameType.fx_common_debuff_ctup_01,
                BuffDebuffType.Trap => InGameVfxNameType.fx_common_trap_hold_01,
                BuffDebuffType.DefenceDown => InGameVfxNameType.fx_common_debuff_dfdown,
                BuffDebuffType.Provocation => InGameVfxNameType.fx_common_buff_provoke,
                BuffDebuffType.HealDown => InGameVfxNameType.fx_common_debuff_healdown,
                // BuffDebuffType.Immune => InGameVfxNameType.fx_common_buff_immune_01,
                // BuffDebuffType.CriticalProbUp => "",
                // BuffDebuffType.CriticalProbDown => "",
                // BuffDebuffType.Slow => "",
                // BuffDebuffType.Entangle => "",
                // BuffDebuffType.Freezing => "",
                // BuffDebuffType.Stun => "",
                // BuffDebuffType.Provocation => "",
                // BuffDebuffType.Sleep => "",
                // BuffDebuffType.Invincibility => "",
                BuffDebuffType.Misa => InGameVfxNameType.fx_common_debuff_misa_01,
                BuffDebuffType.NormalAttackShield => InGameVfxNameType.fx_common_job_guardian_01,
                _ => InGameVfxNameType.NONE
            };
        }

        public static InGameVfxNameType GetLoopVfxName(this BuffDebuffType type)
        {
            return type switch
            {
                // BuffDebuffType.Meditation => "",
                // BuffDebuffType.Shield => InGameVfxNameType.fx_common_buff_shield_02,
                // BuffDebuffType.Bleeding => "",
                // BuffDebuffType.Poison => "",
                // BuffDebuffType.Burn => "",
                BuffDebuffType.AttackUp => InGameVfxNameType.fx_common_buff_atkup_02,
                BuffDebuffType.AttackDown => InGameVfxNameType.fx_common_debuff_atkdown_02,
                BuffDebuffType.AttackSpeedUp => InGameVfxNameType.fx_common_buff_spdup_02,
                BuffDebuffType.CoolTimeUp => InGameVfxNameType.fx_common_buff_ctdown_02,
                BuffDebuffType.AbilityPowerUp => InGameVfxNameType.fx_common_buff_apup_01,
                BuffDebuffType.DefenceUp => InGameVfxNameType.fx_common_buff_dfup_01,
                // BuffDebuffType.ResistanceUp => "",
                // BuffDebuffType.AttackDown => "",
                // BuffDebuffType.DefenceDown => "",
                // BuffDebuffType.ResistanceDown => "",
                // BuffDebuffType.AttackSpeedUp => "",
                BuffDebuffType.AttackSpeedDown => InGameVfxNameType.fx_common_debuff_spddown_02,
                BuffDebuffType.CoolTimeDown => InGameVfxNameType.fx_common_debuff_ctup_02,
                BuffDebuffType.Trap => InGameVfxNameType.fx_common_trap_hold_02,
                BuffDebuffType.DefenceDown => InGameVfxNameType.fx_common_debuff_dfdown_01,
                BuffDebuffType.HealDown => InGameVfxNameType.fx_common_debuff_healdown_01,
                // BuffDebuffType.Immune => InGameVfxNameType.fx_common_buff_immune_02,
                // BuffDebuffType.CriticalProbUp => "",
                // BuffDebuffType.CriticalProbDown => "",
                // BuffDebuffType.Slow => "",
                // BuffDebuffType.Entangle => "",
                // BuffDebuffType.Freezing => "",
                BuffDebuffType.Stun => InGameVfxNameType.fx_common_debuff_stun,
                BuffDebuffType.Silence => InGameVfxNameType.fx_common_debuff_silence,
                BuffDebuffType.Provocation => InGameVfxNameType.fx_common_buff_provoke_01,
                // BuffDebuffType.Sleep => "",
                // BuffDebuffType.Invincibility => "",
                BuffDebuffType.Misa => InGameVfxNameType.fx_common_debuff_misa_02,
                BuffDebuffType.Airborne => InGameVfxNameType.fx_common_commander_skill_03,
                _ => InGameVfxNameType.NONE
            };
        }

        public static SoundFX GetSoundFx(this BuffDebuffType type)
        {
            if(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageReady ||
            InGameMainFlowManager.Instance.CurrentFlowState is FlowStateTrialDungeonReady ||
            InGameMainFlowManager.Instance.CurrentFlowState is FlowStateInGameTestReady)
            {
                return SoundFX.NONE;
            }

            return type switch
            {
                // 버프 (0~999)
                BuffDebuffType.Meditation => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.Shield => SoundFX.snd_sfx_ingame_shield,
                BuffDebuffType.AttackUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.AbilityPowerUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.DefenceUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.ResistanceUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.AttackSpeedUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.CriticalProbUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.NormalAttackShield => SoundFX.snd_sfx_ingame_shield,
                BuffDebuffType.NoDamageShield => SoundFX.snd_sfx_ingame_shield,
                BuffDebuffType.Invincibility => SoundFX.snd_sfx_ingame_shield,
                BuffDebuffType.CoolTimeUp => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.TargetImpossible => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.Immune => SoundFX.snd_sfx_ingame_buff,
                BuffDebuffType.AvoidProbUp => SoundFX.snd_sfx_ingame_buff,

                // 디버프 (1000 이상)
                BuffDebuffType.AttackDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.DefenceDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.CoolTimeDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.ResistanceDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.AttackSpeedDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.CriticalProbDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.HealDown => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Bleeding => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Poison => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Burn => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Slow => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Entangle => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Freezing => SoundFX.snd_sfx_ingame_freeze,
                BuffDebuffType.Stun => SoundFX.snd_sfx_ingame_stun,
                BuffDebuffType.Sleep => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Provocation => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Anger => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Trap => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Silence => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Airborne => SoundFX.snd_sfx_ingame_debuff,
                BuffDebuffType.Misa => SoundFX.snd_sfx_ingame_debuff,

                _ => SoundFX.NONE
            };
        }
    }
}