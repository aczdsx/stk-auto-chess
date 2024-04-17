using System;
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
                if (charactersInPlaygroundForUpdate[i].CharacterUId == characUId)
                {
                    return charactersInPlaygroundForUpdate[i];
                }
            }

            return null;
        }

        public async UniTask<CharacterController> AddCharacterToField(ICharacterStatData statData, Vector2 initPos, AllianceType allianceType, Type startStateType)
        {
            var characCtrl = new CharacterController();
            characCtrl.Initialize(statData, initPos, allianceType);
            characCtrl.GetCharacterView().CachedTr.SetParent(Playground, false);

            if (allianceType == AllianceType.Player)
            {
                charactersInPlaygroundForUpdate.Add(characCtrl);
            }
            else if (allianceType == AllianceType.Enemy)
            {
                enemiesInPlaygroundForUpdate.Add(characCtrl);
            }

            characCtrl.AddNextState(startStateType);

            return characCtrl;
        }

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

        public void GetNearestColleaguesInRange(CharacterController self, float range, List<CharacterController> resTargets)
        {
            List<CharacterController> searchList = null;
            if (self.AllianceType == AllianceType.Player)
            {
                searchList = enemiesInPlaygroundForUpdate;
            }
            else if (self.AllianceType == AllianceType.Enemy)
            {
                searchList = charactersInPlaygroundForUpdate;
            }

            if (searchList == null)
                return;

            for (var i = 0; i < searchList.Count; i++)
            {
                CharacterController other = searchList[i];
                if (other is not {IsAlive: true})
                {
                    continue;
                }

                Vector2 distance = self.Position - other.Position;
                bool isInRange = distance.sqrMagnitude < range * range;
                if (isInRange)
                {
                    resTargets.Add(other);
                }
            }
        }

        // 타겟 기준 범위
        public void GetNearestEnemiesInRange(CharacterController self, float range, List<CharacterController> resTargets)
        {
            List<CharacterController> searchList = null;
            if (self.AllianceType == AllianceType.Player)
            {
                searchList = enemiesInPlaygroundForUpdate;
            }
            else if (self.AllianceType == AllianceType.Enemy)
            {
                searchList = charactersInPlaygroundForUpdate;
            }

            if (searchList == null)
                return;

            for (var i = 0; i < searchList.Count; i++)
            {
                CharacterController other = searchList[i];
                if (other is not {IsAlive: true})
                {
                    continue;
                }

                Vector2 distance = self.Position - other.Position;
                bool isInRange = distance.sqrMagnitude < range * range;
                if (isInRange)
                {
                    resTargets.Add(other);
                }
            }
        }

        public CharacterController GetNearestEnemy(CharacterController self)
        {
            CharacterController target = null;

            List<CharacterController> targets = null;

            if (self.AllianceType == AllianceType.Enemy)
            {
                targets = charactersInPlaygroundForUpdate;
            }
            else if (self.AllianceType == AllianceType.Player)
            {
                targets = enemiesInPlaygroundForUpdate;
            }

            if (targets == null || targets.Count == 0)
            {
                return null;
            }

            var minDistance = float.MaxValue;
            for (var idx = 0; idx < targets.Count; ++idx)
            {
                if (targets[idx].IsAlive == false)
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(self.Position - targets[idx].Position);
                if (minDistance > distance)
                {
                    minDistance = distance;

                    target = targets[idx];
                }
            }

            return target;
        }

        #region 아군 탐색
        public void GetAllAliveCharacters(AllianceType type, List<CharacterController> resTargets)
        {
            List<CharacterController> searchList = null;
            if (type == AllianceType.Player)
            {
                searchList = enemiesInPlaygroundForUpdate;
            }
            else if (type == AllianceType.Enemy)
            {
                searchList = charactersInPlaygroundForUpdate;
            }

            if (searchList == null)
                return;

            resTargets.Clear();
            for (var i = 0; i < searchList.Count; i++)
            {
                CharacterController other = searchList[i];

                if (!other.IsAlive)
                {
                    continue;
                }

                resTargets.Add(other);
            }
        }

        public CharacterController GetAnyCharacter(AllianceType allianceType)
        {
            CharacterController res = null;
            using var _ = ListPool<CharacterController>.Get(out List<CharacterController> targets);
            GetAllAliveCharacters(allianceType, targets);
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
        public List<CharacterController> GetRandomColleaguesInRange(CharacterController self, float range, bool includeOwner, int count = int.MaxValue, CharacterController exception = null)
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
                    if (self == other)
                    {
                        continue;
                    }
                }

                Vector2 posDiff = other.Position - self.Position;
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
