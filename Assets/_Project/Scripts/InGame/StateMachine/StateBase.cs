using System;
using Cysharp.Threading.Tasks;
using CookApps.AutoBattler;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Collections.Generic;

namespace CookApps.BattleSystem
{
    public abstract class StateBase
    {
        public virtual void SetStateData(object data) { }
        public abstract void StateInit(object owner);
        public abstract void StateStart();
        public abstract void StateRunning(float dt);
        public abstract void StateEnd(bool isForced);
    }

    public abstract class StateCombatStepBase : StateBase, IEffectCodeSource
    {
        // public void AddSynergy(AllianceType callerAllianceType)
        // {
        //     //성단 시너지 넣고
        //     SynergyType synergyType = SynergyType.NONE;
        //     for (int i = (int)synergyType + 1; i < Enum.GetValues(typeof(SynergyType)).Length; i++)
        //     {
        //         synergyType = (SynergyType)i;
        //         if (!CanAddSynergy(callerAllianceType, synergyType, out var outMaxGradeSynergyData, out var outTargetSynergyDataList))
        //             continue;

        //         //모든 시너지관련 이펙트코드는 1단계에서 최대까지 호출한다.
        //         for (int j = 1; j <= outMaxGradeSynergyData.grade; j++)
        //         {
        //             var synergyData = outTargetSynergyDataList[j];
        //             switch (synergyData.synergy_cover_type)
        //             {
        //                 case SynergyCoverType.SQUAD_STELLA://본인의 엘리먼트나 포지션에 비교하여 맞는다면 수행
        //                     AddSynergyIfMySynergy(callerAllianceType, outTargetSynergyDataList[0].id, synergyData, synergyType);
        //                     break;
        //                 case SynergyCoverType.SQUAD_ALL://모든 캐릭터에 주입
        //                     AddSynergyAllMember(callerAllianceType, outTargetSynergyDataList[0].id, synergyData);
        //                     break;
        //                 case SynergyCoverType.SQUAD_ONCE:
        //                     AddSynergyTeamOnce(callerAllianceType, outTargetSynergyDataList[0].id, synergyData);
        //                     break;
        //             }
        //         }
        //     }

        //     var elementType = ElementType.NONE;
        //     for (int i = (int)elementType + 1; i < Enum.GetValues(typeof(ElementType)).Length; i++)
        //     {
        //         elementType = (ElementType)i;
        //         if (!CanAddSynergy(callerAllianceType, elementType, out var outSynergyData, out var outSynergyList))
        //             continue;
                    
        //         //모든 시너지관련 이펙트코드는 1단계에서 최대까지 호출한다.
        //         for (int j = 1; j <= outSynergyData.grade; j++)
        //         {
        //             var synergyData = outSynergyList[j];
        //             switch (synergyData.synergy_cover_type)
        //             {
        //                 case SynergyCoverType.SQUAD_STELLA:
        //                     AddSynergyIfMySynergy(callerAllianceType, outSynergyList[0].id, synergyData, elementType);
        //                     break;
        //                 case SynergyCoverType.SQUAD_ALL:
        //                     AddSynergyAllMember(callerAllianceType, outSynergyList[0].id, synergyData);
        //                     break;
        //                 case SynergyCoverType.SQUAD_ONCE:
        //                     AddSynergyTeamOnce(callerAllianceType, outSynergyList[0].id, synergyData);
        //                     break;
        //             }
        //         }
        //     }
        // }
        public void ApplyTargetSynergy(AllianceType callerAllianceType, ElementType elementType, SynergyType asterismType)
        {
            // SynergyType synergyType = SynergyType.NONE;
            // for (int i = (int)synergyType + 1; i < Enum.GetValues(typeof(SynergyType)).Length; i++)
            // {
            //     synergyType = (SynergyType)i;
            //     if (elementType != SynergyType.NONE && asterismType != SynergyType.NONE)
            //     {
            //         if (synergyType != elementType && synergyType != asterismType)
            //         {
            //             continue;
            //         }
            //     }
            //
            //     if (!CanAddSynergy(callerAllianceType, synergyType, out var outMaxGradeSynergyData, out var outTargetSynergyDataList))
            //         continue;
            //
            //     //모든 시너지관련 이펙트코드는 1단계에서 최대까지 호출한다.
            //     for (int j = 1; j <= outMaxGradeSynergyData.grade; j++)
            //     {
            //
            //         var synergyData = outTargetSynergyDataList[j];
            //         switch (synergyData.synergy_affect_type)
            //         {
            //             case SynergyAffectType.APPLY_IF_MYSYNERGY://본인의 엘리먼트나 포지션에 비교하여 맞는다면 수행
            //                 AddSynergyIfMySynergy(callerAllianceType, outTargetSynergyDataList[0].id, synergyData, synergyType);
            //                 break;
            //             case SynergyAffectType.APPLY_ALL_MEMBER://모든 캐릭터에 주입
            //                 AddSynergyAllMember(callerAllianceType, outTargetSynergyDataList[0].id, synergyData);
            //                 break;
            //             case SynergyAffectType.APPLY_OTHER_TEAM_ONCE:
            //             case SynergyAffectType.APPLY_TEAM_ONCE:
            //                 AddSynergyTeamOnce(callerAllianceType, outTargetSynergyDataList[0].id, synergyData);
            //                 break;
            //         }
            //     }
            // }
        }
        public void TidyUpPreviewSynergy(AllianceType callerAllianceType)
        {
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(callerAllianceType))
            {
                character.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(character);
            }
        }
        public void TidyUpPreviewSynergy(AllianceType callerAllianceType, ElementType elementType, SynergyType asterismType)
        {
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(callerAllianceType))
            {
                if (character.SpecCharacter.character_element_type == elementType || character.SpecCharacter.character_stella_type == asterismType)
                {
                    character.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(character);
                }
            }

            // if (!CanAddSynergy(callerAllianceType, elementType, out var outSynergyDataElementType, out var outSynergyListElementType))
            // {
            //     InGameManager.Instance.RemoveSynergyTeamOnce(callerAllianceType, elementType);
            // }
            // if (!CanAddSynergy(callerAllianceType, asterismType, out var outSynergyDataAsterism, out var outSynergyListAsterism))
            // {
            //     InGameManager.Instance.RemoveSynergyTeamOnce(callerAllianceType, asterismType);
            // }

        }

        protected void AddPassive(AllianceType allianceType)
        {
            return;
            var specDataManagerInstance = SpecDataManager.Instance;
            int testGrade = 0;
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
            {
                var passiveList = specDataManagerInstance.GetPassivePositionList(character.SpecCharacter.character_position_type);
                if (passiveList == null || passiveList.Count == 0)
                    continue;

                foreach (var passive in passiveList)
                {
                    character.InjectPassive((long)passive[testGrade].passive_skill_type, passive[testGrade]);
                }
            }
        }

        // private void AddSynergyAllMember(AllianceType allianceType, long effectCodeId, SpecSynergy synergyData)
        // {
        //     foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
        //     {//이건 무조건 주입하는 함수
        //         character.InjectSynergy(effectCodeId, synergyData);
        //     }
        // }
        // private void AddSynergyIfMySynergy(AllianceType allianceType, long effectCodeId, SpecSynergy synergyData, SynergyType targetSynergyType)
        // {
        //     foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
        //     {
        //         //이건 본인의 시너지와 맞으면 적용하는 함수
        //         character.AddSynergyApplyEach(targetSynergyType, effectCodeId, synergyData);
        //     }
        // }

        // public void AddSynergyTeamOnce(AllianceType AllianceType, long effectCodeId, SpecSynergy synergyData)
        // {
        //     InGameManager.Instance.AddSynergyTeamOnce(AllianceType, effectCodeId, synergyData, this);
        // }

        private bool CanAddSynergy(AllianceType allianceType, SynergyType targetSynergyType
        , out SynergyStarAsterism outSynergyData, out List<SynergyStarAsterism> outSynergyList)
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
        
        private bool CanAddSynergy(AllianceType allianceType, ElementType targetElementType
        , out SynergyElemental outSynergyData, out List<SynergyElemental> outSynergyList)
        {
            outSynergyData = null;
            outSynergyList = null;
            var inGameObjectManagerInstance = InGameObjectManager.Instance;
            
            var targetElementTypeCharacterCount =
                inGameObjectManagerInstance.GetCharacterSynergyCount(allianceType, targetElementType);

            if (targetElementTypeCharacterCount < 1)
                return false;

            return SpecDataManager.Instance.TryGetSynergyDataByCount(targetElementType, targetElementTypeCharacterCount,
            out outSynergyData, out outSynergyList);
        }
    }

    public abstract class StateCombatBase : StateCombatStepBase
    {

    }

    public abstract class StateReadyBase : StateCombatStepBase
    {
        protected async UniTaskVoid StartDrawingLinesAsync(float intervalITime)
        {
            while (InGameMainFlowManager.Instance.CurrentFlowState is StateReadyBase)
            {
                InGameObjectManager.Instance.DrawPlayerLine(true);

                await UniTask.Delay(TimeSpan.FromSeconds(intervalITime));

                if (InGameMainFlowManager.Instance.CurrentFlowState is not StateReadyBase)
                    break;

                InGameObjectManager.Instance.DrawPlayerLine(false);

                await UniTask.Delay(TimeSpan.FromSeconds(intervalITime));
            }
        }
    }
}
