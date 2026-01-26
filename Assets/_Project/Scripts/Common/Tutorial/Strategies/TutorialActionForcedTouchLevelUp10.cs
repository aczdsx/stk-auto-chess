using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 특정 캐릭터 Lv10 달성까지 레벨업 버튼 강제 터치 액션.
    /// FORCED_TOUCH_UI와 유사하게 버튼을 최상위로 이동시키고,
    /// prefab_id로 지정된 캐릭터가 Lv10에 도달하면 다음 튜토리얼로 진행합니다.
    /// </summary>
    public class TutorialActionForcedTouchLevelUp10 : ITutorialActionStrategy
    {
        private const int TARGET_LEVEL = 10;

        /// <summary>
        /// Lv10 달성 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static Action OnLevelUpCompleted;

        /// <summary>
        /// 현재 등록된 버튼 참조 (정리용)
        /// </summary>
        private static Button _registeredButton;

        /// <summary>
        /// 현재 튜토리얼 컨텍스트 참조
        /// </summary>
        private static TutorialActionContext _currentContext;

        /// <summary>
        /// UI 원위치 복구 완료 여부
        /// </summary>
        private static bool _isRestored;

        /// <summary>
        /// 레이아웃 그룹 관련
        /// </summary>
        private static bool _layoutExist;
        private static LayoutGroup _layoutGroup;

        /// <summary>
        /// 캐릭터 데이터 구독
        /// </summary>
        private static IDisposable _characterUpdateSubscription;

        /// <summary>
        /// 타겟 캐릭터 ID
        /// </summary>
        private static int _targetCharacterId;

        public void OnShow(TutorialActionContext context)
        {
            context.TargetUIObj = TutorialTargetRegistry.FindGameObject(context.CurrentTutorial.tutorial_action_key);

            if (context.TargetUIObj == null)
            {
                Debug.LogWarning($"[TutorialActionForcedTouchLevelUp10] 타겟을 찾을 수 없음: {context.CurrentTutorial.tutorial_action_key}");
                context.NextObj.SetActive(true);
                return;
            }

            // 타겟 캐릭터 ID 저장 (prefab_id 사용)
            _targetCharacterId = 3401;

            // 현재 레벨 체크 - 이미 Lv10 이상이면 바로 완료
            var characterBridge = new CharacterDataBridge();
            int currentLevel = characterBridge.GetCharacterLevel(_targetCharacterId);
            if (currentLevel >= TARGET_LEVEL)
            {
                Debug.LogColor($"[TutorialActionForcedTouchLevelUp10] 이미 Lv{currentLevel} 달성됨, 즉시 완료", "green");
                context.NextObj.SetActive(true);
                OnLevelUpCompleted?.Invoke();
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

            // 마스크 홀 타겟 설정
            context.TargetUnmaskObj = context.TargetUIObj;

            // 화살표 설정
            context.ArrowRectTransform.gameObject.SetActive(true);
            context.WorldArrowRectTransform.gameObject.SetActive(true);
            Vector3 arrowTargetPosition = context.TargetUIObj.transform.localPosition;
            context.ArrowRectTransform.localPosition = new Vector3(
                arrowTargetPosition.x,
                arrowTargetPosition.y + context.CurrentTutorial.arrow_yPos,
                arrowTargetPosition.z);

            if (_layoutExist)
                _layoutGroup.enabled = false;

            // 버튼 클릭 이벤트 등록
            RegisterButtonListener(context.TargetUIObj, context);

            // 캐릭터 레벨 변경 구독
            SubscribeCharacterUpdate();
        }

        public void OnNext(TutorialActionContext context)
        {
            // 다음 버튼 비활성화 (레벨 조건 달성으로만 진행)
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로는 진행 불가 (레벨 조건 달성 필요)
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            // UI 원위치 복구
            RestoreUIBeforeCallback();

            // 버튼 클릭 이벤트 해제
            UnregisterButtonListener();

            // 캐릭터 업데이트 구독 해제
            UnsubscribeCharacterUpdate();

            // 컨텍스트 정리
            context.TargetUIObj = null;
            context.TargetUnmaskObj = null;
            context.OriginalParent = null;
            OnLevelUpCompleted = null;

            _layoutExist = false;
            _layoutGroup = null;
            _targetCharacterId = 0;
        }

        /// <summary>
        /// 캐릭터 데이터 업데이트 구독
        /// </summary>
        private static void SubscribeCharacterUpdate()
        {
            UnsubscribeCharacterUpdate();

            var characterBridge = new CharacterDataBridge();
            _characterUpdateSubscription = characterBridge.OnCharacterUpdated
                .Subscribe(OnCharacterUpdated);
        }

        /// <summary>
        /// 캐릭터 데이터 업데이트 구독 해제
        /// </summary>
        private static void UnsubscribeCharacterUpdate()
        {
            _characterUpdateSubscription?.Dispose();
            _characterUpdateSubscription = null;
        }

        /// <summary>
        /// 캐릭터 데이터 업데이트 시 호출
        /// </summary>
        private static void OnCharacterUpdated(Tech.Hive.V1.CharacterData characterData)
        {
            if (characterData == null) return;
            if (characterData.CharacterId != _targetCharacterId) return;

            Debug.LogColor($"[TutorialActionForcedTouchLevelUp10] 캐릭터 {_targetCharacterId} 레벨 업데이트: Lv{characterData.Level}", "cyan");

            if (characterData.Level >= TARGET_LEVEL)
            {
                Debug.LogColor($"[TutorialActionForcedTouchLevelUp10] Lv{TARGET_LEVEL} 달성! 다음 튜토리얼로 진행", "green");

                // UI 원위치 복구 후 콜백 호출
                RestoreUIBeforeCallback();
                OnLevelUpCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 타겟 오브젝트의 버튼에 클릭 리스너 등록
        /// </summary>
        private static void RegisterButtonListener(GameObject targetObj, TutorialActionContext context)
        {
            UnregisterButtonListener();

            if (targetObj == null) return;

            _registeredButton = targetObj.GetComponent<Button>();
            if (_registeredButton == null)
            {
                Debug.LogWarning($"[TutorialActionForcedTouchLevelUp10] Button 컴포넌트를 찾을 수 없음: {targetObj.name}");
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
        /// 버튼 클릭 시 호출 (레벨업 진행, Lv10 미달이면 계속 유지)
        /// </summary>
        private static void OnButtonClickHandler()
        {
            // 버튼 클릭 시에는 레벨업이 진행되지만,
            // Lv10 달성 여부는 OnCharacterUpdated에서 체크
            // 여기서는 아무것도 하지 않음 (기본 버튼 동작만 실행)
            Debug.LogColor("[TutorialActionForcedTouchLevelUp10] 레벨업 버튼 클릭됨", "cyan");
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
