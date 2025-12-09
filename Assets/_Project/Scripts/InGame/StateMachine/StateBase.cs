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

    public abstract class StateCombatBase : StateBase
    {
        protected void AddSynergy(AllianceType callerAllianceType)
        {
            SynergyType synergyType = SynergyType.NONE;
            for (int i = (int)synergyType + 1; i < Enum.GetValues(typeof(SynergyType)).Length; i++)
            {
                // synergyType = (SynergyType)i;
                // if (!CanAddSynergy(callerAllianceType, synergyType, out var outMaxGradeSynergyData, out var outTargetSynergyDataList))
                //     continue;
                //
                // //모든 시너지관련 이펙트코드는 1단계에서 최대까지 호출한다.
                // for (int j = 1; j <= outMaxGradeSynergyData.grade; j++)
                // {
                //     var synergyData = outTargetSynergyDataList[j];
                //     switch (synergyData.synergy_affect_type)
                //     {
                //         case SynergyAffectType.APPLY_IF_MYSYNERGY://본인의 엘리먼트나 포지션에 비교하여 맞는다면 수행
                //             AddSynergyIfMySynergy(callerAllianceType, outTargetSynergyDataList[0].id, synergyData, synergyType);
                //             break;
                //         case SynergyAffectType.APPLY_ALL_MEMBER://모든 캐릭터에 주입
                //             AddSynergyAllMember(callerAllianceType, outTargetSynergyDataList[0].id, synergyData);
                //             break;
                //         case SynergyAffectType.APPLY_OTHER_TEAM_ONCE:
                //             AddSynergyTeamOnce(callerAllianceType, outTargetSynergyDataList[0].id, synergyData);
                //             break;
                //     }
                // }
            }
        }

        protected void AddPassive(AllianceType allianceType)
        {
            var specDataManagerInstance = SpecDataManager.Instance;
            int testGrade = 0;
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
            {
                // var passiveList = specDataManagerInstance.GetPassivePositionList(character.SpecCharacter.position_type);
                // if (passiveList == null || passiveList.Count == 0)
                //     continue;
                //
                // foreach (var passive in passiveList)
                // {
                //     character.InjectPassive((long)passive[testGrade].passieve_id, passive[testGrade]);
                // }
            }
        }

        // private void AddSynergyAllMember(AllianceType allianceType, long effectCodeId, SpecSynergy synergyData)
        // {
        //
        //     foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
        //     {//이건 무조건 주입하는 함수
        //         character.InjectSynergy(effectCodeId, synergyData);
        //     }
        // }
        //
        // private void AddSynergyIfMySynergy(AllianceType allianceType, long effectCodeId, SpecSynergy synergyData, SynergyType targetSynergyType)
        // {
        //     foreach (var character in InGameObjectManager.Instance.GetCharacterList(allianceType))
        //     {
        //         //이건 본인의 시너지와 맞으면 적용하는 함수
        //         character.AddSynergyApplyEach(targetSynergyType, effectCodeId, synergyData);
        //     }
        // }
        //
        // public void AddSynergyTeamOnce(AllianceType AllianceType, long effectCodeId, SpecSynergy synergyData)
        // {
        //     InGameManager.Instance.AddSynergyTeamOnce(AllianceType, effectCodeId, synergyData);
        // }


        // private bool CanAddSynergy(AllianceType allianceType, SynergyType targetSynergyType
        // , out SpecSynergy outSynergyData, out List<SpecSynergy> outSynergyList)
        // {
        //     outSynergyData = null;
        //     outSynergyList = null;
        //     var inGameObjectManagerInstance = InGameObjectManager.Instance;
        //
        //
        //     var targetSynergyCharacterCount =
        //         inGameObjectManagerInstance.GetCharacterSynergyCount(allianceType, targetSynergyType);
        //
        //     if (targetSynergyCharacterCount < 1)
        //         return false;
        //
        //     return SpecDataManager.Instance.TryGetSynergyDataByCount(targetSynergyType, targetSynergyCharacterCount,
        //     out outSynergyData, out outSynergyList);
        // }
    }

    public abstract class StateReadyBase : StateBase
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
