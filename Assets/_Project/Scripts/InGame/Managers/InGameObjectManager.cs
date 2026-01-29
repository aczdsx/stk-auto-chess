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

namespace CookApps.BattleSystem
{
    public class InGameObjectManager : SingletonMonoBehaviour<InGameObjectManager>
    {
        private Transform playground;
        public Transform Playground => playground;
        public InGameGrid InGameGrid => _grid;
        public InGameStage InGameStage => _stage;
        public List<CharacterController> StartingPlayerCharacters => startingPlayerCharacters;
        public List<CharacterController> StartingEnemiesCharacters => startingEnemiesCharacters;
        public List<CharacterController> EnemiesInPlaygroundForUpdate => enemiesInPlaygroundForUpdate;

        private InGameGrid _grid;
        private InGameStage _stage;
        private List<CharacterController> enemiesInPlaygroundForUpdate = new();
        private List<CharacterController> charactersInPlaygroundForUpdate = new();
        private List<CharacterController> neutralInPlaygroundForUpdate = new();
        private List<CharacterController> battleItemInPlaygroundForUpdate = new();
        private List<CharacterController> nonStatObstacleInPlaygroundForUpdate = new();

        private List<CharacterController> startingPlayerCharacters = new();
        private List<CharacterController> startingEnemiesCharacters = new();
        private List<CharacterController> reusableList = new List<CharacterController>();

        private List<InGameVfxTargetLine> playerTargetLines = new List<InGameVfxTargetLine>();
        private List<InGameVfxTargetLine> enemyTargetLines = new List<InGameVfxTargetLine>();

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

            // URP Light 큐 강제 갱신: Addressables로 로드된 프리팹의 Light가 첫 프레임에 등록되지 않는 버그 수정
            var lights = stageObj.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].gameObject.SetActive(false);
                lights[i].gameObject.SetActive(true);
            }

            if (!stageObj.TryGetComponent(out InGameStage stage))
            {
                Debug.LogError("InGameStage is not found");
                return;
            }

            playground = GameObject.FindWithTag("Playground").GetComponent<Transform>();
            startingPlayerCharacters = new();
            startingEnemiesCharacters = new List<CharacterController>();

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
            ClearAllCharactersInField(AllianceType.BattleItem);
            ClearAllCharactersInField(AllianceType.Wall);
            ClearTargetLine();
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
            else if (allianceType == AllianceType.BattleItem)
            {
                return battleItemInPlaygroundForUpdate;
            }

            return neutralInPlaygroundForUpdate;
        }

        public List<CharacterController> GetCharacterListSortedByCurrentHPDescending(AllianceType allianceType, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter
                ? (allianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (allianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderByDescending(c => c.CurrentHp).ToList();
        }

        public List<CharacterController> GetCharacterListSortedByHPRateDescending(AllianceType allianceType, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter
                ? (allianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (allianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderByDescending(c => c.CurrentHp / c.HP).ToList();
        }

        public List<CharacterController> GetCharacterListSortedByADDescending(AllianceType allianceType, bool isOwnCharacter)
        {
            List<CharacterController> characterList = isOwnCharacter
                ? (allianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (allianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            return characterList.OrderByDescending(c => c.AD).ToList();
        }

        public int GetAverageAD(AllianceType allianceType, bool isOwnCharacter)
        {
            double averageAD = 0;
            List<CharacterController> list = isOwnCharacter
                ? (allianceType == AllianceType.Player ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate)
                : (allianceType == AllianceType.Player ? enemiesInPlaygroundForUpdate : charactersInPlaygroundForUpdate);

            foreach (var character in list)
            {
                averageAD += character.AD;
            }
            averageAD /= list.Count;

            return Convert.ToInt32(averageAD);
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

            for (var i = 0; i < battleItemInPlaygroundForUpdate.Count; i++)
            {
                if (battleItemInPlaygroundForUpdate[i].CharacterUId == characUId)
                {
                    return battleItemInPlaygroundForUpdate[i];
                }
            }

            return null;
        }

        public int GetCharacterSynergyCount(AllianceType allianceType, SynergyType synergyType)
        {
            int value = 0;

            List<CharacterController> targetList = (allianceType == AllianceType.Player)
                ? charactersInPlaygroundForUpdate
                : enemiesInPlaygroundForUpdate;


            foreach (var character in targetList)
            {
                if (DistinguishSynergyTypeHelper.IsAsterismSynergyType(synergyType))
                {
                    value += character.GetCharacterStat().Spec.character_stella_type == synergyType ? 1 : 0;
                }
                else if (DistinguishSynergyTypeHelper.IsElementSynergyType(synergyType))
                {
                    value += character.GetCharacterStat().Spec.character_element_type == synergyType ? 1 : 0;
                }
            }

            return value;
        }

        public async UniTask<CharacterController> AddCharacterToField(CharacterStatData statData, int2 initPos,
            AllianceType allianceType, Type startStateType, bool hasSkill = true, HpBarType type = HpBarType.None, bool isSummonFx = true)
        {
            var characCtrl = new CharacterController();
            var tile = _grid.GetTile(initPos);

            // 타일이 이미 점유되어 있으면 다른 빈 타일을 찾습니다
            if (tile.OccupiedCharacter != null)
            {
                // 배틀 아이템이 있는 타일도 제외하고 빈 타일 찾기
                var emptyTile = _grid.GetRecommandedTile(statData.Spec);
                if (emptyTile != null && emptyTile.OccupiedCharacter == null)
                {
                    tile = emptyTile;
                }
                else
                {
                    // GetRecommandedTile이 실패하면 GetPriorityEmptyTile 사용
                    tile = _grid.GetPriorityEmptyTile(allianceType == AllianceType.Player ? AllianceType.Player : null);
                    if (tile == null || tile.OccupiedCharacter != null)
                    {
                        Debug.LogWarning($"[AddCharacterToField] 빈 타일을 찾을 수 없습니다. CharacterId: {statData.CharacterId}, initPos: {initPos}");
                        // 그래도 원래 타일에 배치 시도 (기존 동작 유지)
                    }
                }
            }

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
            else if (allianceType == AllianceType.BattleItem)
            {
                battleItemInPlaygroundForUpdate.Add(characCtrl);
                summonVfxType = InGameVfxNameType.fx_common_summon_awful;
            }

            if (summonVfxType != InGameVfxNameType.NONE)
            {
                if (isSummonFx)
                {
                    var vfx = InGameVfxManager.Instance.AddInGameVfx(summonVfxType, tile.View.CachedTr.position);
                    vfx.Initialize(false);
                }

                if (SoundManager.Instance.IsPlayingGacha == false)
                {
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_spawn);
                }
            }

            characCtrl.AddNextState(startStateType);
            InGameSynergyManager.Instance.OnAddCharacter(characCtrl);

            return characCtrl;
        }

        /// <summary>
        /// 고스트 캐릭터 생성 (드래그 프리뷰용 - Synergy 미등록, 리스트 미등록)
        /// </summary>
        public async UniTask<CharacterController> CreateGhostCharacter(CharacterStatData statData, InGameTile tile)
        {
            var ghostCtrl = new CharacterController();

            // 타일 점유 없이 캐릭터만 초기화 (hasSkill: false로 스킬 미적용)
            await ghostCtrl.InitializeAsGhost(statData, tile, AllianceType.Player);
            ghostCtrl.GetCharacterView().CachedTr.SetParent(Playground, false);

            // Synergy 등록 안 함, 리스트에 추가 안 함
            // Material은 InGameTouchManager에서 SetHologramShader()로 적용
            ghostCtrl.AddNextState(typeof(CharacterStateReady));
            ghostCtrl.GetCharacterView().SetFirstDirection(AllianceType.Player);

            return ghostCtrl;
        }

        /// <summary>
        /// 고스트 캐릭터 제거 (Synergy 해제 없이 단순 제거)
        /// </summary>
        public void RemoveGhostCharacter(CharacterController ghostCtrl)
        {
            if (ghostCtrl == null) return;

            // 고스트는 타일을 점유하지 않으므로 SetUnoccupied 호출 불필요
            // 뷰만 제거
            ghostCtrl.ClearGhost();
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
            InGameSynergyManager.Instance.OnRemoveCharacter(characCtrl);

            characCtrl.Clear();
        }

        public async UniTask<CharacterController> AddNonStatObstacleToField(ObfuscatorInt gridID, ObfuscatorInt chapterID,
            AllianceType allianceType, bool isSpawnFx = false)
        {
            var characCtrl = new CharacterController();
            var tile = InGameGrid.GetTile(gridID);
            if (tile.OccupiedCharacter == null)
            {
                await characCtrl.Initialize(tile, Playground, chapterID, allianceType);
                nonStatObstacleInPlaygroundForUpdate.Add(characCtrl);

                if (isSpawnFx)
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_summon_awful,
                        tile.View.CachedTr.position);
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
            startingEnemiesCharacters.Clear();
            startingEnemiesCharacters.AddRange(enemiesInPlaygroundForUpdate);

            startingPlayerCharacters.Clear();
            startingPlayerCharacters.AddRange(charactersInPlaygroundForUpdate);

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

        public bool IsInRange(CharacterController pivot, CharacterController target, int addRange = 0)
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
                    if (_grid.IsInRange(pivot.CurrentTile, targetTile, pivot.AttackRange + addRange))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
                return _grid.IsInRange(pivot.CurrentTile, target.CurrentTile, pivot.AttackRange + addRange);
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
                if (other is not { IsAlive: true })
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
        /// BFS 기준으로 가장 거리가 짧은 타겟을 반환
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

        /// <summary>
        /// pivot을 기준으로 가장 체력이 낮은 우리 팀을 반환
        /// 자신을 제외합니다.
        /// </summary>
        /// <param name="pivot"></param>    
        /// <returns></returns>
        public CharacterController GetLowestHPOurTeam(CharacterController pivot)
        {
            CharacterController target = null;

            reusableList.Clear();
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible) || l.AllianceType == AllianceType.BattleItem);

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var minHP = double.MaxValue;
            foreach (var ourTeamCharacter in reusableList)
            {
                if (ourTeamCharacter.IsAlive == false || ourTeamCharacter == pivot)
                {
                    continue;
                }

                var curHPvalue = ourTeamCharacter.CurrentHp;
                if (minHP > curHPvalue)
                {
                    minHP = curHPvalue;
                    target = ourTeamCharacter;
                }
            }

            return target;
        }

        /// <summary>
        /// pivot을 기준으로 체력이 낮은 순서대로 정렬된 우리 팀 리스트를 반환
        /// 자신을 제외합니다.
        /// </summary>
        /// <param name="pivot"></param>
        /// <returns>체력이 낮은 순서대로 정렬된 리스트 (오름차순)</returns>
        public List<CharacterController> GetLowestHPOurTeamSorted(CharacterController pivot)
        {
            reusableList.Clear();

            // 우리 팀 리스트 구성
            if (pivot.AllianceType == AllianceType.Player)
            {
                reusableList = new List<CharacterController>(charactersInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }
            else if (pivot.AllianceType == AllianceType.Enemy)
            {
                reusableList = new List<CharacterController>(enemiesInPlaygroundForUpdate);
                reusableList.AddRange(neutralInPlaygroundForUpdate);
            }

            // 타겟 불가능한 캐릭터 제거
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible));

            // 살아있고, 자신이 아닌 캐릭터만 필터링 (for문으로 최적화)
            var filteredList = new List<CharacterController>(reusableList.Count);
            for (int i = 0; i < reusableList.Count; i++)
            {
                var character = reusableList[i];
                if (character.IsAlive && character != pivot)
                {
                    filteredList.Add(character);
                }
            }

            // 체력 비율(CurrentHP/HP) 기준 오름차순 정렬
            filteredList.Sort((a, b) =>
            {
                // 0으로 나누기 방지
                double hpA = a.HP;
                double hpB = b.HP;

                // HP가 0인 경우 처리 (비율을 0으로 처리)
                double ratioA = hpA > 0 ? a.CurrentHp / hpA : 0;
                double ratioB = hpB > 0 ? b.CurrentHp / hpB : 0;

                return ratioA.CompareTo(ratioB);
            });

            return filteredList;
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

        public CharacterController GetFarthestTargetByManhattanDistance(CharacterController pivot)
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
            foreach (var enemy in reusableList)
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

        //공격 적 타겟 찾기 + 못 찾았으면 [내가 공격 가능한 범위]까지 [최소한의 이동]으로 갈 수 있는 대상 찾기
        public CharacterController GetOptimalAttackTarget(CharacterController pivot)
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
            _grid.ClearReusableTilesHashSet();
            var minDistance = int.MaxValue;
            foreach (var enemy in reusableList)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                int distance = 0;
                distance = _grid.GetOptimalDistanceByAttackRange(pivot, enemy);

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
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible) || l.AllianceType == AllianceType.BattleItem);

            if (reusableList == null || reusableList.Count == 0)
            {
                return null;
            }

            var minDistance = float.MaxValue;
            foreach (var enemy in reusableList)
            {//여기 어쎄신 예외처리 되어있었ㅇ므.
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
            reusableList.RemoveAll(l => l.HasBuffDebuffType(BuffDebuffType.TargetImpossible) || l.AllianceType == AllianceType.BattleItem);

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
                if (enemy.IsAlive == false)
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

        public CharacterController GetTargetOnceByPositionType(CharacterController pivot)
        {
            var pivotPositionType = pivot.GetCharacterStat().Spec.character_position_type;
            if (pivotPositionType == CharacterPositionType.GHOST)
            {
                return GetFarthestTargetByOnce(pivot);
            }
            else if (pivotPositionType == CharacterPositionType.ORACLE)
            {
                return GetLowestHPOurTeam(pivot);
            }
            else
            {
                return GetNearestTargetOnce(pivot);
            }
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

            return (float)(currentHp / maxHp);
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

            for (var i = 0; i < battleItemInPlaygroundForUpdate.Count; i++)
            {
                battleItemInPlaygroundForUpdate[i].ManagedUpdate(dt);
            }

            var effectCodes = InGameManager.Instance.TeamEcc.GetEffectCodesByTypeByFlag(EffectCodeType.Game);
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
                attrValue += character.GetCharacterStat().GetAttrValueCP();
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
                attrValue += character.GetCharacterStat().GetAttrValueCP();
            }

            return attrValue;
        }

        public double GetStartingEnemiesAttr()
        {
            double attrValue = 0;
            foreach (var character in startingEnemiesCharacters)
            {
                attrValue += character.GetCharacterStat().GetAttrValueCP();
            }
            return attrValue;
        }

        public void DrawPlayerLine(bool isPlayer)
        {
            List<CharacterController> characterControllers = (isPlayer) ? charactersInPlaygroundForUpdate : enemiesInPlaygroundForUpdate;
            List<InGameVfxTargetLine> targetLines = (isPlayer) ? playerTargetLines : enemyTargetLines;

            // foreach (var line in targetLines)
            // {
            //     line.Remove();
            // }
            // targetLines.Clear();

            foreach (var anyCharacter in characterControllers)
            {
                var target = GetTargetOnceByPositionType(anyCharacter);

                if (target != null)
                {
                    InGameVfxTargetLine targetLine = null;
                    foreach (var line in targetLines)
                    {
                        if (line.CachedGo.activeSelf == false)
                        {
                            targetLine = line;
                            break;
                        }
                    }

                    if (targetLine == null)
                    {
                        targetLine = anyCharacter.SetLine(target, isPlayer,
                            (targetLine) =>
                            {
                                targetLine.SetActiveObject(false); //대기상태로 변경
                            });
                        targetLines.Add(targetLine);
                    }
                    else
                    {
                        anyCharacter.ReUseLine(targetLine, target, isPlayer,
                            (targetLine) =>
                            {
                                targetLine.SetActiveObject(false); //대기상태로 변경
                            });
                    }
                }
            }
        }

        public void ClearTargetLine()
        {
            foreach (var line in playerTargetLines) line.Remove();
            foreach (var line in enemyTargetLines) line.Remove();
            playerTargetLines.Clear();
            enemyTargetLines.Clear();
        }


    }
}
