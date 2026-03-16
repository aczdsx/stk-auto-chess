using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// IdleCombatRunner ↔ View 브릿지.
    /// 유휴 전투용 경량 브릿지로, GameWorld 없이 CombatMatchState + SimEventQueue만 사용.
    /// UnitDied / UI 이벤트는 무시 (유휴 전투 = 사망/UI 없음).
    /// </summary>
    public class IdleCombatViewBridge : MonoBehaviour
    {
        private IdleCombatRunner _runner;
        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private CombatVfxManager _combatVfxManager;

        private const int BoardIndex = 0;

        public void Setup(
            IdleCombatRunner runner,
            UnitViewManager unitViewManager,
            CombatViewManager combatViewManager,
            CombatVfxManager combatVfxManager = null)
        {
            _runner = runner;
            _unitViewManager = unitViewManager;
            _combatViewManager = combatViewManager;
            _combatVfxManager = combatVfxManager;
        }

        // ── 초기화 ──

        public void Initialize()
        {
            _unitViewManager.SetActiveBoard(BoardIndex);

            // IdleCombatRunner 이벤트 구독
            _runner.OnTick += HandleTick;
            _runner.OnCombatStarted += HandleCombatStarted;
            _runner.OnCombatStopped += HandleCombatStopped;
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.OnTick -= HandleTick;
                _runner.OnCombatStarted -= HandleCombatStarted;
                _runner.OnCombatStopped -= HandleCombatStopped;
            }
        }

        // ── 전투 시작/종료 ──

        private void HandleCombatStarted()
        {
            _unitViewManager.OnCombatStart();
            _combatViewManager.OnCombatStart();
        }

        private void HandleCombatStopped()
        {
            _unitViewManager.ForceAllCombatViewsIdle();
            _unitViewManager.OnCombatEnd();
            _combatViewManager.OnCombatEnd();
            _combatVfxManager?.OnCombatEnd();
        }

        // ── 틱 핸들러 ──

        private void HandleTick(CombatMatchState matchState)
        {
            // 이벤트 큐 먼저 처리 (ATK/ATK2/CRIT 애니메이션 타입 결정 → 상태 동기화에서 사용)
            ProcessEvents(matchState);

            // 전투 뷰 동기화
            _unitViewManager.SyncCombatUnits(matchState, BoardIndex);
        }

        // ── 이벤트 처리 ──

        private void ProcessEvents(CombatMatchState matchState)
        {
            var queue = _runner.EventQueue;
            if (queue == null) return;

            for (int i = 0; i < queue.Count; i++)
            {
                ref var evt = ref queue.Events[i];
                DispatchEvent(ref evt, matchState);
            }
            queue.Clear();
        }

        // ── 스폰 VFX ──

        private GameObject _summonPlayerVfx;
        private GameObject _summonEnemyVfx;

        public void SetSpawnVfxPrefabs(GameObject playerVfx, GameObject enemyVfx)
        {
            _summonPlayerVfx = playerVfx;
            _summonEnemyVfx = enemyVfx;
        }

        private void SpawnSummonVfx(ref SimEvent evt, CombatMatchState matchState)
        {
            int combatId = evt.EntityId;
            int unitIdx = matchState.FindUnitIndex(combatId);
            if (unitIdx < 0) return;

            ref var unit = ref matchState.Units[unitIdx];
            var prefab = unit.TeamIndex == 0 ? _summonPlayerVfx : _summonEnemyVfx;
            if (prefab == null) return;

            var worldPos = BoardWorldHelper.CombatGridToWorld(BoardIndex, unit.GridCol, unit.GridRow);
            var vfxObj = Object.Instantiate(prefab, worldPos, UnityEngine.Quaternion.identity);
            Object.Destroy(vfxObj, 3f);
        }

        private void DispatchEvent(ref SimEvent evt, CombatMatchState matchState)
        {
            switch (evt.Type)
            {
                case SimEventType.UnitSpawned:
                    SpawnSummonVfx(ref evt, matchState);
                    break;

                case SimEventType.UnitAttacked:
                {
                    bool isProjectile = (evt.Value1 & 1) != 0;
                    bool isPreTimed = (evt.Value1 & 2) != 0;
                    _combatViewManager.OnUnitAttacked(evt.EntityId, evt.TargetEntityId, evt.Value0, evt.Flag0, isProjectile, isPreTimed);
                    break;
                }

                case SimEventType.UnitDamaged:
                    _combatViewManager.OnUnitDamaged(evt.EntityId, evt.Value0, (DamageType)evt.Value1, evt.Flag0);
                    break;

                // UnitDied: 유휴 전투에서는 사망 무시
                // case SimEventType.UnitDied:

                case SimEventType.UnitCastSkill:
                {
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    _combatViewManager.OnUnitCastSkill(evt.EntityId, evt.TargetEntityId, evt.Value0, element, evt.Flag0, evt.Flag1);
                    break;
                }

                case SimEventType.ProjectileSpawned:
                {
                    int champSpecId = ResolveChampSpecId(matchState, evt.EntityId);
                    int projectileId = evt.Value0;
                    int skillSpecId = evt.Value1;
                    _combatViewManager.OnProjectileSpawned(
                        evt.EntityId, evt.TargetEntityId, evt.ProjType,
                        evt.Col, evt.Row, (sbyte)evt.DirCol, (sbyte)evt.DirRow, champSpecId, projectileId, skillSpecId,
                        evt.SkillVfxIndex, evt.MoveInterval, evt.Flag0, evt.ArrivalVfxIndex);
                    break;
                }

                case SimEventType.ProjectileMoved:
                {
                    int projectileId = evt.Value0;
                    _combatViewManager.OnProjectileMoved(projectileId, evt.Col, evt.Row);

                    // 이동한 타일에 원소 타일 이펙트 표시 (width에 따라 다중 타일)
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    if (element != SynergyType.NONE && _combatViewManager != null)
                    {
                        var castType = TileEffectManager.SynergyToAreaType(element);
                        int width = evt.Radius > 1 ? evt.Radius : 1;
                        int halfW = width / 2;
                        sbyte dirCol = (sbyte)evt.DirCol;
                        sbyte dirRow = (sbyte)evt.DirRow;

                        for (int offset = -halfW; offset <= halfW; offset++)
                        {
                            int tileCol = evt.Col;
                            int tileRow = evt.Row;

                            if (offset != 0)
                            {
                                if (dirCol == 0)
                                    tileCol += offset;
                                else if (dirRow == 0)
                                    tileRow += offset;
                                else
                                {
                                    int diagCol = evt.Col + offset;
                                    if (BoardHelper.IsValidCombatPosition(diagCol, evt.Row))
                                    {
                                        var posA = BoardWorldHelper.CombatGridToWorld(BoardIndex, diagCol, evt.Row);
                                        _combatViewManager.ShowTileEffectAt(castType, posA);
                                    }
                                    tileCol = evt.Col;
                                    tileRow = evt.Row + offset;
                                }
                            }

                            if (!BoardHelper.IsValidCombatPosition(tileCol, tileRow)) continue;
                            var worldPos = BoardWorldHelper.CombatGridToWorld(BoardIndex, tileCol, tileRow);
                            _combatViewManager.ShowTileEffectAt(castType, worldPos);
                        }
                    }
                    break;
                }

                case SimEventType.ProjectileExpired:
                {
                    int projectileId = evt.Value0;
                    _combatViewManager.OnProjectileExpired(projectileId);
                    break;
                }

                case SimEventType.ProjectileExploded:
                {
                    var element = ResolveElementFromSkill(matchState, evt.Value0);
                    _combatViewManager.OnProjectileExploded(evt.Col, evt.Row, evt.Radius, element);
                    break;
                }

                case SimEventType.SkillPhaseVfx:
                {
                    int casterId = evt.EntityId;
                    int targetId = evt.TargetEntityId;
                    int skillSpecId = evt.Value0;
                    byte vfxIndex = (byte)evt.Value1;
                    sbyte dirCol = (sbyte)evt.DirCol;
                    sbyte dirRow = (sbyte)evt.DirRow;

                    // 미사 봉인: 타겟 캐릭터 숨김 (VFX는 useGridPos 경로로 공용 처리)
                    if (skillSpecId == 217323201 && targetId > 0)
                    {
                        var targetView = _unitViewManager?.FindCombatView(targetId);
                        targetView?.SetModelVisible(false);
                    }

                    _combatViewManager.OnSkillPhaseVfx(casterId, skillSpecId, vfxIndex, dirCol, dirRow, targetId);
                    break;
                }

                case SimEventType.SkillRectAreaEffect:
                {
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    sbyte dirCol = (sbyte)evt.DirCol;
                    sbyte dirRow = (sbyte)evt.DirRow;
                    _combatViewManager.OnSkillRectAreaEffect(evt.Col, evt.Row, dirCol, dirRow, element);
                    break;
                }

                case SimEventType.SkillAreaEffect:
                {
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    bool isBox = evt.Value1 != 0;
                    _combatViewManager.OnSkillAreaEffect(evt.Col, evt.Row, evt.Radius, element, evt.Flag0, isBox);
                    break;
                }

                // UI 이벤트: 유휴 전투에서는 무시
                // case SimEventType.GoldChanged:
                // case SimEventType.LevelUp:
                // case SimEventType.PlayerEliminated:
                // case SimEventType.CombatResult:
                // case SimEventType.SynergyUpdated:

                case SimEventType.UnitMissed:
                    _combatViewManager.OnUnitMissed(evt.EntityId, evt.TargetEntityId);
                    break;

                case SimEventType.UnitHealed:
                    _combatViewManager.OnUnitHealed(evt.EntityId, evt.Value0);
                    break;

                case SimEventType.StatusEffectAdded:
                {
                    var vfxType = SimEventHelper.DecodeVfxType(evt.Value0);
                    var statType = SimEventHelper.DecodeStatType(evt.Value0);
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, vfxType, statType);
                    break;
                }

                case SimEventType.StatusEffectRemoved:
                {
                    var vfxType = SimEventHelper.DecodeVfxType(evt.Value0);
                    var statType = SimEventHelper.DecodeStatType(evt.Value0);
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, vfxType, statType);
                    break;
                }

                case SimEventType.CCAdded:
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0, default);
                    break;

                case SimEventType.CCRemoved:
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0, default);
                    // 미사 봉인 해제: 숨겨진 캐릭터 복원
                    {
                        var unitView = _unitViewManager?.FindCombatView(evt.EntityId);
                        if (unitView != null && unitView.IsModelHidden)
                            unitView.SetModelVisible(true);
                    }
                    break;
            }
        }

        // ── 원소 타입 조회 ──

        /// <summary>시전자 entityId → 캐릭터 원소 타입 (CombatId 또는 SourceEntityId 매칭)</summary>
        private static SynergyType ResolveElementFromCaster(CombatMatchState matchState, int casterId)
        {
            if (matchState == null) return SynergyType.NONE;

            for (int u = 0; u < matchState.UnitCount; u++)
            {
                if (matchState.Units[u].CombatId == casterId ||
                    matchState.Units[u].SourceEntityId == casterId)
                {
                    int champId = matchState.Units[u].ChampionSpecId;
                    return GetElementFromCharacterId(champId);
                }
            }
            return SynergyType.NONE;
        }

        /// <summary>skillSpecId → 원소 타입 (유휴 전투에서는 간소화 — CombatMatchState에서 역추적)</summary>
        private static SynergyType ResolveElementFromSkill(CombatMatchState matchState, int skillSpecId)
        {
            if (skillSpecId <= 0 || matchState == null) return SynergyType.NONE;

            // matchState의 유닛 스킬ID로 역추적
            for (int u = 0; u < matchState.UnitCount; u++)
            {
                if (matchState.Units[u].SkillSpecId == skillSpecId)
                {
                    int champId = matchState.Units[u].ChampionSpecId;
                    return GetElementFromCharacterId(champId);
                }
            }
            return SynergyType.NONE;
        }

        /// <summary>combatId → ChampionSpecId</summary>
        private static int ResolveChampSpecId(CombatMatchState matchState, int combatId)
        {
            if (matchState == null) return 0;

            for (int u = 0; u < matchState.UnitCount; u++)
            {
                if (matchState.Units[u].CombatId == combatId)
                    return matchState.Units[u].ChampionSpecId;
            }
            return 0;
        }

        private static SynergyType GetElementFromCharacterId(int champId)
        {
            var charInfo = SpecDataManager.Instance.GetCharacterData(champId);
            return charInfo?.character_element_type ?? SynergyType.NONE;
        }
    }
}
