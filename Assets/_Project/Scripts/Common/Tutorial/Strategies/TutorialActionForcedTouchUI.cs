using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 특정 UI 버튼 강조 액션.
    /// 타겟 버튼을 최상위로 이동시켜 해당 버튼만 터치 가능하게 만듭니다.
    /// </summary>
    public class TutorialActionForcedTouchUI : ITutorialActionStrategy
    {
        public void OnShow(TutorialActionContext context)
        {
            // ShowNextTutorial 시점에는 아무것도 안 함
            // OnNext에서 타겟 설정
        }

        public void OnNext(TutorialActionContext context)
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
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 타겟이 없을 때만 딤드 클릭으로 진행 가능
            // (타겟이 있으면 해당 버튼을 눌러야 함)
            return context.TargetUIObj == null;
        }

        public void OnClear(TutorialActionContext context)
        {
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
        }
    }
}
