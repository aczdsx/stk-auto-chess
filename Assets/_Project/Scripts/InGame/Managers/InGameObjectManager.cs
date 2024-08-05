using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
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
        public List<CharacterController> StartingPlayerCharacters => startingPlayerCharacters;
        public List<CharacterController> EnemiesInPlaygroundForUpdate => enemiesInPlaygroundForUpdate;

        private InGameGrid _grid;
        private InGameStage _stage;
        private List<CharacterController> enemiesInPlaygroundForUpdate = new();
        private List<CharacterController> charactersInPlaygroundForUpdate = new();
        private List<CharacterController> neutralInPlaygroundForUpdate = new();
        private List<CharacterController> nonStatObstacleInPlaygroundForUpdate = new();

        private List<CharacterController> startingPlayerCharacters = new();
        private List<CharacterController> reusableList = new List<CharacterController>();

        private List<InGameVfx> _synergyVfxList = new();
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

            playground = GameObject.FindWithTag("Playground").GetComponent<Transform>();
            startingPlayerCharacters = new();

            _stage = stage;
            InGameGrid grid = new InGameGrid(_stage.GridSize, _stage.TileViews);
            _grid = grid;
        }

        public void Clear()
        {
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
            InGameMainFlowManager.Instance.RemoveLateUpdateListener(LateManagedUpdate);
            playground = null;
            ClearAllCharactersInField(AllianceType.Player);
            ClearAllCharactersInField(AllianceType.Enemy);
            ClearAllCharactersInField(AllianceType.Neutral);
            ClearAllCharactersInField(AllianceType.Wall);
            ClearSynergyFx();
        }

        public List<CharacterController> GetCharacterList(AllianceType allianceType)
        {
            if (allianceType == AllianceType.Player)
            {
                return charactersInPlaygroundForUpdate;
            }
            else if (allianceType == AllianceType.Enemy)
            {
                return enemiesInPlaygroundForUpdate;
            }
            else if (allianceType == AllianceType.Wall)
            {
                return nonStatObstacleInPlaygroundForUpdate;
            }
            
            return neutralInPlaygroundForUpdate;
        }

        public List<CharacterController> GetCharacterListSortedByHpRate(AllianceType allianceType, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter 
                ? (allianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (allianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderBy(c => c.CurrentHp).ToList();
        }
        
        public List<CharacterController> GetCharacterListSortedByADDescending(AllianceType allianceType, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter 
                ? (allianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (allianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderByDescending(c => c.AD).ToList();
        }
        
        public List<CharacterController> GetCharacterListSortedByDistance(CharacterController character, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter 
                ? (character.AllianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (character.AllianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderBy(c => Vector3.Distance(character.Position, c.Position)).ToList();
        }
        
        public List<CharacterController> GetCharacterListSortedByDistanceDescending(CharacterController character, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter 
                ? (character.AllianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (character.AllianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderByDescending(c => Vector3.Distance(character.Position, c.Position)).ToList();
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

            for (var i = 0; i < enemiesInPlaygroundForUpdate.Count; i++)
            {
                if (enemiesInPlaygroundForUpdate[i].CharacterUId == characUId)
                {
                    return enemiesInPlaygroundForUpdate[i];
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
            AllianceType allianceType, Type startStateType, bool hasSkill = true, HpBarType type = HpBarType.None)
        {
            var characCtrl = new CharacterController();
            var tile = _grid.GetTile(initPos);
            await characCtrl.Initialize(statData, tile, allianceType, hasSkill, type);
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
            else if (allianceType == AllianceType.Neutral)
            {
                neutralInPlaygroundForUpdate.Add(characCtrl);
                summonVfxType = InGameVfxNameType.fx_common_summon_enemy;
            }

            if (summonVfxType != InGameVfxNameType.NONE)
            {
                var vfx = InGameVfxManager.Instance.AddInGameVfx(summonVfxType, tile.View.CachedTr.position);
                vfx.Initialize(false);

                if (SoundManager.Instance.IsPlayingGacha == false)
                {
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_spawn);
                }
            }

            characCtrl.AddNextState(startStateType);

            return characCtrl;
        }

        public void RemoveCharacterFromField(CharacterController characCtrl)
        {
            List<CharacterController> targetList = GetCharacterList(characCtrl.AllianceType);

            if (characCtrl.AllianceType != AllianceType.Wall)
            {
                for (var i = 0; i < targetList.Count; i++)
                {
                    CharacterController other = targetList[i];
                    other.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(characCtrl);
                }
            }

            targetList.Remove(characCtrl);
            if (characCtrl.AllianceType == AllianceType.Player)
            {
                var uiLayer = SceneUILayerManager.Instance.GetUILayer("BattleStatisticsPopup");
                if (uiLayer != null)
                    uiLayer.GetComponent<BattleStatisticsPopup>().SetDeadSlot(characCtrl.CharacterId);
            }

            characCtrl.Clear();
        }

        public async UniTask<CharacterController> AddNonStatObstacleToField(ObfuscatorInt gridID, ObfuscatorInt chapterID,
            AllianceType allianceType)
        {
            var characCtrl = new CharacterController();
            var tile = InGameGrid.GetTile(gridID);
            if (tile.OccupiedCharacter == null)
            {
                await characCtrl.Initialize(tile, Playground, chapterID, allianceType);
                nonStatObstacleInPlaygroundForUpdate.Add(characCtrl);
            }
            return characCtrl;
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

        public void ClearAllCharactersInField(AllianceType allianceType)
        {
            List<CharacterController> targetList = GetCharacterList(allianceType);

            for (var i = 0; i < targetList.Count; i++)
            {
                targetList[i].Clear();
            }

            targetList.Clear();
        }

        public InGameTile GetInGameTile(int id)
        {
            return _grid.GetTile(id);
        }
        
        public InGameTile GetInGameTile(int2 pos)
        {
            return _grid.GetTile(pos);
        }
        
        public int2 GetInOppositePosition(int2 pos)
        {
            int x = _grid.Width - pos.x - 1;
            int y = _grid.Height - pos.y - 1;
            return new int2(x, y);
        }

        public InGameTile[] GetAllInGameTiles()
        {
            return _grid.GetAllTiles();
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

            selectedCharacterTile.SetUnoccupied();
            occupiedCharacterTile.SetUnoccupied();
            // 각 캐릭터가 새로운 타일로 이동하도록 설정
            selectedCharacter.ChangeOccupiedTile(occupiedCharacterTile);
            occupiedCharacter.ChangeOccupiedTile(selectedCharacterTile);
        }

        public void SaveStartingPlayerCharacter()
        {
            startingPlayerCharacters.Clear();
            startingPlayerCharacters.AddRange(charactersInPlaygroundForUpdate);
            UserDataManager.Instance.SetUserCharaceterBattleDeckList(InGameResourceHolder.InGameType, startingPlayerCharacters);

            charactersInPlaygroundForUpdate = charactersInPlaygroundForUpdate
                .OrderBy(character => character.SpecCharacter.atk_range).ToList();
            enemiesInPlaygroundForUpdate =
                enemiesInPlaygroundForUpdate.OrderBy(enemy => enemy.SpecCharacter.atk_range).ToList();
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

        public List<CharacterController> GetAllCharacterList()
        {
            return enemiesInPlaygroundForUpdate.Concat(charactersInPlaygroundForUpdate).ToList();
        }

        public bool IsInRange(CharacterController pivot, CharacterController target)
        {
            if (pivot == null || target == null)
            {
                return false;
            }

            if (target.SpecCharacter.size > 0)
            {
                var targetTiles = InGameGrid.GetTileListByShapeSquare(target.CurrentTile, target.SpecCharacter.size);
                foreach (var targetTile in targetTiles)
                {
                    if (_grid.IsInRange(pivot.CurrentTile, targetTile, pivot.AttackRange))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
                return _grid.IsInRange(pivot.CurrentTile, target.CurrentTile, pivot.AttackRange);
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
            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            if (reusableList == null)
                return;

            for (var i = 0; i < reusableList.Count; i++)
            {
                CharacterController other = reusableList[i];
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
        public CharacterController GetNearestTargetByBFS(CharacterController pivot)
        {
            CharacterController target = null;

            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var minDistance = float.MaxValue;
            foreach (var enemy in reusableList)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                var distance = _grid.BFS(pivot.CurrentTile, enemy.CurrentTile);
                if (minDistance > distance)
                {
                    minDistance = distance;
                    target = enemy;
                }
            }

            return target;
        }
        
        public CharacterController GetNearestTargetByManhattanDistance(CharacterController pivot)
        {
            CharacterController target = null;

            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var minDistance = float.MaxValue;
            foreach (var enemy in reusableList)
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

        public CharacterController GetTargetForMove(CharacterController pivot)
        {
            CharacterController target = null;

            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var minDistance = float.MaxValue;
            foreach (var enemy in reusableList)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                int distance = 0;
                if (pivot.AttackRange == 1)
                {
                    distance = _grid.BFS(pivot.CurrentTile, enemy.CurrentTile);
                }
                else
                {
                    distance = _grid.GetManhattanDistance(pivot.CurrentTile, enemy.CurrentTile);
                }
                
                if (minDistance > distance)
                {
                    minDistance = distance;
                    target = enemy;
                }
            }

            return target;
        }

        public CharacterController GetNearestTargetOnce(CharacterController pivot)
        {
            CharacterController target = null;

            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var minDistance = float.MaxValue;
            foreach (var enemy in reusableList)
            {
                if (enemy.IsAlive == false || enemy.GetCharacterStat().Spec.character_position_type ==
                    CharacterPositionType.ASSASSIN)
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

        public CharacterController GetFarthestTargetByOnce(CharacterController pivot)
        {
            CharacterController target = null;

            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var maxDistance = float.MinValue;
            var sortedTargets = pivot.AllianceType == AllianceType.Enemy
                ? reusableList.OrderBy(t => t.CurrentTile.Y).ToList()
                : reusableList.OrderByDescending(t => t.CurrentTile.Y).ToList();

            foreach (var enemy in sortedTargets)
            {
                if (enemy.IsAlive == false || enemy.GetCharacterStat().Spec.character_position_type ==
                    CharacterPositionType.ASSASSIN)
                {
                    continue;
                }

                var distance = _grid.GetManhattanDistance(pivot.CurrentTile, enemy.CurrentTile);
                if (maxDistance < distance)
                {
                    if (target != null && target.CurrentTile.Y != enemy.CurrentTile.Y)
                        return target;
                    else
                    {
                        maxDistance = distance;
                        target = enemy;
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// type과 동일한 모든 캐릭터를 반환
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resTargets"></param>
        public void GetAllAliveOnlyCharacters(AllianceType type, List<CharacterController> resTargets)
        {
            reusableList.Clear();
            if (type == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
            }
            else if (type == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
            }
            else if (type == AllianceType.Neutral)
            {
                reusableList = new List<CharacterController>(neutralInPlaygroundForUpdate);
            }

            if (reusableList == null)
                return;

            resTargets.Clear();
            for (var i = 0; i < reusableList.Count; i++)
            {
                CharacterController other = reusableList[i];

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
            GetAllAliveOnlyCharacters(allianceType, targets);
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

            for (var i = 0; i < neutralInPlaygroundForUpdate.Count; i++)
            {
                neutralInPlaygroundForUpdate[i].ManagedUpdate(dt);
            }

            var effectCodes = InGameManager.Instance.EffectCodeContainer.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeGameLambda.CallOnUpdateLambda, dt);
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

        public string GetAttrText(AllianceType type)
        {
            double attrValue = 0;
            List<CharacterController> characterList = (type == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;

            foreach (var character in characterList)
            {
                attrValue += character.GetCharacterStat().GetAttrValue();
            }

            return attrValue.ToString($"n0");
        }
        
        public double GetAttr(AllianceType type)
        {
            double attrValue = 0;
            List<CharacterController> characterList = (type == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;

            foreach (var character in characterList)
            {
                attrValue += character.GetCharacterStat().GetAttrValue();
            }

            return attrValue;
        }

        public void SpawnSynergyFx(AllianceType type, ElementType elementType)
        {
            List<CharacterController> targetList = (type == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;

            foreach (var character in targetList)
            {
                InGameVfxNameType inGameVfxNameType = InGameVfxNameType.NONE;
                if (elementType == ElementType.FIRE)
                {
                    inGameVfxNameType = InGameVfxNameType.fx_common_synergy_fire;
                }
                else if (elementType == ElementType.WATER)
                {
                    inGameVfxNameType = InGameVfxNameType.fx_common_synergy_water;
                }
                else if (elementType == ElementType.DARK)
                {
                    inGameVfxNameType = InGameVfxNameType.fx_common_synergy_darkness;
                }
                else if (elementType == ElementType.LIGHT)
                {
                    inGameVfxNameType = InGameVfxNameType.fx_common_synergy_light;
                }
                else if (elementType == ElementType.EARTH)
                {
                    inGameVfxNameType = InGameVfxNameType.fx_common_synergy_ground;
                }
                else if (elementType == ElementType.WIND)
                {
                    inGameVfxNameType = InGameVfxNameType.fx_common_synergy_wind;
                }

                if (character.SpecCharacter.element_type == elementType)
                {
                    _synergyVfxList.Add(InGameVfxManager.Instance.AddInGameVfxByTransform(inGameVfxNameType,
                        character.GetCharacterView().CachedTr));
                }
            }
        }

        public void ClearSynergyFx()
        {
            _synergyVfxList.ForEach(vfx =>
            {
                vfx.transform.SetParent(Playground);
                vfx.Remove();
            });
            _synergyVfxList.Clear();
        }
    }
}
