using System;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 건물 강제 터치 액션.
    /// 특정 건물(TouchableBuilding)에 홀 마스크를 표시하여 강조하고,
    /// 건설 완료 상태에서 터치하면 다음 튜토리얼로 진행합니다.
    ///
    /// tutorial_action_key: "Building_{FacilityType}" 형식 (예: "Building_FacilityTypeCommandCenter")
    /// </summary>
    public class TutorialActionForcedTouchBuilding : ITutorialActionStrategy
    {
        /// <summary>
        /// 건물 클릭 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static Action OnBuildingClicked;

        /// <summary>
        /// 현재 타겟 FacilityType
        /// </summary>
        private static ElpisFacilityType _targetFacilityType;

        /// <summary>
        /// 현재 타겟 건물 오브젝트
        /// </summary>
        private static GameObject _targetBuildingObj;

        public void OnShow(TutorialActionContext context)
        {
            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // tutorial_action_key에서 건물 찾기 (예: "Building_FacilityTypeCommandCenter")
            string actionKey = context.CurrentTutorial.tutorial_action_key;
            _targetBuildingObj = TutorialTargetRegistry.FindGameObject(actionKey);

            if (_targetBuildingObj == null)
            {
                Debug.LogWarning($"[TutorialActionForcedTouchBuilding] 타겟 건물을 찾을 수 없음: {actionKey}");
                context.NextObj.SetActive(true);
                return;
            }

            // TouchableBuilding에서 FacilityType 가져오기
            var touchableBuilding = _targetBuildingObj.GetComponent<TouchableBuilding>();
            if (touchableBuilding == null)
            {
                Debug.LogWarning($"[TutorialActionForcedTouchBuilding] TouchableBuilding 컴포넌트를 찾을 수 없음: {actionKey}");
                context.NextObj.SetActive(true);
                return;
            }

            _targetFacilityType = touchableBuilding.FacilityType;

            // 홀 마스크 타겟 설정 (3D 건물 위치)
            context.TargetUnmaskObj = _targetBuildingObj;

            // TouchableBuilding의 건설 완료 콜백 등록
            TouchableBuilding.OnBuildCompleteClicked += OnBuildCompleteClickedHandler;
        }

        public void OnNext(TutorialActionContext context)
        {
            // 다음 버튼 비활성화 (건물 클릭으로만 진행)
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 타겟이 없을 때만 딤드 클릭으로 진행 가능
            return _targetBuildingObj == null;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 콜백 해제
            TouchableBuilding.OnBuildCompleteClicked -= OnBuildCompleteClickedHandler;

            // 타겟 정리
            context.TargetUnmaskObj = null;
            _targetBuildingObj = null;
            OnBuildingClicked = null;
        }

        /// <summary>
        /// 건설 완료 터치 시 호출되는 핸들러
        /// </summary>
        private static void OnBuildCompleteClickedHandler(ElpisFacilityType facilityType)
        {
            // 타겟 FacilityType과 일치하는지 확인
            if (facilityType != _targetFacilityType)
                return;

            // 콜백 호출
            OnBuildingClicked?.Invoke();
        }
    }
}
