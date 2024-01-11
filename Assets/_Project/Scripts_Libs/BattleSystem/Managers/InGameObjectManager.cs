using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameObjectManager : SingletonMonoBehaviour<InGameObjectManager>
    {
        private List<CharacterController> enemiesInPlaygroundForUpdate = new ();
        private List<CharacterController> charactersInPlaygroundForUpdate = new ();

        private bool isChangeCharacterFailStage;

        public void ChangeCharacterFailStage(bool isOn)
        {
            isChangeCharacterFailStage = isOn;
        }

        public bool GetChangeCharacterFailStage()
        {
            return isChangeCharacterFailStage;
        }

        private int currentMainHeroId = 0;

        public int GetMainHeroId()
        {
            return currentMainHeroId;
        }

        public void ChangeMainHero(int id)
        {
            currentMainHeroId = id;
        }

        private Transform playground;
        public Transform Playground => playground;

        private List<InGameEffectBase> inGameEffects = new ();
        private Queue<InGameEffectBase> addWaitingInGameEffects = new ();
        private Queue<InGameEffectBase> removeWaitingInGameEffects = new ();

        private bool isAuto = false;
        public bool IsAuto => isAuto;

        public void ChangeAuto(bool isAuto)
        {
            this.isAuto = isAuto;
        }

        private bool isAutoAssemble = false;
        public bool IsAutoAssemble => isAutoAssemble;

        public void ChangeAutoAssemble(bool isAuto)
        {
            isAutoAssemble = isAuto;
        }

        private bool isPreAuto;

        public void SavePreAuto()
        {
            isPreAuto = isAuto;
        }

        public void SetPreAuto()
        {
            isAuto = isPreAuto;
        }

        public void Initialize()
        {
            InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects, ManagedUpdate);
            InGameMainFlowManager.Instance.AddLateUpdateListener(InGameMainFlowManager.UpdatePriority_Objects, LateManagedUpdate);
            playground = GameObject.Find("Playground").GetComponent<Transform>();
        }

        public void Clear()
        {
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
            InGameMainFlowManager.Instance.RemoveLateUpdateListener(LateManagedUpdate);
            playground = null;
            ClearAllCharactersInField();
            ClearAllEnemiesInField();
            addWaitingInGameEffects.Clear();
            removeWaitingInGameEffects.Clear();
        }

        private void ManagedUpdate(float dt)
        {
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                charactersInPlaygroundForUpdate[i].ManagedUpdate(dt);
            }

            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                enemiesInPlaygroundForUpdate[i].ManagedUpdate(dt);
            }

            for (var i = 0; i < inGameEffects.Count; i++)
            {
                inGameEffects[i].ManagedUpdate(dt);
            }
        }

        private void LateManagedUpdate(float dt)
        {
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                charactersInPlaygroundForUpdate[i].LateUpdate(dt);
            }

            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                enemiesInPlaygroundForUpdate[i].LateUpdate(dt);
            }

            while (addWaitingInGameEffects.Count > 0)
            {
                InGameEffectBase effect = addWaitingInGameEffects.Dequeue();
                inGameEffects.Add(effect);
            }

            while (removeWaitingInGameEffects.Count > 0)
            {
                InGameEffectBase effect = removeWaitingInGameEffects.Dequeue();
                inGameEffects.Remove(effect);
            }
        }

        #region Ingame Effect
        public void AddInGameEffect(InGameEffectBase effect)
        {
            addWaitingInGameEffects.Enqueue(effect);
        }

        public void RemoveInGameEffect(InGameEffectBase view)
        {
            removeWaitingInGameEffects.Enqueue(view);
        }
        #endregion

        public List<CharacterController> GetCharacterAll()
        {
            return charactersInPlaygroundForUpdate;
        }

        public CharacterController GetCharacterInField(int characUId)
        {
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                if (charactersInPlaygroundForUpdate[i].GetCharacterStat().CharacterUId == characUId)
                {
                    return charactersInPlaygroundForUpdate[i];
                }
            }

            return null;
        }

        // public async UniTask<CharacterController> AddCharacterToField(int charId, ICharacterStatData statData,
        //     Vector2 initPos, AllianceType allianceType, EnemyType enemyType, int equipIdx = -1, SpecStageMonster stageSpecData = null, bool IsInvincibility = true)
        // {
        //     var characCtrl = new CharacterController();
        //     await characCtrl.Initialize(charId, statData, initPos, allianceType, enemyType, equipIdx, stageSpecData);
        //
        //     Transform trParent = allianceType == AllianceType.Player || allianceType == AllianceType.SubPlayer || allianceType == AllianceType.Summon
        //         ? JoystickMover.transform
        //         : Playground;
        //
        //     characCtrl.GetCharacterView().CachedTr.SetParent(trParent, false);
        //     characCtrl.GetCharacterView().SetCamera();
        //
        //     if (allianceType == AllianceType.Player || allianceType == AllianceType.SubPlayer || allianceType == AllianceType.Summon)
        //     {
        //         characCtrl.GetCharacterView().ResetAssemble();
        //
        //         var isAdded = false;
        //         for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
        //         {
        //             if (charactersInPlaygroundForUpdate[i].GetCharacterStat().heroId < charId)
        //             {
        //                 charactersInPlaygroundForUpdate.Insert(i, characCtrl);
        //                 isAdded = true;
        //                 break;
        //             }
        //         }
        //
        //         if (!isAdded)
        //         {
        //             charactersInPlaygroundForUpdate.Add(characCtrl);
        //         }
        //     }
        //     else if (allianceType == AllianceType.Enemy)
        //     {
        //         if (enemyType == EnemyType.Boss)
        //         {
        //             characCtrl.IsInvincibility = IsInvincibility;
        //         }
        //
        //         enemyCtrlsInPlaygroundForUpdate.Add(characCtrl);
        //         InGameMain.GetIngameMainUI().SetAssembleDebuff(characCtrl);
        //         InGameMain.GetIngameMainUI().SetBlackHole(characCtrl);
        //     }
        //
        //     if (InGameMain.GetIngameMainUI().IsTown() == true)
        //     {
        //         if (allianceType == AllianceType.SubPlayer)
        //         {
        //             characCtrl.AddNextState<CharacterStateAi>();
        //         }
        //         else
        //         {
        //             characCtrl.AddNextState<CharacterStateIdle>();
        //         }
        //     }
        //     else
        //     {
        //         characCtrl.AddNextState<CharacterStateIdle>();
        //     }
        //
        //     return characCtrl;
        // }

        public void RemoveCharacterFromField(CharacterController characCtrl)
        {
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = charactersInPlaygroundForUpdate[i];
                other.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(characCtrl);
            }

            if (!charactersInPlaygroundForUpdate.Remove(characCtrl))
            {
                enemiesInPlaygroundForUpdate.Remove(characCtrl);
            }

            characCtrl.Clear();
        }

        public void RemoveAllEffectCodesWithoutSourceIsNull()
        {
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = charactersInPlaygroundForUpdate[i];
                other.GetEffectCodeContainer().RemoveAllEffectCodesWithoutSourceIsNull();
            }
        }

        public bool IsCheckAllIsAliveCharacter()
        {
            var isAlive = false;
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                if (charactersInPlaygroundForUpdate[i].IsAlive == true)
                {
                    isAlive = true;
                    break;
                }
            }

            return isAlive;
        }

        public bool IsCheckAllDieCharacter()
        {
            return charactersInPlaygroundForUpdate.Count == 0 ? true : false;
        }

        public void RemoveCharacterFromField(int characUId)
        {
            CharacterController charCtrl = GetCharacterInField(characUId);

            CharacterController target = null;

            if (charCtrl.target != null)
            {
                target = charCtrl.target;
            }

            RemoveCharacterFromField(charCtrl);
            if (target != null)
            {
                target.target = null;
            }
        }

        public void ClearAllCharactersInField()
        {
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                charactersInPlaygroundForUpdate[i].Clear();
            }

            charactersInPlaygroundForUpdate.Clear();
        }

        public void ClearAllEnemiesInField()
        {
            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                enemiesInPlaygroundForUpdate[i].Clear();
            }

            enemiesInPlaygroundForUpdate.Clear();
        }

        public void RemoveEnemyFromField(CharacterController characCtrl)
        {
            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = enemiesInPlaygroundForUpdate[i];
                other.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(characCtrl);
            }

            characCtrl.Clear();
            enemiesInPlaygroundForUpdate.Remove(characCtrl);
        }

        public void RemoveEnemyEffectCode(CharacterController characCtrl)
        {
            if (characCtrl != null)
            {
                characCtrl.GetEffectCodeContainer().Clear();
                for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
                {
                    CharacterController other = enemiesInPlaygroundForUpdate[i];
                    other.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(characCtrl);
                }
            }
        }

        public List<CharacterController> GetEnemiesList()
        {
            return enemiesInPlaygroundForUpdate;
        }

        public List<CharacterController> GetNearestCharactersInRange(CharacterController owner, float range)
        {
            List<CharacterController> targets = ListPool<CharacterController>.Get();
            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = enemiesInPlaygroundForUpdate[i];
                if (other == null || !other.IsAlive)
                {
                    continue;
                }

                Vector2 posDiff = owner.Position - other.Position;
                bool isInRange = posDiff.sqrMagnitude < range * range;
                if (isInRange)
                {
                    targets.Add(other);
                }
            }

            return targets;
        }

        public List<CharacterController> GetNearestEnemyInRange(CharacterController owner, float range)
        {
            List<CharacterController> targets = ListPool<CharacterController>.Get();
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = charactersInPlaygroundForUpdate[i];
                if (other == null || !other.IsAlive)
                {
                    continue;
                }

                Vector2 posDiff = owner.Position - other.Position;
                bool isInRange = posDiff.sqrMagnitude < range * range;
                if (isInRange)
                {
                    targets.Add(other);
                }
            }

            return targets;
        }

        // 타겟 기준 범위
        public List<CharacterController> GetNearestTargetInRange(CharacterController target, float range)
        {
            List<CharacterController> targets = ListPool<CharacterController>.Get();
            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = enemiesInPlaygroundForUpdate[i];
                if (other == null || !other.IsAlive)
                {
                    continue;
                }

                Vector2 posDiff = target.Position - other.Position;
                bool isInRange = posDiff.sqrMagnitude < range * range;
                if (isInRange)
                {
                    targets.Add(other);
                }
            }

            return targets;
        }

        // 체력이 제일 적은 기준
        public CharacterController GetNearestCharactersInHp()
        {
            CharacterController healTarget = null;
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = charactersInPlaygroundForUpdate[i];
                if (!other.IsAlive)
                {
                    continue;
                }

                if (healTarget == null)
                {
                    healTarget = other;
                }
                else
                {
                    if (healTarget.CurrentHp > other.CurrentHp)
                    {
                        healTarget = other;
                    }
                }
            }

            return healTarget;
        }

        public CharacterController GetTarget(CharacterController characCtrl)
        {
            return GetCharactersWithTargetNear(characCtrl);
        }

        public CharacterController GetNearPlayer(Vector2 pos)
        {
            CharacterController target = null;

            List<CharacterController> targets = charactersInPlaygroundForUpdate;

            if (targets.Count == 0)
            {
                return null;
            }

            var minDest = float.MaxValue; // (recognitionRange * recognitionRange);
            var dis = float.MaxValue;

            for (var idx = 0; idx < targets.Count; ++idx)
            {
                if (targets[idx].IsAlive == false)
                {
                    continue;
                }

                dis = Vector3.SqrMagnitude(pos - targets[idx].Position);
                if (minDest > dis)
                {
                    minDest = dis;
                    target = targets[idx];
                }
            }

            return target;
        }

        public CharacterController GetCharactersWithTargetNear(CharacterController characCtrl)
        {
            CharacterController target = null;

            List<CharacterController> targets = null;

            if (characCtrl.AllianceType == AllianceType.Enemy)
            {
                targets = charactersInPlaygroundForUpdate;
            }
            else if (characCtrl.AllianceType == AllianceType.Player)
            {
                targets = enemiesInPlaygroundForUpdate;
            }

            if (targets.Count == 0)
            {
                return null;
            }

            var minDest = float.MaxValue; // (recognitionRange * recognitionRange);
            var dis = float.MaxValue;

            for (var idx = 0; idx < targets.Count; ++idx)
            {
                if (targets[idx].IsAlive == false)
                {
                    continue;
                }

                dis = Vector3.SqrMagnitude(characCtrl.Position - targets[idx].Position);
                if (minDest > dis)
                {
                    minDest = dis;

                    target = targets[idx];
                }
            }

            return target;
        }

        public CharacterController GetAssembleTarget()
        {
            CharacterController target = null;

            target = charactersInPlaygroundForUpdate[0].target;

            return target;
        }
        //
        // public CharacterController GetRandomEnemy(CharacterController myUnit)
        // {
        //     CharacterController target = null;
        //
        //     List<CharacterController> targetlist = enemiesInPlaygroundForUpdate;
        //
        //     var minDest = float.MaxValue; //(recognitionRange * recognitionRange);
        //     var dis = float.MaxValue;
        //
        //     for (var idx = 0; idx < targetlist.Count; ++idx)
        //     {
        //         if (!targetlist[idx].IsAlive || targetlist[idx].GetTargetAttackerPlayerList().Count > 0)
        //         {
        //             continue;
        //         }
        //
        //         dis = GameUtil.DistanceNoSqrt((Vector2) myUnit.Position, (Vector2) targetlist[idx].Position);
        //
        //         if (minDest > dis)
        //         {
        //             minDest = dis;
        //
        //             target = targetlist[idx];
        //         }
        //     }
        //
        //     if (target == null)
        //     {
        //         for (var idx = 0; idx < targetlist.Count; ++idx)
        //         {
        //             if (targetlist[idx].IsAlive == false)
        //             {
        //                 continue;
        //             }
        //
        //             dis = GameUtil.DistanceNoSqrt((Vector2) myUnit.Position, (Vector2) targetlist[idx].Position);
        //
        //             if (minDest > dis)
        //             {
        //                 minDest = dis;
        //
        //                 target = targetlist[idx];
        //             }
        //         }
        //     }
        //
        //     if (target != null)
        //     {
        //         target.SetTargetAttackerPlayer(myUnit);
        //     }
        //
        //     return target;
        // }
        //
        // public CharacterController GetRandomTargetEnemy(CharacterController myUnit)
        // {
        //     CharacterController target = null;
        //
        //     List<CharacterController> targets = null;
        //
        //     if (myUnit.GetCharacterView().allianceType == AllianceType.Enemy)
        //     {
        //         targets = charactersInPlaygroundForUpdate;
        //     }
        //     else if (myUnit.GetCharacterView().allianceType == AllianceType.Player || myUnit.GetCharacterView().allianceType == AllianceType.SubPlayer || myUnit.GetCharacterView().allianceType == AllianceType.Summon)
        //     {
        //         targets = enemyCtrlsInPlaygroundForUpdate;
        //     }
        //
        //     if (targets.Count != 0)
        //     {
        //         int ran = Random.Range(0, targets.Count);
        //
        //         if (targets[ran].IsAlive == false)
        //         {
        //             return null;
        //         }
        //         else
        //         {
        //             return targets[ran];
        //         }
        //     }
        //
        //     return null;
        // }

        // 타겟 기준 원뿔 범위 내 모든 캐릭터
        // public List<CharacterController> GetCharactersWithTargetByCornRange(CharacterController target, float overRange, float nearRange, float cornAngle)
        // {
        //     List<CharacterController> targets = ListPool<CharacterController>.Get();
        //
        //     var bossPos = boss.Position;
        //     var targetPos = target.Position;
        //     var diff = bossPos - targetPos;
        //     var diffSqrMag = diff.sqrMagnitude;
        //     float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        //     float angleRange = cornAngle / 2f;
        //     float angleMin = angle - angleRange;
        //     float angleMax = angle + angleRange;
        //
        //     for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
        //     {
        //         CharacterController other = charactersInPlaygroundForUpdate[i];
        //         if (!other.IsAlive)
        //         {
        //             continue;
        //         }
        //
        //         var otherPos = other.Position;
        //         var otherDiff = bossPos - otherPos;
        //         var otherDiffSqrMag = Mathf.Abs(otherDiff.sqrMagnitude - diffSqrMag);
        //         bool isInRange = nearRange * nearRange <= otherDiffSqrMag && otherDiffSqrMag <= overRange * overRange;
        //         if (!isInRange)
        //         {
        //             continue;
        //         }
        //
        //         float otherAngle = Mathf.Atan2(otherDiff.y, otherDiff.x) * Mathf.Rad2Deg;
        //         bool isAngleInRange = angleMin <= otherAngle && otherAngle <= angleMax;
        //         if (!isAngleInRange)
        //         {
        //             continue;
        //         }
        //
        //         targets.Add(other);
        //     }
        //
        //     return targets;
        // }

        //특정 오브젝트를 기준으로 범위 내 모든 캐릭터들
        // public List<CharacterController> GetCharactersInRangeByObject(float range, Vector2 objPos)
        // {
        //     List<CharacterController> targets = ListPool<CharacterController>.Get();
        //     for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
        //     {
        //         CharacterController other = charactersInPlaygroundForUpdate[i];
        //         if (!other.IsAlive)
        //         {
        //             continue;
        //         }
        //
        //         Vector2 posDiff = other.Position - objPos;
        //         if (posDiff.sqrMagnitude <= range * range)
        //         {
        //             targets.Add(other);
        //         }
        //     }
        //
        //     return targets;
        // }

        #region 아군 탐색
        // public int GetAllHeroCount()
        // {
        //     List<CharacterController> targets = ListPool<CharacterController>.Get();
        //
        //     for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
        //     {
        //         CharacterController other = charactersInPlaygroundForUpdate[i];
        //
        //         if (other.AllianceType == AllianceType.Summon)
        //         {
        //             continue;
        //         }
        //
        //         targets.Add(other);
        //     }
        //
        //     return targets.Count;
        // }

        public void GetAllAliveCharacters(ref List<CharacterController> targets)
        {
            targets.Clear();
            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                CharacterController other = charactersInPlaygroundForUpdate[i];

                if (!other.IsAlive)
                {
                    continue;
                }

                targets.Add(other);
            }
        }

        public CharacterController GetAnyCharacter()
        {
            CharacterController res = null;
            List<CharacterController> targets = ListPool<CharacterController>.Get();
            GetAllAliveCharacters(ref targets);
            if (targets.Count == 0)
            {
                ListPool<CharacterController>.Release(targets);
                return null;
            }

            InGameRandomManager.UniversalShuffle(targets);

            res = targets[0];
            ListPool<CharacterController>.Release(targets);
            return res;
        }

        //자신을 기준으로 범위내 랜덤 아군
        public List<CharacterController> GetRandomColleaguesInRange(CharacterController owner, float range, bool includeOwner, int count = int.MaxValue, CharacterController exception = null)
        {
            List<CharacterController> colleagues = ListPool<CharacterController>.Get();

            List<int> indices = null;
            if (count != int.MaxValue)
            {
                // 인덱스를 셔플해서 매번 같은 캐릭터가 나오지않게 만들자.
                indices = ListPool<int>.Get();
                for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
                {
                    indices.Add(i);
                }

                InGameRandomManager.UniversalShuffle(indices);
                ListPool<int>.Release(indices);
            }

            for (var i = 0; i < charactersInPlaygroundForUpdate.Count; i++)
            {
                int index = indices != null ? indices[i] : i;
                CharacterController other = charactersInPlaygroundForUpdate[index];
                if (!other.IsAlive)
                {
                    continue;
                }

                if (exception != null)
                {
                    if (exception == other)
                    {
                        continue;
                    }
                }

                if (!includeOwner)
                {
                    if (owner == other)
                    {
                        continue;
                    }
                }

                Vector2 posDiff = other.Position - owner.Position;
                bool isInRange = posDiff.sqrMagnitude <= range * range;
                if (isInRange)
                {
                    colleagues.Add(other);
                    if (colleagues.Count >= count)
                    {
                        break;
                    }
                }
            }

            return colleagues;
        }
        #endregion
    }
}
