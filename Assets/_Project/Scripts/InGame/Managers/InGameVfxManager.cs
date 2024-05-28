using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxManager : Singleton<InGameVfxManager>
    {
        private List<InGameEffectView> runningEffects = new ();
        private Queue<InGameEffectView> addWaitingInGameEffects = new ();
        private Queue<InGameEffectView> removeWaitingInGameEffects = new ();

        public void Initialize()
        {
            InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects, ManagedUpdate);
            InGameMainFlowManager.Instance.AddLateUpdateListener(InGameMainFlowManager.UpdatePriority_Objects, LateManagedUpdate);
        }

        public void Clear()
        {
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
            InGameMainFlowManager.Instance.RemoveLateUpdateListener(LateManagedUpdate);
            addWaitingInGameEffects.Clear();
            removeWaitingInGameEffects.Clear();
        }

        private void ManagedUpdate(float dt)
        {
            for (var i = 0; i < runningEffects.Count; i++)
            {
                runningEffects[i].ManagedUpdate(dt);
            }
        }

        private void LateManagedUpdate(float dt)
        {
            while (addWaitingInGameEffects.Count > 0)
            {
                InGameEffectView effect = addWaitingInGameEffects.Dequeue();
                runningEffects.Add(effect);
            }

            while (removeWaitingInGameEffects.Count > 0)
            {
                InGameEffectView effect = removeWaitingInGameEffects.Dequeue();
                runningEffects.Remove(effect);
            }
        }

        #region Ingame Effect
        public void AddInGameEffect(InGameEffectView effect)
        {
            addWaitingInGameEffects.Enqueue(effect);
        }

        public void RemoveInGameEffect(InGameEffectView view)
        {
            removeWaitingInGameEffects.Enqueue(view);
        }
        #endregion

        public InGameEffectView Get(string effectName, Transform trPos = null)
        {
            // IngameObjectManager.Instance.AddIngameEffect(effect);
            // return effect;
            return null;
        }

        public InGameEffectView Get(BuffDebuffType buffDebuffType, Transform parent)
        {
            // InGameEffectBase effect = buffDebuffType switch
            // {
            //     BuffDebuffType.Burn => UnityPool<FX_Dot_Fire>.Instance.Get(parent),
            //     BuffDebuffType.Poision => UnityPool<FX_Dot_Poision>.Instance.Get(parent),
            //     BuffDebuffType.Stun => UnityPool<FX_Stun>.Instance.Get(parent),
            //     BuffDebuffType.AttackUp => UnityPool<FX_AttackUp>.Instance.Get(parent),
            //     BuffDebuffType.DeathGoldUp => UnityPool<FX_503_Loop>.Instance.Get(parent),
            //     BuffDebuffType.Invincibility => UnityPool<FX_Invincibility>.Instance.Get(parent),
            //     BuffDebuffType.Paint => UnityPool<FX_c_606_Lisa_Skill_Hit_Loop>.Instance.Get(parent),
            //     //  BuffDebuffType.Slow => UnityPool<FX_c_606_Lisa_Skill_Hit_Loop>.Instance.Get(parent),
            //     _ => null, //throw new ArgumentOutOfRangeException(nameof(buffDebuffType), buffDebuffType, null)
            // };
            //
            // if (effect == null)
            // {
            //     return null;
            // }
            //
            // InGameObjectManager.Instance.AddInGameEffect(effect);
            // return effect;
            return null;
        }
    }

    // public static InGameEffectHitBase Get(HitEffectType type)
    // {
    //     InGameEffectHitBase effect = type switch
    //     {
    //         HitEffectType.Basic => UnityPool<InGameEffectHitBasic>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Fire => UnityPool<IngameEffectHit_Fire>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Charlotte => UnityPool<IngameEffectHit_Charlotte>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Rose => UnityPool<IngameEffectHit_Rose>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.RoseSkill => UnityPool<IngameEffectHit_RoseSkill>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Wolfie => UnityPool<IngameEffectHit_Wolfie>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Aqua => UnityPool<IngameEffectHit_Aqua>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Winter => UnityPool<IngameEffectHit_Winter>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Lucy => UnityPool<IngameEffectHit_Lucy>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Kiosk => UnityPool<IngameEffectHit_Kiosk>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Jayden => UnityPool<IngameEffectHit_Jaden>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Zephy => UnityPool<IngameEffectHit_Zephy>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Vortex => UnityPool<IngameEffectHit_Vortex>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.LusySkill => UnityPool<IngameEffectHit_LucySkill>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Gold => UnityPool<IngameEffectHit_Gold>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Luna => UnityPool<IngameEffectHit_Luna>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Kain => UnityPool<IngameEffectHit_kain>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Dragon => UnityPool<IngameEffectHit_Dragon>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Lisa => UnityPool<IngameEffectHit_Lisa>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.MookE => UnityPool<IngameEffectHit_Mook>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.RookE => UnityPool<IngameEffectHit_RookE>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.knight => UnityPool<IngameEffectHit_Knight>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.king => UnityPool<IngameEffectHit_King>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Asura => UnityPool<IngameEffectHit_Asura>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Robin => UnityPool<IngameEffectHit_Robin>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Awake => UnityPool<FX_Awake_Hit>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         HitEffectType.Jewel => UnityPool<FX_Jewel_Hit>.Instance.Get(IngameObjectManager.Instance.Playground),
    //         _ => null,
    //     };
    //
    //     if (effect == null)
    //     {
    //         return null;
    //     }
    //
    //     IngameObjectManager.Instance.AddIngameEffect(effect);
    //     return effect;
    // }

    // public static class InGameEffectProjectileFactory
    // {
    //     public static InGameEffectProjectileBase Get(AttackEffectType type)
    //     {
    //         InGameEffectProjectileBase effect = type switch
    //         {
    //             AttackEffectType.Fire => UnityPool<InGameEffectProjectileFire>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Arrow => UnityPool<InGameEffectProjectileArrow>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Charlotte => UnityPool<InGameEffectProjectileCharlotte>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Rose => UnityPool<InGameEffectProjectileRose>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.AquaSkill => UnityPool<InGameEffectProjectileAqua>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Winter => UnityPool<InGameEffectProjectileWinter>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Kiosk => UnityPool<InGameEffectProjectileKiosk>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.JaydenSkill => UnityPool<InGameEffectProjectileJayden>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Ophelia => UnityPool<InGameEffectProjectileOphelia>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.OpheliaSkill => UnityPool<InGameEffectProjectileOpheliaSkill>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Hana => UnityPool<InGameEffectProjectileHana>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.HanaSkill => UnityPool<InGameEffectProjectileHanaSkill>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Zephy => UnityPool<InGameEffectProjectileZephy>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Vortex => UnityPool<InGameEffectProjectileVortex>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.LucySkill => UnityPool<InGameEffectProjectileLucySkill>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.FireSkill => UnityPool<InGameEffectProjectileFireSkill>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Luna => UnityPool<InGameEffectProjectileLuna>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Dragon => UnityPool<InGameEffectProjectileDragon>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Lisa => UnityPool<InGameEffectProjectileLisa>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Eagle => UnityPool<InGameEffectProjectileEagle>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.RookE => UnityPool<InGameEffectProjectileRookE>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.MookE => UnityPool<InGameEffectProjectileMookE>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Knight => UnityPool<InGameEffectProjectileKnight>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.king => UnityPool<InGameEffectProjectileKing>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Asura => UnityPool<InGameEffectProjectileAsura>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             AttackEffectType.Robin => UnityPool<InGameEffectProjectileRobin>.Instance.Get(IngameObjectManager.Instance.Playground),
    //             _ => null,
    //         };
    //
    //         if (effect == null)
    //         {
    //             return null;
    //         }
    //
    //         IngameObjectManager.Instance.AddIngameEffect(effect);
    //         return effect;
    //     }
    // }
}
