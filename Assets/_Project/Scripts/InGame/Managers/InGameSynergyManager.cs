using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;

namespace CookApps.BattleSystem
{
    public class InGameSynergyManager : SingletonMonoBehaviour<InGameSynergyManager>
    {
        private Dictionary<AllianceType, HashSet<SynergyType>> _synergyManagerDataDic = new Dictionary<AllianceType, HashSet<SynergyType>>();
        private InGameBattleItemDragDropComponent _itemComponent = new InGameBattleItemDragDropComponent();

        // 플레이어와 적의 시너지 이펙트를 분리하여 관리
        private Dictionary<SynergyType, List<InGameVfx>> _playerSynergyVfxDic = new();
        private Dictionary<SynergyType, List<InGameVfx>> _enemySynergyVfxDic = new();


        public void Initialize()
        {
            if (_synergyManagerDataDic == null)
            {
                _synergyManagerDataDic = new Dictionary<AllianceType, HashSet<SynergyType>>();
            }
            _synergyManagerDataDic.Clear();
            _itemComponent.Initialize();
            _synergyManagerDataDic.Add(AllianceType.Player, new HashSet<SynergyType>());
            _synergyManagerDataDic.Add(AllianceType.Enemy, new HashSet<SynergyType>());
        }

        public void Clear()
        {
            ClearSynergyFx();
            _synergyManagerDataDic.Clear();
            _itemComponent.Clear();
        }
        public void ClearSynergyFx()
        {
            ClearSynergyFxByAlliance(AllianceType.Player);
            ClearSynergyFxByAlliance(AllianceType.Enemy);
        }

        /// <summary>
        /// 특정 진영의 모든 시너지 이펙트를 제거합니다.
        /// </summary>
        /// <param name="allianceType">제거할 진영 타입</param>
        private void ClearSynergyFxByAlliance(AllianceType allianceType)
        {
            var synergyVfxDic = GetSynergyVfxDic(allianceType);

            foreach (var kvp in synergyVfxDic)
            {
                foreach (var vfx in kvp.Value)
                {
                    if (vfx != null)
                    {
                        vfx.transform.SetParent(InGameObjectManager.Instance.Playground);
                        vfx.Remove();
                    }
                }
            }
            synergyVfxDic.Clear();
        }

        /// <summary>
        /// 특정 진영의 특정 시너지 타입의 이펙트만 제거합니다.
        /// </summary>
        /// <param name="allianceType">제거할 진영 타입</param>
        /// <param name="synergyType">제거할 시너지 타입</param>
        public void ClearTargetSynergyFx(AllianceType allianceType, SynergyType synergyType)
        {
            var synergyVfxDic = GetSynergyVfxDic(allianceType);

            if (synergyVfxDic.TryGetValue(synergyType, out var vfxList))
            {
                foreach (var vfx in vfxList)
                {
                    if (vfx != null)
                    {
                        vfx.transform.SetParent(InGameObjectManager.Instance.Playground);
                        vfx.Remove();
                    }
                }
                vfxList.Clear();
                synergyVfxDic.Remove(synergyType);
            }
        }

        public void OnAddCharacter(CharacterController character)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat
            || InGameMainFlowManager.Instance.CurrentFlowState is CookApps.AutoBattler.Prologue.FlowStatePrologueReady
            || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat
            || character.SpecCharacter.character_type == CharacterType.BATTLEITEM)
            {
                return;
            }

            if (character.SpecCharacter.character_type != CharacterType.CHARACTER)
                return;

            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_element_type);
            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);

            ClearTargetSynergyFx(character.AllianceType, character.SpecCharacter.character_element_type);
            ClearTargetSynergyFx(character.AllianceType, character.SpecCharacter.character_stella_type);

            ApplyTargetSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);
            ApplyTargetSynergy(character.AllianceType, character.SpecCharacter.character_element_type);

        }

        public void OnRemoveCharacter(CharacterController character)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat
            || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat
            || character.SpecCharacter.character_type == CharacterType.BATTLEITEM)
            {
                return;
            }

            if (character.SpecCharacter.character_type != CharacterType.CHARACTER)
                return;


            _itemComponent.CheckAffectedByItemController(character);

            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_element_type);
            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);

            ClearTargetSynergyFx(character.AllianceType, character.SpecCharacter.character_element_type);
            ClearTargetSynergyFx(character.AllianceType, character.SpecCharacter.character_stella_type);


            ApplyTargetSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);
            ApplyTargetSynergy(character.AllianceType, character.SpecCharacter.character_element_type);
        }

        public void ApplyTargetSynergy(AllianceType callerAllianceType, SynergyType targetSynergyType)
        {

            bool canAddSynergy = CanAddSynergy(callerAllianceType, targetSynergyType, out var outSynergyData, out var outSynergyList);
            if (canAddSynergy)
            {
                _synergyManagerDataDic[callerAllianceType].Add(targetSynergyType);

                //모든 시너지관련 이펙트코드는 1단계에서 최대까지 호출한다.
                for (int j = 1; j <= outSynergyData.grade; j++)
                {
                    var synergyData = outSynergyList[j - 1];
                    switch (synergyData.synergy_cover_type)
                    {
                        case SynergyCoverType.SQUAD_STELLA://본인의 엘리먼트나 포지션에 비교하여 맞는다면 수행
                            AddSynergyIfMySynergy(callerAllianceType, outSynergyList[0].synergy_group_id, synergyData, targetSynergyType);
                            break;
                        case SynergyCoverType.SQUAD_ALL://모든 캐릭터에 주입
                            AddSynergyAllMember(callerAllianceType, outSynergyList[0].synergy_group_id, synergyData);
                            break;
                        case SynergyCoverType.SQUAD_ONCE:
                            AddSynergyTeamOnce(callerAllianceType, outSynergyList[0].synergy_group_id, synergyData, j);
                            break;
                    }
                }


            }

        }
        public void TidyUpPreviewSynergy(AllianceType callerAllianceType, SynergyType synergyType)
        {
            if (_synergyManagerDataDic[callerAllianceType].Contains(synergyType))
            {
                var synergyList = SpecDataManager.Instance.GetSpecSynergyList(synergyType);
                foreach (var character in InGameObjectManager.Instance.GetCharacterList(callerAllianceType))
                {
                    character.RemoveSynergyEffectCode(synergyType);
                }
            }

            if (!CanAddSynergy(callerAllianceType, synergyType, out var outSynergyDataSynergyType, out var outSynergyListSynergyType))
            {
                InGameManager.Instance.RemoveSynergyTeamOnce(callerAllianceType, synergyType);
            }

            _synergyManagerDataDic[callerAllianceType].Remove(synergyType);
        }

        private void AddSynergyAllMember(AllianceType allianceType, long effectCodeId, ISpecSynergyData synergyData)
        {
            var targetList = InGameObjectManager.Instance.GetCharacterList(allianceType);

            foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
            {//이건 무조건 주입하는 함수
                character.InjectSynergy(effectCodeId, synergyData);
            }

            SpawnSynergyFx(allianceType, targetList, synergyData.synergy_type, synergyData.grade);

        }
        private void AddSynergyIfMySynergy(AllianceType allianceType, long effectCodeId, ISpecSynergyData synergyData, SynergyType targetSynergyType)
        {
            var targetList = InGameObjectManager.Instance.GetCharacterList(allianceType);
            foreach (var character in targetList)
            {
                //이건 본인의 시너지와 맞으면 적용하는 함수
                character.AddSynergyApplyEach(targetSynergyType, effectCodeId, synergyData);
            }
            SpawnSynergyFx(allianceType, targetList, targetSynergyType, synergyData.grade);
        }

        public void AddSynergyTeamOnce(AllianceType AllianceType, long effectCodeId, ISpecSynergyData synergyData, int grade)
        {
            InGameManager.Instance.AddSynergyTeamOnce(AllianceType, effectCodeId, synergyData, grade);
        }

        private bool CanAddSynergy(AllianceType allianceType, SynergyType targetSynergyType
        , out ISpecSynergyData outSynergyData, out List<ISpecSynergyData> outSynergyList)
        {
            outSynergyData = null;
            outSynergyList = null;
            var inGameObjectManagerInstance = InGameObjectManager.Instance;


            var targetSynergyCharacterCount =
                inGameObjectManagerInstance.GetCharacterSynergyCount(allianceType, targetSynergyType);

            if (targetSynergyCharacterCount < 1)
                return false;

            return SpecDataManager.Instance.TryGetSynergyDataByCount(targetSynergyType, targetSynergyCharacterCount,
            out outSynergyData, out outSynergyList);
        }

        /// <summary>
        /// 특정 진영의 캐릭터들에게 시너지 이펙트를 생성합니다.
        /// </summary>
        /// <param name="allianceType">진영 타입</param>
        /// <param name="targetList">대상 캐릭터 리스트</param>
        /// <param name="synergyType">시너지 타입</param>
        /// <param name="grade">시너지 등급</param>
        public void SpawnSynergyFx(AllianceType allianceType, List<CharacterController> targetList, SynergyType synergyType, int grade)
        {
            var synergyVfxDic = GetSynergyVfxDic(allianceType);

            foreach (var character in targetList)
            {
                InGameVfxNameType inGameVfxNameType = InGameVfxNameType.NONE;

                switch (synergyType)
                {
                    case SynergyType.FIRE:
                        if (grade == 1)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_fire;
                        }
                        else if (grade == 2)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_fire_02;
                        }
                        else if (grade == 3)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_fire_03;
                        }
                        break;
                    case SynergyType.WATER:
                        if (grade == 1)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_water;
                        }
                        else if (grade == 2)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_water_02;
                        }
                        else if (grade == 3)
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_water_03;
                        break;
                    case SynergyType.LIGHTNING:
                        if (grade == 1)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_lightning_01;
                        }
                        else if (grade == 2)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_lightning_02;
                        }
                        else if (grade == 3)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_lightning_03;
                        }
                        break;
                    case SynergyType.EARTH:
                        if (grade == 1)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_ground;
                        }
                        else if (grade == 2)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_ground_02;
                        }
                        else if (grade == 3)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_ground_03;
                        }
                        break;
                    case SynergyType.WIND:
                        if (grade == 1)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_wind;
                        }
                        else if (grade == 2)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_wind_02;
                        }
                        else if (grade == 3)
                        {
                            inGameVfxNameType = InGameVfxNameType.fx_common_synergy_wind_03;
                        }
                        break;
                    case SynergyType.NOBLESSE:
                    case SynergyType.TROUBLESHOOTER:
                    case SynergyType.SUPERNOVA:
                    // inGameVfxNameType = InGameVfxNameType.fx_common_synergy_darkness;
                    // break;
                    default:
                        break;
                }

                if (inGameVfxNameType == InGameVfxNameType.NONE)
                {
                    continue;
                }

                if (character.SpecCharacter.character_element_type == synergyType)
                {
                    var vfx = InGameVfxManager.Instance.AddInGameVfxByTransform(inGameVfxNameType,
                        character.GetCharacterView().CachedTr);

                    // 시너지 타입별로 리스트에 추가
                    if (!synergyVfxDic.TryGetValue(synergyType, out var vfxList))
                    {
                        vfxList = new List<InGameVfx>();
                        synergyVfxDic[synergyType] = vfxList;
                    }
                    vfxList.Add(vfx);
                }
                else if (character.SpecCharacter.character_stella_type == synergyType)
                {
                    var vfx = InGameVfxManager.Instance.AddInGameVfxByTransform(inGameVfxNameType,
                        character.GetCharacterView().CachedTr);

                    // 시너지 타입별로 리스트에 추가
                    if (!synergyVfxDic.TryGetValue(synergyType, out var vfxList))
                    {
                        vfxList = new List<InGameVfx>();
                        synergyVfxDic[synergyType] = vfxList;
                    }
                    vfxList.Add(vfx);
                }

            }
        }

        /// <summary>
        /// 진영 타입에 따라 해당하는 시너지 이펙트 Dictionary를 반환합니다.
        /// </summary>
        /// <param name="allianceType">진영 타입</param>
        /// <returns>시너지 이펙트 Dictionary</returns>
        private Dictionary<SynergyType, List<InGameVfx>> GetSynergyVfxDic(AllianceType allianceType)
        {
            return allianceType == AllianceType.Player ? _playerSynergyVfxDic : _enemySynergyVfxDic;
        }

        #region item

        public void RegisterBattleItem(InGameBattleItemDragDropComponent.InGameBattleItemInfo itemInfo)
        {
            _itemComponent.RegisterBattleItem(itemInfo);
        }
        public bool IsDragAndDropBattleItem(CharacterController character)
        {
            return _itemComponent.IsDragAndDropBattleItem(character);
        }

        public List<CharacterController> GetBattleItemList(int prefab_id)
        {
            return _itemComponent.GetBattleItemList(prefab_id);
        }
        public bool ApplyBattleItem(CharacterController itemObj, CharacterController targetObj)
        {
            return _itemComponent.ApplyBattleItem(itemObj, targetObj);
        }
        public List<InGameBattleItemDragDropComponent.InGameBattleItemInfo> GetBattleItemInfoList(int prefab_id)
        {
            return _itemComponent.GetBattleItemInfoList(prefab_id);
        }
        public bool IsRegisteredBattleItem(int prefab_id)
        {
            return _itemComponent.IsRegisteredBattleItem(prefab_id);
        }
        /// <summary>
        /// 위 함수 호출 시 해당 인스턴스(게임오브젝트도 removefromfield로 호출됩니ㅏ.)
        /// </summary>
        /// <param name="prefab_id"></param>
        public void TryRemoveBattleItemFromTarget(int prefab_id)
        {
            _itemComponent.TryRemoveBattleItemFromTarget(prefab_id);
        }
        /// <summary>
        /// 전투 시작 시 아이템이 적용되지 않은 상태인 아이템들의 콜백을 호출합니다.
        /// </summary>
        public void CheckAndHandleNotAppliedItemsBeforeCombat()
        {
            _itemComponent.CheckAndHandleNotAppliedItemsBeforeCombat();
        }

        /// <summary>
        /// 배틀 아이템 상태를 변경합니다.
        /// </summary>
        /// <param name="itemState">배틀 아이템 상태</param>
        /// <param name="itemObj">배틀 아이템 오브젝트</param>
        /// <param name="targetObj">대상 캐릭터 오브젝트</param>
        public void ModifyBattleItemState(InGameBattleItemDragDropComponent.ItemState itemState, CharacterController itemObj, CharacterController targetObj)
        {
            _itemComponent.ModifyBattleItemState(itemState, itemObj, targetObj);
        }
    }
    #endregion
}
