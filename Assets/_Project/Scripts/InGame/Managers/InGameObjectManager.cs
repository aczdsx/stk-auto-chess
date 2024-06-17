using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace CookApps.BattleSystem
{
    public class InGameObjectManager : SingletonMonoBehaviour<InGameObjectManager>
    {
        private Transform playground;
        public Transform Playground => playground;
        public InGameGrid InGameGrid => _grid;
        public InGameStage InGameStage => _stage;

        private InGameGrid _grid;
        private InGameStage _stage;
        private List<CharacterController> enemiesInPlaygroundForUpdate = new();
        private List<CharacterController> charactersInPlaygroundForUpdate = new();

        private List<CharacterController> startingPlayerCharacters = new();
        private double _playerSumMaxHp;
        private double _enemySumMaxHp;
        private float _lastRate;

        public void Initialize()
        {
            InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects,
                ManagedUpdate);
            InGameMainFlowManager.Instance.AddLateUpdateListener(InGameMainFlowManager.UpdatePriority_Objects,
                LateManagedUpdate);

            GameObject stageObj = Instantiate(InGameResourceHolder.StagePrefab);
            if (!stageObj.TryGetComponent(out InGameStage stage))
            {
                Debug.LogError("InGameStage is not found");
                return;
            }

            playground = GameObject.Find("Playground").GetComponent<Transform>();

            _stage = stage;
            InGameGrid grid = new InGameGrid(_stage.GridSize, _stage.TileViews);
            _grid = grid;
        }

        public void Clear()
        {
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
            InGameMainFlowManager.Instance.RemoveLateUpdateListener(LateManagedUpdate);
            playground = null;
            ClearAllCharactersInField();
            ClearAllEnemiesInField();
        }

        public List<CharacterController> GetCharacterList()
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

        public int GetCharacterSynergyCount(AllianceType allianceType, ElementType type)
        {
            int value = 0;

            List<CharacterController> targetList = (allianceType == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;
            foreach (var character in targetList)
            {
                value += character.GetCharacterStat().Spec.element_type == type ? 1 : 0;
            }

            return value;
        }

        public int GetCharacterSynergyCount(AllianceType allianceType, CharacterPositionType type)
        {
            int value = 0;

            List<CharacterController> targetList = (allianceType == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;

            foreach (var character in targetList)
            {
                value += character.GetCharacterStat().Spec.character_position_type == type ? 1 : 0;
            }

            return value;
        }

        public async UniTask<CharacterController> AddCharacterToField(CharacterStatData statData, int2 initPos,
            AllianceType allianceType, Type startStateType)
        {
            var characCtrl = new CharacterController();
            var tile = _grid.GetTile(initPos);
            await characCtrl.Initialize(statData, tile, allianceType);
            characCtrl.GetCharacterView().CachedTr.SetParent(Playground, false);

            InGameVfxNameType summonVfxType = InGameVfxNameType.NONE;
            if (allianceType == AllianceType.Player)
            {
                charactersInPlaygroundForUpdate.Add(characCtrl);
                summonVfxType = InGameVfxNameType.fx_common_summon_awful;
            }
            else if (allianceType == AllianceType.Enemy)
            {
                enemiesInPlaygroundForUpdate.Add(characCtrl);
                summonVfxType = InGameVfxNameType.fx_common_summon_enemy;
            }

            if (summonVfxType != InGameVfxNameType.NONE)
            {
                var vfx = InGameVfxManager.Instance.AddInGameVfx(summonVfxType, tile.View.CachedTr.position);
                vfx.Initialize(false);
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

            characCtrl.CurrentTile.SetUnoccupied();
            characCtrl.Clear();
        }

        public bool IsCharacterAllIsAlive()
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

        public bool IsDieCharacterAllDead()
        {
            return charactersInPlaygroundForUpdate.Count == 0 ? true : false;
        }

        public void RemoveCharacterFromField(int characUId)
        {
            CharacterController charCtrl = GetCharacterInField(characUId);

            CharacterController target = null;

            if (charCtrl.Target != null)
            {
                target = charCtrl.Target;
            }

            RemoveCharacterFromField(charCtrl);
            if (target != null)
            {
                target.Target = null;
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

            characCtrl.CurrentTile.SetUnoccupied();
            characCtrl.Clear();
            enemiesInPlaygroundForUpdate.Remove(characCtrl);
        }

        public InGameTile GetInGameTile(int id)
        {
            return _grid.GetTile(id);
        }

        public InGameTile GetNextMovableTile(InGameTile src, InGameTile dest)
        {
            return _grid.GetNextMovableTile(src, dest);
        }

        public void ChangeTileCharacterToCharacter(CharacterController selectedCharacter,
            CharacterController occupiedCharacter)
        {
            // 임시 변수를 사용하여 타일을 교환
            InGameTile selectedCharacterTile = selectedCharacter.CurrentTile;
            InGameTile occupiedCharacterTile = occupiedCharacter.CurrentTile;

            // 각 캐릭터가 새로운 타일로 이동하도록 설정
            selectedCharacter.ChangeOccupiedTile(occupiedCharacterTile);
            occupiedCharacter.ChangeOccupiedTile(selectedCharacterTile);
        }

        public void ChangeTile(CharacterController selectedCharacter, InGameTile newTile)
        {
            selectedCharacter.ChangeOccupiedTile(newTile);
        }

        public void SaveStartingPlayerCharacter()
        {
            startingPlayerCharacters.AddRange(charactersInPlaygroundForUpdate);
        }

        public bool IsCheckAllPlayerCharacterAlive()
        {
            for (var i = 0; i < startingPlayerCharacters.Count; i++)
            {
                if (startingPlayerCharacters[i].IsAlive == false)
                {
                    return false;
                }
            }

            return true;
        }

        #region 탐색

        public List<CharacterController> GetEnemiesList()
        {
            return enemiesInPlaygroundForUpdate;
        }

        public bool IsInRange(CharacterController pivot, CharacterController target)
        {
            if (pivot == null || target == null)
            {
                return false;
            }

            return _grid.IsInRange(pivot.CurrentTile, target.CurrentTile, pivot.AttackRange);
        }

        /// <summary>
        /// pivot을 기준으로 range내에 있는 동료들을 반환
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="range"></param>
        /// <param name="rangeShapeType"></param>
        /// <param name="includePivot"></param>
        /// <param name="resTargets"></param>
        public void GetNearestColleaguesInRange(CharacterController pivot, int range,
            BattleSystem.AttackRangeShape rangeShape, bool includePivot, List<CharacterController> resTargets)
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

                if (_grid.IsInRange(pivot.CurrentTile, other.CurrentTile, range))
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
        /// <param name="rangeShapeType"></param>
        /// <param name="resTargets"></param>
        public void GetNearestEnemiesInRange(CharacterController pivot, int range, AttackRangeShape rangeShapeType,
            List<CharacterController> resTargets)
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

                if (_grid.IsInRange(pivot.CurrentTile, other.CurrentTile, range))
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
            foreach (var enemy in targets)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                var distance = _grid.GetManhattanDistance(pivot.CurrentTile, enemy.CurrentTile);
                if (minDistance > distance)
                {
                    minDistance = distance;
                    target = enemy;
                }
            }

            return target;
        }

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

            var maxDistance = float.MinValue;
            foreach (var enemy in targets)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                var distance = _grid.GetManhattanDistance(pivot.CurrentTile, enemy.CurrentTile);
                if (maxDistance < distance)
                {
                    maxDistance = distance;
                    target = enemy;
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
                searchList = charactersInPlaygroundForUpdate;
            }
            else if (type == AllianceType.Enemy)
            {
                searchList = enemiesInPlaygroundForUpdate;
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

        public float GetHpRate(AllianceType type)
        {
            List<CharacterController> targetList = (type == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;
            double maxHp = (type == AllianceType.Player) ? _playerSumMaxHp : _enemySumMaxHp;
            double currentHp = targetList.Sum(character => character.CurrentHp);

            return (float) (currentHp / maxHp);
        }

        public void UpdateSumMaxHp(AllianceType type)
        {
            if (type == AllianceType.Player)
            {
                _playerSumMaxHp = 0;
                foreach (var t in charactersInPlaygroundForUpdate)
                    _playerSumMaxHp += t.CurrentHp;
            }
            else
            {
                _enemySumMaxHp = 0;
                foreach (var t in enemiesInPlaygroundForUpdate)
                    _enemySumMaxHp += t.CurrentHp;
            }
        }

        // TODO: 부채꼴 검색, 직선 검색등 검색 기능이 많이 필요할텐데.. 모듈 내에 이 코드가 있는게 맞을지 고민해보자.

        #endregion

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
        }
    }
}
