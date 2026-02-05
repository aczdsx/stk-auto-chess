using Cysharp.Threading.Tasks;
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

        /// <summary>
        /// 현재 튜토리얼 컨텍스트 참조 (버튼 클릭 시 원위치 복구용)
        /// </summary>
        private static TutorialActionContext _currentContext;

        /// <summary>
        /// UI 원위치 복구 완료 여부
        /// </summary>
        private static bool _isRestored;

        private static bool _layoutExist = false;
        private static LayoutGroup _layoutGroup;

        public void OnShow(TutorialActionContext context)
        {
            // 3D 터치 차단 (캐릭터 드래그 방지)
            TutorialTouchBlocker.Allow3DTouch = false;

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
            if (context.OriginalParent.TryGetComponent<LayoutGroup>(out LayoutGroup resComponent))
            {
                _layoutExist = true;
                _layoutGroup = resComponent;
            }

            // 타겟을 최상위로 이동
            context.TargetUIObj.transform.SetParent(context.TargetSpawnTransform, true);

            // 마스크 홀 타겟 설정 (해당 UI 위치에 홀 생성)
            context.TargetUnmaskObj = context.TargetUIObj;

            // 화살표 설정
            context.ArrowRectTransform.gameObject.SetActive(true);
            context.WorldArrowRectTransform.gameObject.SetActive(true);

            // 타겟의 월드 위치를 화살표 부모 기준 로컬 좌표로 변환 (앵커 무관)
            RectTransform targetRect = context.TargetUIObj.GetComponent<RectTransform>();
            RectTransform arrowParent = context.ArrowRectTransform.parent as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arrowParent,
                RectTransformUtility.WorldToScreenPoint(context.TutorialCanvas.worldCamera, targetRect.position),
                context.TutorialCanvas.worldCamera,
                out localPoint);

            context.ArrowRectTransform.localPosition = new Vector3(
                localPoint.x,
                localPoint.y + context.CurrentTutorial.arrow_yPos,
                0);

            if (_layoutExist)
                _layoutGroup.enabled = false;

            // 버튼 클릭 이벤트 등록
            RegisterButtonListener(context.TargetUIObj, context);
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

        public async void OnClear(TutorialActionContext context)
        {
            // UI 원위치 복구 (아직 복구되지 않은 경우에만 실행)
            RestoreUIBeforeCallback();

            // 버튼 클릭 이벤트 해제
            UnregisterButtonListener();

            // 컨텍스트 정리
            context.TargetUIObj = null;
            context.TargetUnmaskObj = null;
            context.OriginalParent = null;
            OnButtonClicked = null;

            _layoutExist = false;
            _layoutGroup = null;
        }

        /// <summary>
        /// 타겟 오브젝트의 버튼에 클릭 리스너 등록
        /// </summary>
        private static void RegisterButtonListener(GameObject targetObj, TutorialActionContext context)
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

            _currentContext = context;
            _isRestored = false;
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
            _currentContext = null;
        }

        /// <summary>
        /// 버튼 클릭 시 호출
        /// OnButtonClicked 콜백 호출 전에 UI 원위치 복구를 먼저 수행
        /// </summary>
        private static void OnButtonClickHandler()
        {
            // OnButtonClicked 호출 전에 UI 원위치 복구 수행
            RestoreUIBeforeCallback();
            OnButtonClicked?.Invoke();
        }

        /// <summary>
        /// 버튼 클릭 콜백 전에 UI를 원위치로 복구
        /// </summary>
        private static void RestoreUIBeforeCallback()
        {
            if (_currentContext == null || _isRestored) return;

            _isRestored = true;

            // 화살표 비활성화
            _currentContext.ArrowRectTransform.gameObject.SetActive(false);
            _currentContext.WorldArrowRectTransform.gameObject.SetActive(false);

            var targetUIObj = _currentContext.TargetUIObj;

            // 버튼 원위치 복구
            if (targetUIObj != null)
            {
                if (_layoutGroup != null)
                    _layoutGroup.enabled = false;

                targetUIObj.transform.SetParent(_currentContext.OriginalParent);
                targetUIObj.transform.SetSiblingIndex(_currentContext.OriginalSiblingIndex);
                targetUIObj.transform.localPosition = _currentContext.OriginalPosition;

                if (_layoutGroup != null)
                    _layoutGroup.enabled = true;
            }
        }
    }
}
