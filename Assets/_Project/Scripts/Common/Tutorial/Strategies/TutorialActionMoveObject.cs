using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 오브젝트 이동 튜토리얼 액션.
    /// 3D 오브젝트를 A에서 B로 드래그하여 이동하도록 유도합니다.
    /// 두 개의 마스크 홀을 사용하여 Source(A)와 Destination(B)를 동시에 표시합니다.
    ///
    /// tutorial_action_key 형식: "SourceTargetId->DestTargetId"
    /// 예: "130601_0->Tile_5" (TutorialTarget에 등록된 ID)
    ///
    /// 이동이 완료되면 자동으로 다음 튜토리얼로 진행됩니다.
    /// </summary>
    public class TutorialActionMoveObject : ITutorialActionStrategy
    {
        private static readonly int HoleRadius2 = Shader.PropertyToID("_HoleRadius2");
        private static readonly int HoleCenter2 = Shader.PropertyToID("_HoleCenter2");
        private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");

        /// <summary>
        /// 이동 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static System.Action OnMoveObjectCompleted;

        /// <summary>
        /// 현재 튜토리얼에서 이동해야 할 Source 타겟 ID
        /// </summary>
        public static string SourceTargetId { get; private set; }

        /// <summary>
        /// 현재 튜토리얼에서 이동 목적지 타겟 ID
        /// </summary>
        public static string DestinationTargetId { get; private set; }

        /// <summary>
        /// 현재 오브젝트 이동 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        private GameObject _sourceObj;
        private GameObject _destObj;
        private TutorialActionContext _cachedContext;

        public void OnShow(TutorialActionContext context)
        {
            _cachedContext = context;

            // 화살표 비활성화 (두 개의 홀로 가이드)
            context.ArrowRectTransform.gameObject.SetActive(false);

            // tutorial_action_key 파싱 (형식: "SourceId_DestId")
            ParseActionKey(context.CurrentTutorial.tutorial_action_key);

            if (string.IsNullOrEmpty(SourceTargetId) || string.IsNullOrEmpty(DestinationTargetId))
            {
                Debug.LogWarning($"[TutorialActionMoveObject] action_key 파싱 실패: {context.CurrentTutorial.tutorial_action_key}");
                context.NextObj.SetActive(true);
                return;
            }

            // Source 타겟 찾기
            _sourceObj = TutorialTargetRegistry.FindGameObject(SourceTargetId);
            if (_sourceObj == null)
            {
                Debug.LogWarning($"[TutorialActionMoveObject] Source 타겟을 찾을 수 없음: {SourceTargetId}");
            }

            // Destination 타겟 찾기
            _destObj = TutorialTargetRegistry.FindGameObject(DestinationTargetId);
            if (_destObj == null)
            {
                Debug.LogWarning($"[TutorialActionMoveObject] Destination 타겟을 찾을 수 없음: {DestinationTargetId}");
            }

            // Source를 첫 번째 홀에 설정
            context.TargetUnmaskObj = _sourceObj;

            // Destination을 두 번째 홀에 설정
            SetSecondHole(context);

            IsActive = true;
        }

        public void OnNext(TutorialActionContext context)
        {
            // 이동 튜토리얼에서는 "다음" 버튼을 표시하지 않음
            // 오브젝트 이동이 완료되면 자동으로 진행됨
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 반드시 오브젝트를 이동해야 함
            // 타겟이 없으면 딤드 클릭으로 진행 가능 (fallback)
            return _sourceObj == null || _destObj == null;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 두 번째 홀 숨기기
            HideSecondHole(context);

            // 상태 초기화
            IsActive = false;
            SourceTargetId = null;
            DestinationTargetId = null;
            OnMoveObjectCompleted = null;
            _sourceObj = null;
            _destObj = null;
            _cachedContext = null;
            context.TargetUnmaskObj = null;
        }

        private const string SEPARATOR = "->";

        /// <summary>
        /// tutorial_action_key 파싱 (형식: "SourceId->DestId")
        /// </summary>
        private void ParseActionKey(string actionKey)
        {
            SourceTargetId = null;
            DestinationTargetId = null;

            if (string.IsNullOrEmpty(actionKey))
            {
                Debug.LogWarning("[TutorialActionMoveObject] action_key가 비어있습니다.");
                return;
            }

            // '->'로 분리
            int separatorIndex = actionKey.IndexOf(SEPARATOR);
            if (separatorIndex <= 0 || separatorIndex >= actionKey.Length - SEPARATOR.Length)
            {
                Debug.LogWarning($"[TutorialActionMoveObject] action_key 형식 오류: {actionKey}. 'SourceId->DestId' 형식이어야 합니다.");
                return;
            }

            SourceTargetId = actionKey.Substring(0, separatorIndex);
            DestinationTargetId = actionKey.Substring(separatorIndex + SEPARATOR.Length);

            Debug.LogColor($"[TutorialActionMoveObject] Source: {SourceTargetId}, Dest: {DestinationTargetId}", "green");
        }

        /// <summary>
        /// 두 번째 홀 설정 (Destination 위치)
        /// </summary>
        private void SetSecondHole(TutorialActionContext context)
        {
            if (_destObj == null || context.MaskMaterial == null)
            {
                return;
            }

            // Destination의 UV 좌표 계산
            Vector2 destUV = CalculateWorldPositionUV(context, _destObj.transform.position);

            // 두 번째 홀 설정
            float aspectRatio = (float)Screen.width / Screen.height;
            context.MaskMaterial.SetFloat(AspectRatio, aspectRatio);
            context.MaskMaterial.SetVector(HoleCenter2, new Vector4(destUV.x, destUV.y, 0, 0));
            context.MaskMaterial.SetFloat(HoleRadius2, context.CurrentTutorial.hole_radius);
        }

        /// <summary>
        /// 두 번째 홀 숨기기
        /// </summary>
        private void HideSecondHole(TutorialActionContext context)
        {
            if (context.MaskMaterial != null)
            {
                context.MaskMaterial.SetFloat(HoleRadius2, 0f);
            }
        }

        /// <summary>
        /// 3D 월드 좌표를 마스크 UV 좌표로 변환
        /// </summary>
        private Vector2 CalculateWorldPositionUV(TutorialActionContext context, Vector3 worldPosition)
        {
            Camera cam = context.MainCamera ?? Camera.main;
            if (cam == null)
            {
                return new Vector2(0.5f, 0.5f);
            }

            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);

            if (screenPosition.z < 0)
            {
                return new Vector2(0.5f, 0.5f);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                context.CanvasRectTransform,
                screenPosition,
                null,
                out var localPoint);

            return new Vector2(
                (localPoint.x + (context.CanvasRectTransform.rect.width * 0.5f)) / context.CanvasRectTransform.rect.width,
                (localPoint.y + (context.CanvasRectTransform.rect.height * 0.5f)) / context.CanvasRectTransform.rect.height);
        }

        /// <summary>
        /// 두 번째 홀 위치 업데이트 (매 프레임 호출 - TutorialController에서)
        /// </summary>
        public void UpdateSecondHolePosition()
        {
            if (!IsActive || _destObj == null || _cachedContext == null)
            {
                return;
            }

            Vector2 destUV = CalculateWorldPositionUV(_cachedContext, _destObj.transform.position);
            _cachedContext.MaskMaterial?.SetVector(HoleCenter2, new Vector4(destUV.x, destUV.y, 0, 0));
        }

        /// <summary>
        /// 오브젝트 이동 완료 시 외부에서 호출
        /// </summary>
        public static void NotifyMoveCompleted()
        {
            if (IsActive)
            {
                OnMoveObjectCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 지정된 Source 오브젝트만 선택 가능한지 확인
        /// </summary>
        public static bool CanSelectObject(string targetId)
        {
            if (!IsActive || string.IsNullOrEmpty(SourceTargetId))
            {
                return true;
            }

            return targetId == SourceTargetId;
        }

        /// <summary>
        /// 지정된 Destination으로 이동 가능한지 확인
        /// </summary>
        public static bool CanMoveToDestination(string targetId)
        {
            if (!IsActive || string.IsNullOrEmpty(DestinationTargetId))
            {
                return true;
            }

            return targetId == DestinationTargetId;
        }
    }
}
