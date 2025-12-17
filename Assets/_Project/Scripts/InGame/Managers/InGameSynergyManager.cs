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
    public class InGameSynergyManager : SingletonMonoBehaviour<InGameSynergyManager>
    {
        private Dictionary<AllianceType, HashSet<SynergyType>> _synergyManagerDataDic = new Dictionary<AllianceType, HashSet<SynergyType>>();
        private InGameBattleItemComponent _itemComponent = new InGameBattleItemComponent();


        public void Initialize()
        {
            if(_synergyManagerDataDic == null)
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
            _synergyManagerDataDic.Clear();
            _itemComponent.Clear();
        }

        public void OnAddCharacter(CharacterController character)
        {
            if(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat)
            {
                return;
            }
            
            if (character.SpecCharacter.character_type == CharacterType.BATTLEITEM)
            {
                return;
            }
            
            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_element_type);
            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);

            ApplyTargetSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);
            ApplyTargetSynergy(character.AllianceType, character.SpecCharacter.character_element_type);
        }

        public void OnRemoveCharacter(CharacterController character)
        {
            if(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat
            || character.SpecCharacter.character_type == CharacterType.BATTLEITEM)
            {
                return;
            }
            _itemComponent.CheckAffectedByItemController(character);

            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_element_type);
            TidyUpPreviewSynergy(character.AllianceType, character.SpecCharacter.character_stella_type);

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
                            AddSynergyTeamOnce(callerAllianceType, outSynergyList[0].synergy_group_id, synergyData);
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
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
            {//이건 무조건 주입하는 함수
                character.InjectSynergy(effectCodeId, synergyData);
            }
        }
        private void AddSynergyIfMySynergy(AllianceType allianceType, long effectCodeId, ISpecSynergyData synergyData, SynergyType targetSynergyType)
        {
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
            {
                //이건 본인의 시너지와 맞으면 적용하는 함수
                character.AddSynergyApplyEach(targetSynergyType, effectCodeId, synergyData);
            }
        }

        public void AddSynergyTeamOnce(AllianceType AllianceType, long effectCodeId, ISpecSynergyData synergyData)
        {
            InGameManager.Instance.AddSynergyTeamOnce(AllianceType, effectCodeId, synergyData);
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



        public void RegisterItem(InGameBattleItemComponent.InGameBattleItemInfo itemInfo)
        {
            _itemComponent.RegisterItem(itemInfo);
        }
        public bool IsDragAndDropItem(CharacterController character)
        {
            return _itemComponent.IsDragAndDropItem(character);
        }
        public bool ApplyItem(CharacterController itemObj, CharacterController targetObj)
        {
            return _itemComponent.ApplyItem(itemObj, targetObj);
        }
        public bool IsRegisteredItem(int prefab_id)
        {
            return _itemComponent.IsRegisteredItem(prefab_id);
        }
        public void TryRemoveItemFromTarget(int prefab_id)
        {
            _itemComponent.TryRemoveItemFromTarget(prefab_id);
        }
        public void CheckAffectedByItemController(CharacterController character)
        {
            _itemComponent.CheckAffectedByItemController(character);
        }
    }
}
