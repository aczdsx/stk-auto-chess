using System;
using System.Collections.Generic;

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

        public static string GetOneShotVfxName(this BuffDebuffType type)
        {
            return type switch
            {
                BuffDebuffType.Meditation => "",
                BuffDebuffType.Shield => "",
                BuffDebuffType.Bleeding => "",
                BuffDebuffType.Poison => "",
                BuffDebuffType.Burn => "",
                BuffDebuffType.AttackUp => "fx_common_buff_atkup_01",
                BuffDebuffType.DefenceUp => "",
                BuffDebuffType.ResistanceUp => "",
                BuffDebuffType.AttackDown => "",
                BuffDebuffType.DefenceDown => "",
                BuffDebuffType.ResistanceDown => "",
                BuffDebuffType.AttackSpeedUp => "",
                BuffDebuffType.AttackSpeedDown => "",
                BuffDebuffType.CriticalProbUp => "",
                BuffDebuffType.CriticalProbDown => "",
                BuffDebuffType.Slow => "",
                BuffDebuffType.Entangle => "",
                BuffDebuffType.Freezing => "",
                BuffDebuffType.Stun => "",
                BuffDebuffType.Provocation => "",
                BuffDebuffType.Sleep => "",
                BuffDebuffType.Invincibility => "",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static string GetLoopVfxName(this BuffDebuffType type)
        {
            return type switch
            {
                BuffDebuffType.Meditation => "",
                BuffDebuffType.Shield => "",
                BuffDebuffType.Bleeding => "",
                BuffDebuffType.Poison => "",
                BuffDebuffType.Burn => "",
                BuffDebuffType.AttackUp => "fx_common_buff_atkup_02",
                BuffDebuffType.DefenceUp => "",
                BuffDebuffType.ResistanceUp => "",
                BuffDebuffType.AttackDown => "",
                BuffDebuffType.DefenceDown => "",
                BuffDebuffType.ResistanceDown => "",
                BuffDebuffType.AttackSpeedUp => "",
                BuffDebuffType.AttackSpeedDown => "",
                BuffDebuffType.CriticalProbUp => "",
                BuffDebuffType.CriticalProbDown => "",
                BuffDebuffType.Slow => "",
                BuffDebuffType.Entangle => "",
                BuffDebuffType.Freezing => "",
                BuffDebuffType.Stun => "",
                BuffDebuffType.Provocation => "",
                BuffDebuffType.Sleep => "",
                BuffDebuffType.Invincibility => "",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
