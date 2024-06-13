using System;
using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    public static class InGameEnumExtensions
    {
        private static Dictionary<int, string> cachedAnimKeyStringMap = new ();
        private static Dictionary<string, int> cachedAnimStringKeyMap = new ();

        public static string ToAnimationName(this AnimationKey key)
        {
            var index = (int) key;
            if (!cachedAnimKeyStringMap.ContainsKey(index))
            {
                cachedAnimKeyStringMap.Add(index, key.ToString());
            }

            return cachedAnimKeyStringMap[index];
        }

        public static AnimationKey ToAnimationKey(this string name)
        {
            if (!cachedAnimStringKeyMap.ContainsKey(name))
            {
                cachedAnimStringKeyMap.Add(name, (int) Enum.Parse(typeof(AnimationKey), name));
            }

            return (AnimationKey) cachedAnimStringKeyMap[name];
        }

        private static Dictionary<int, string> cachedInGameVfxAnimKeyStringMap = new ();

        public static string ToAnimationName(this InGameVfxAnimationKey key)
        {
            var index = (int) key;
            if (!cachedInGameVfxAnimKeyStringMap.ContainsKey(index))
            {
                cachedInGameVfxAnimKeyStringMap.Add(index, key.ToString());
            }

            return cachedInGameVfxAnimKeyStringMap[index];
        }

        public static InGameVfxNameType GetOneShotVfxName(this BuffDebuffType type)
        {
            return type switch
            {
                // BuffDebuffType.Meditation => "",
                BuffDebuffType.Shield =>InGameVfxNameType.fx_common_buff_shield_01,
                // BuffDebuffType.Bleeding => "",
                // BuffDebuffType.Poison => "",
                // BuffDebuffType.Burn => "",
                BuffDebuffType.AttackUp => InGameVfxNameType.fx_common_buff_atkup_01,
                // BuffDebuffType.DefenceUp => "",
                // BuffDebuffType.ResistanceUp => "",
                // BuffDebuffType.AttackDown => "",
                // BuffDebuffType.DefenceDown => "",
                // BuffDebuffType.ResistanceDown => "",
                // BuffDebuffType.AttackSpeedUp => "",
                // BuffDebuffType.AttackSpeedDown => "",
                // BuffDebuffType.CriticalProbUp => "",
                // BuffDebuffType.CriticalProbDown => "",
                // BuffDebuffType.Slow => "",
                // BuffDebuffType.Entangle => "",
                // BuffDebuffType.Freezing => "",
                // BuffDebuffType.Stun => "",
                // BuffDebuffType.Provocation => "",
                // BuffDebuffType.Sleep => "",
                // BuffDebuffType.Invincibility => "",
                _ => InGameVfxNameType.NONE
            };
        }

        public static InGameVfxNameType GetLoopVfxName(this BuffDebuffType type)
        {
            return type switch
            {
                // BuffDebuffType.Meditation => "",
                BuffDebuffType.Shield =>  InGameVfxNameType.fx_common_buff_shield_02,
                // BuffDebuffType.Bleeding => "",
                // BuffDebuffType.Poison => "",
                // BuffDebuffType.Burn => "",
                BuffDebuffType.AttackUp => InGameVfxNameType.fx_common_buff_atkup_02,
                // BuffDebuffType.DefenceUp => "",
                // BuffDebuffType.ResistanceUp => "",
                // BuffDebuffType.AttackDown => "",
                // BuffDebuffType.DefenceDown => "",
                // BuffDebuffType.ResistanceDown => "",
                // BuffDebuffType.AttackSpeedUp => "",
                // BuffDebuffType.AttackSpeedDown => "",
                // BuffDebuffType.CriticalProbUp => "",
                // BuffDebuffType.CriticalProbDown => "",
                // BuffDebuffType.Slow => "",
                // BuffDebuffType.Entangle => "",
                // BuffDebuffType.Freezing => "",
                // BuffDebuffType.Stun => "",
                // BuffDebuffType.Provocation => "",
                // BuffDebuffType.Sleep => "",
                // BuffDebuffType.Invincibility => "",
                _ => InGameVfxNameType.NONE
            };
        }
    }
}
