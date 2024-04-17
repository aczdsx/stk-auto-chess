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

        #region 탐색
        public List<CharacterController> GetEnemiesList()
        {
            return enemiesInPlaygroundForUpdate;
        }

        /// <summary>
        /// pivot을 기준으로 range내에 있는 동료들을 반환
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="range"></param>
        /// <param name="includePivot"></param>
        /// <param name="resTargets"></param>
        public void GetNearestColleaguesInRange(CharacterController pivot, float range, bool includePivot, List<CharacterController> resTargets)
        {
            List<CharacterController> searchList = null;
            if (pivot.AllianceType == AllianceType.Player)
            {
                searchList = enemiesInPlaygroundForUpdate;
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                searchList = charactersInPlaygroundForUpdate;
            }

            if (searchList == null)
                return;

            for (var i = 0; i < searchList.Count; i++)
            {
                CharacterController other = searchList[i];
                if (!includePivot && other == pivot)
                {
                    continue;
                }
                if (other is not {IsAlive: true})
                {
                    continue;
                }

                Vector2 distance = pivot.Position - other.Position;
                bool isInRange = distance.sqrMagnitude < range * range;
                if (isInRange)
                {
                    resTargets.Add(other);
                }
            }
        }

        /// <summary>
        /// pivot을 기준으로 range내에 있는 적들을 반환
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="range"></param>
        /// <param name="resTargets"></param>
        public void GetNearestEnemiesInRange(CharacterController pivot, float range, List<CharacterController> resTargets)
        {
            List<CharacterController> searchList = null;
            if (pivot.AllianceType == AllianceType.Player)
            {
                searchList = enemiesInPlaygroundForUpdate;
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
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

                Vector2 distance = pivot.Position - other.Position;
                bool isInRange = distance.sqrMagnitude < range * range;
                if (isInRange)
                {
                    resTargets.Add(other);
                }
            }
        }

        /// <summary>
        /// pivot을 기준으로 가장 가까운 적을 반환
        /// </summary>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public CharacterController GetNearestEnemy(CharacterController pivot)
        {
            CharacterController target = null;

            List<CharacterController> targets = null;

            if (pivot.AllianceType == AllianceType.Enemy)
            {
                targets = charactersInPlaygroundForUpdate;
            }
            else if (pivot.AllianceType == AllianceType.Player)
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

                var distance = Vector3.SqrMagnitude(pivot.Position - targets[idx].Position);
                if (minDistance > distance)
                {
                    minDistance = distance;

                    target = targets[idx];
                }
            }

            return target;
        }

        /// <summary>
        /// pivot을 기준으로 가장 먼 적을 반환
        /// </summary>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public CharacterController GetFarthestEnemy(CharacterController pivot)
        {
            CharacterController target = null;

            List<CharacterController> targets = null;

            if (pivot.AllianceType == AllianceType.Enemy)
            {
                targets = charactersInPlaygroundForUpdate;
            }
            else if (pivot.AllianceType == AllianceType.Player)
            {
                targets = enemiesInPlaygroundForUpdate;
            }

            if (targets == null || targets.Count == 0)
            {
                return null;
            }

            var maxDistance = 0f;
            for (var idx = 0; idx < targets.Count; ++idx)
            {
                if (targets[idx].IsAlive == false)
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(pivot.Position - targets[idx].Position);
                if (maxDistance < distance)
                {
                    maxDistance = distance;

                    target = targets[idx];
                }
            }

            return target;
        }

        /// <summary>
        /// type과 동일한 모든 캐릭터를 반환
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resTargets"></param>
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

        /// <summary>
        /// type과 동일한 랜덤한 캐릭터를 반환
        /// </summary>
        /// <param name="allianceType"></param>
        /// <returns></returns>
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

        // TODO: 부채꼴 검색, 직선 검색등 검색 기능이 많이 필요할텐데.. 모듈 내에 이 코드가 있는게 맞을지 고민해보자.
        #endregion
    }
}
