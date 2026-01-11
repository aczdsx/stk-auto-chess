using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 특정 UI 버튼 강조 액션.
    /// 타겟 버튼을 최상위로 이동시켜 해당 버튼만 터치 가능하게 만듭니다.
    /// 버튼 클릭 시 자동으로 다음 튜토리얼로 진행됩니다.
    /// </summary>
    public class TutorialActionForcedTouchUI : ITutorialActionStrategy
    {
        /// <summary>
        /// 버튼 클릭 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static System.Action OnButtonClicked;

        /// <summary>
        /// 현재 등록된 버튼 참조 (정리용)
        /// </summary>
        private static Button _registeredButton;

        public void OnShow(TutorialActionContext context)
        {
            // 타겟 오브젝트 찾기
            context.TargetUIObj = TutorialTargetRegistry.FindGameObject(context.CurrentTutorial.tutorial_action_key);

            if (context.TargetUIObj == null)
            {
                // 타겟을 찾지 못하면 다음 버튼으로 진행
                Debug.LogWarning($"[TutorialActionForcedTouchUI] 타겟을 찾을 수 없음: {context.CurrentTutorial.tutorial_action_key}");
                context.NextObj.SetActive(true);
                return;
            }

            // 버튼 원위치 정보 저장
            context.OriginalParent = context.TargetUIObj.transform.parent;
            context.OriginalSiblingIndex = context.TargetUIObj.transform.GetSiblingIndex();
            context.OriginalPosition = context.TargetUIObj.transform.localPosition;

            // 타겟을 최상위로 이동
            context.TargetUIObj.transform.SetParent(context.TargetSpawnTransform, true);

            // 화살표 설정
            context.ArrowRectTransform.gameObject.SetActive(true);
            Vector3 arrowTargetPosition = context.TargetUIObj.transform.localPosition;
            context.ArrowRectTransform.localPosition = new Vector3(
                arrowTargetPosition.x,
                arrowTargetPosition.y + context.CurrentTutorial.arrow_yPos,
                arrowTargetPosition.z);

            // 버튼 클릭 이벤트 등록
            RegisterButtonListener(context.TargetUIObj);
        }

        public void OnNext(TutorialActionContext context)
        {
            // 다음 버튼 비활성화 (버튼 클릭으로만 진행)
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 타겟이 없을 때만 딤드 클릭으로 진행 가능
            // (타겟이 있으면 해당 버튼을 눌러야 함)
            return context.TargetUIObj == null;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 버튼 클릭 이벤트 해제
            UnregisterButtonListener();

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 버튼 원위치 복구
            if (context.OriginalParent != null && context.TargetUIObj != null)
            {
                context.TargetUIObj.transform.SetParent(context.OriginalParent);
                context.TargetUIObj.transform.SetSiblingIndex(context.OriginalSiblingIndex);
                context.TargetUIObj.transform.localPosition = context.OriginalPosition;
            }

            // 컨텍스트 정리
            context.TargetUIObj = null;
            context.OriginalParent = null;
            OnButtonClicked = null;
        }

        /// <summary>
        /// 타겟 오브젝트의 버튼에 클릭 리스너 등록
        /// </summary>
        private static void RegisterButtonListener(GameObject targetObj)
        {
            // 기존 리스너 해제
            UnregisterButtonListener();

            if (targetObj == null) return;

            // Button 컴포넌트 찾기
            _registeredButton = targetObj.GetComponent<Button>();
            if (_registeredButton == null)
            {
                Debug.LogWarning($"[TutorialActionForcedTouchUI] Button 컴포넌트를 찾을 수 없음: {targetObj.name}");
                return;
            }

            _registeredButton.onClick.AddListener(OnButtonClickHandler);
        }

        /// <summary>
        /// 버튼 클릭 리스너 해제
        /// </summary>
        private static void UnregisterButtonListener()
        {
            if (_registeredButton != null)
            {
                _registeredButton.onClick.RemoveListener(OnButtonClickHandler);
                _registeredButton = null;
            }
        }

        /// <summary>
        /// 버튼 클릭 시 호출
        /// </summary>
        private static void OnButtonClickHandler()
        {
            OnButtonClicked?.Invoke();
        }
    }
}
