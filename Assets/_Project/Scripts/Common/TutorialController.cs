using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    /// <summary>
    /// 구멍 외 영역 터치 차단 여부 (데이터로 제어 가능)
    /// </summary>
    [Header("Touch Blocking")]
    [SerializeField] private bool _blockTouchOutsideHole = true;

    private TutorialMaskRaycastFilter _maskRaycastFilter;

    private static readonly int Show = Animator.StringToHash("Show");
    private static readonly int LongShow = Animator.StringToHash("LongShow");
    private static readonly int Close = Animator.StringToHash("Close");
    private static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
    private static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
    private static readonly int HoleRadius2 = Shader.PropertyToID("_HoleRadius2");
    private static readonly int HoleCenter2 = Shader.PropertyToID("_HoleCenter2");
    private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");
    private static readonly int MaskAlpha = Shader.PropertyToID("_MaskAlpha");

    [SerializeField] private RectTransform _canvasRectTransform;

    [SerializeField] private RectTransform _bodyRectTransform;
    [SerializeField] private RectTransform _arrowRectTransform;
    [SerializeField] private Animator _tutorialAnimator;
    [SerializeField] private GameObject _nextObj;
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private Transform _targetSpawnTransform;
    [SerializeField] private Image _dimmedImage;

    [Header("3D World Target Arrow")]
    [SerializeField] private RectTransform _worldArrowRectTransform;
    [SerializeField] private SpriteLoader _spriteLoaderCharacter;
    [SerializeField] private TextMeshProUGUI _characterNameText;
    [SerializeField] private Canvas _tutorialCanvas;
    [SerializeField] private GameObject _dragObj;

    [Header("Tutorial Toast Pop")]
    [SerializeField] private GameObject _tutorialToastObj;
    [SerializeField] private TextMeshProUGUI _tutoiralText;
    [SerializeField] private Animator _tutorialToastAnimator;
    public Material _maskMaterial;

    [Header("Dialogue Bubble Offset")]
    [SerializeField] private float _characterBubbleOffsetX = 200f;

    private List<TutorialDialogue> _currentSpecTutorialList;

    private AnimationState _currentState = AnimationState.Idle;

    private Vector2 _currentUvPosition;
    private Vector2 _lerpUvPosition;

    private int _tutorialListIndex;
    public TutorialDialogue CurrentSpecTutorial { get; private set; }

    // Manager와의 통신을 위한 콜백 (Manager가 주입)
    private Action _onTutorialCloseRequested;

    // 전략 패턴
    private ITutorialActionStrategy _currentStrategy;
    private TutorialActionContext _actionContext;

    // 전략 인스턴스 캐싱
    private static readonly Dictionary<TutorialActionType, ITutorialActionStrategy> _strategies = new()
    {
        { TutorialActionType.NONE, new TutorialActionNone() },
        { TutorialActionType.FORCED_TOUCH_UI, new TutorialActionForcedTouchUI() },
        { TutorialActionType.FORCED_TOUCH_BUILD, new TutorialActionForcedTouchBuilding() },
        { TutorialActionType.FOCUS_OBJECT, new TutorialActionFocusObject() },
        { TutorialActionType.FOCUS_UI, new TutorialActionFocusUI() },
        { TutorialActionType.TOAST_MESSAGE, new TutorialActionToastMessage() },
        { TutorialActionType.SHOW_DIALOGUE_POP, new TutorialActionShowDialoguePop() },
        { TutorialActionType.SHOW_DIALOGUE_POP_CALLBACK, new TutorialActionShowDialoguePopWithCallback() },
        { TutorialActionType.CHARACTER_PLACEMENT_UI, new TutorialActionCharacterPlacementUI() },
        { TutorialActionType.SPAWN_ENEMY, new TutorialActionSpawnEnemy() },
        { TutorialActionType.MOVE_OBJECT, new TutorialActionMoveObject() }
    };
    protected void Update()
    {
        UpdateMaskPosition();
        // UpdateWorldArrowPosition();
        UpdateSecondHolePosition();
    }

    /// <summary>
    /// 전략별 홀 위치 업데이트 (A→B 왕복 애니메이션)
    /// </summary>
    private void UpdateSecondHolePosition()
    {
        if (CurrentSpecTutorial == null) return;

        // MOVE_OBJECT 전략
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.MOVE_OBJECT &&
            _currentStrategy is TutorialActionMoveObject moveStrategy)
        {
            moveStrategy.UpdateHolePositions();
        }

        // CHARACTER_PLACEMENT_UI 전략
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.CHARACTER_PLACEMENT_UI)
        {
            TutorialActionCharacterPlacementUI.UpdateHolePositions();
        }
    }

    public void OnClickDimmedBG()
    {
        if (!_nextObj.activeSelf) return;

        // 현재 전략에게 딤드 클릭 처리 가능 여부 확인
        if (_currentStrategy != null && _currentStrategy.CanProceedOnDimmedClick(_actionContext))
        {
            ProceedToNext();
        }
    }

    /// <summary>
    /// Manager가 Controller를 초기화할 때 호출. 콜백을 주입받음
    /// </summary>
    public void Initialize(Action onTutorialCloseRequested)
    {
        _onTutorialCloseRequested = onTutorialCloseRequested;
        InitializeContext();
        InitializeTouchBlocker();
    }

    /// <summary>
    /// 터치 차단 시스템 초기화
    /// </summary>
    private void InitializeTouchBlocker()
    {
        // DimmedImage에 RaycastFilter 추가
        if (_dimmedImage != null)
        {
            _maskRaycastFilter = _dimmedImage.gameObject.GetComponent<TutorialMaskRaycastFilter>();
            if (_maskRaycastFilter == null)
            {
                _maskRaycastFilter = _dimmedImage.gameObject.AddComponent<TutorialMaskRaycastFilter>();
            }
            _maskRaycastFilter.Initialize(_maskMaterial, _canvasRectTransform);
        }

        // 정적 TutorialTouchBlocker 초기화 (3D 터치 차단용)
        Camera uiCamera = _tutorialCanvas != null ? _tutorialCanvas.worldCamera : null;
        TutorialTouchBlocker.Initialize(_maskMaterial, _canvasRectTransform, uiCamera);
    }

    /// <summary>
    /// 구멍 외 영역 터치 차단 설정
    /// </summary>
    public void SetBlockTouchOutsideHole(bool block)
    {
        _blockTouchOutsideHole = block;

        if (_maskRaycastFilter != null)
        {
            _maskRaycastFilter.BlockOutsideHole = block;
        }

        TutorialTouchBlocker.IsBlocking = block;
    }

    /// <summary>
    /// 현재 터치 차단 상태 반환
    /// </summary>
    public bool IsBlockingTouchOutsideHole => _blockTouchOutsideHole;

    private void InitializeContext()
    {
        _actionContext = new TutorialActionContext
        {
            NextObj = _nextObj,
            ArrowRectTransform = _arrowRectTransform,
            TargetSpawnTransform = _targetSpawnTransform,
            WorldArrowRectTransform = _worldArrowRectTransform,
            TutorialCanvas = _tutorialCanvas,
            MainCamera = Camera.main,
            CanvasRectTransform = _canvasRectTransform,
            MaskMaterial = _maskMaterial,
            DragObj = _dragObj,
            DimmedImage = _dimmedImage,
            TutorialToastObj = _tutorialToastObj,
            TutorialToastText = _tutoiralText,
            TutorialToastAnimator = _tutorialToastAnimator
        };
    }

    public void SetTutorial(List<TutorialDialogue> specTutorialList, bool isLongShow)
    {
        _currentSpecTutorialList = specTutorialList;
        _tutorialListIndex = 0;
        _actionContext.TargetUnmaskObj = null;

        // HoleRadius 값 초기화
        _maskMaterial.SetFloat(HoleRadius, 0f);
        _maskMaterial.SetFloat(HoleRadius2, 0f);
        _maskMaterial.SetFloat(MaskAlpha, 1f);

        // 터치 차단 활성화
        SetBlockTouchOutsideHole(_blockTouchOutsideHole);

        ShowNextTutorial(_currentSpecTutorialList[_tutorialListIndex]);
        _tutorialAnimator.SetTrigger(isLongShow ? LongShow : Show);
    }

    /// <summary>
    /// 애니메이션 완료 후 호출됨 (Animator Event)
    /// </summary>
    public void OnNextObj()
    {
        _currentStrategy?.OnNext(_actionContext);
    }

    public void ShowNextTutorial(TutorialDialogue specTutorial)
    {
        Debug.LogColor($"ShowNextTutorial: {specTutorial.tutorial_action_type} {specTutorial.tutorial_action_key} {specTutorial.seq}", "green");
        // 이전 전략 정리 (버튼 원위치 등)
        RestorePreviousState();

        // 이전 전략 OnClear 호출 (전략 교체 시)
        _currentStrategy?.OnClear(_actionContext);

        // SHOW_DIALOGUE_POP에서 비활성화된 Canvas 복구
        if (_tutorialCanvas != null && !_tutorialCanvas.enabled)
        {
            _tutorialCanvas.enabled = true;
        }

        CurrentSpecTutorial = specTutorial;
        _actionContext.CurrentTutorial = specTutorial;

        _nextObj.SetActive(false);
        _actionContext.TargetUnmaskObj = null;

        // TOAST_MESSAGE, SHOW_DIALOGUE_POP 타입은 말풍선 숨김
        bool hideDialogueBubble = CurrentSpecTutorial.tutorial_action_type == TutorialActionType.TOAST_MESSAGE ||
                                   CurrentSpecTutorial.tutorial_action_type == TutorialActionType.SHOW_DIALOGUE_POP ||
                                   CurrentSpecTutorial.tutorial_action_type == TutorialActionType.SHOW_DIALOGUE_POP_CALLBACK;
        _bodyRectTransform.gameObject.SetActive(!hideDialogueBubble);

        if (!hideDialogueBubble)
        {

            // 텍스트 설정
            // string tutorialText = LanguageManager.Instance.GetDefaultText(CurrentSpecTutorial.desc_key);
            Debug.LogColor($"CurrentSpecTutorial.prefab_id: {CurrentSpecTutorial.prefab_id}", "green");
            var characterInfo = SpecDataManager.Instance.GetCharacterData(CurrentSpecTutorial.prefab_id);//id로 가져와져서 바꿔야함.
            _spriteLoaderCharacter.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(characterInfo.prefab_id)).Forget();

            _characterNameText.text = LanguageManager.Instance.GetDefaultText(characterInfo.name_token);
            string tutorialText = CurrentSpecTutorial.desc_key;
            _descText.text = tutorialText;

            // 말풍선 위치 결정
            var targetPosition = CalculateDialogueBubblePosition(CurrentSpecTutorial);
            _bodyRectTransform.DOLocalMove(targetPosition, 0.6f).SetEase(Ease.OutQuad);
        }

        // 전략 선택 및 실행
        _currentStrategy = GetStrategy(CurrentSpecTutorial.tutorial_action_type);
        _currentStrategy?.OnShow(_actionContext);

        // TOAST_MESSAGE 전략일 경우 토스트 완료 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.TOAST_MESSAGE)
        {
            TutorialActionToastMessage.OnToastCompleted = OnToastCompleted;
        }

        // FORCED_TOUCH_UI 전략일 경우 버튼 클릭 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.FORCED_TOUCH_UI)
        {
            TutorialActionForcedTouchUI.OnButtonClicked = OnForcedTouchUIButtonClicked;
        }

        // CHARACTER_PLACEMENT_UI 전략일 경우 배치 완료 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.CHARACTER_PLACEMENT_UI)
        {
            TutorialActionCharacterPlacementUI.OnPlacementCompleted = OnCharacterPlacementUICompleted;
        }

        // SPAWN_ENEMY 전략일 경우 스폰 완료 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.SPAWN_ENEMY)
        {
            TutorialActionSpawnEnemy.OnSpawnEnemyCompleted = OnSpawnEnemyCompleted;
        }

        // MOVE_OBJECT 전략일 경우 이동 완료 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.MOVE_OBJECT)
        {
            TutorialActionMoveObject.OnMoveObjectCompleted = OnMoveObjectCompleted;
        }

        // SHOW_DIALOGUE_POP_WITH_CALLBACK 전략일 경우 다이얼로그 완료 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.SHOW_DIALOGUE_POP_CALLBACK)
        {
            TutorialActionShowDialoguePopWithCallback.OnDialogueCompleted = OnDialoguePopWithCallbackCompleted;
        }

        // FORCED_TOUCH_BUILD 전략일 경우 건물 클릭 콜백 설정
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.FORCED_TOUCH_BUILD)
        {
            TutorialActionForcedTouchBuilding.OnBuildingClicked = OnForcedTouchBuildingClicked;
        }
    }

    /// <summary>
    /// 강제 터치 UI 버튼 클릭 시 호출되는 콜백
    /// </summary>
    private void OnForcedTouchUIButtonClicked()
    {
        // 콜백 해제
        TutorialActionForcedTouchUI.OnButtonClicked = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }

    /// <summary>
    /// 건물 강제 터치 클릭 시 호출되는 콜백
    /// </summary>
    private void OnForcedTouchBuildingClicked()
    {
        // 콜백 해제
        TutorialActionForcedTouchBuilding.OnBuildingClicked = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }

    /// <summary>
    /// 토스트 메시지 완료 시 호출되는 콜백 (ToastManager용 - 레거시)
    /// </summary>
    private void OnToastCompleted()
    {
        // 콜백 해제
        TutorialActionToastMessage.OnToastCompleted = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }


    /// <summary>
    /// UI 캐릭터 배치 완료 시 호출되는 콜백
    /// </summary>
    private void OnCharacterPlacementUICompleted()
    {
        // 콜백 해제
        TutorialActionCharacterPlacementUI.OnPlacementCompleted = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }

    /// <summary>
    /// 적 스폰 완료 시 호출되는 콜백
    /// </summary>
    private void OnSpawnEnemyCompleted()
    {
        // 콜백 해제
        TutorialActionSpawnEnemy.OnSpawnEnemyCompleted = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }

    /// <summary>
    /// 오브젝트 이동 완료 시 호출되는 콜백
    /// </summary>
    private void OnMoveObjectCompleted()
    {
        // 콜백 해제
        TutorialActionMoveObject.OnMoveObjectCompleted = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }

    /// <summary>
    /// 다이얼로그 팝업 완료 시 호출되는 콜백 (콜백 버전)
    /// </summary>
    private void OnDialoguePopWithCallbackCompleted()
    {
        // 콜백 해제
        TutorialActionShowDialoguePopWithCallback.OnDialogueCompleted = null;

        // 다음 튜토리얼로 진행
        ProceedToNext();
    }

    /// <summary>
    /// 튜토리얼 제거 - TutorialManager의 가이드가 다 비었을 때 호출됨
    /// </summary>
    public void ClearTutorial()
    {
        if (_currentSpecTutorialList == null)
        {
            return;
        }

        _actionContext.TargetUnmaskObj = null;

        // 현재 전략 정리
        _currentStrategy?.OnClear(_actionContext);

        // 월드 화살표 비활성화
        if (_worldArrowRectTransform != null)
        {
            _worldArrowRectTransform.gameObject.SetActive(false);
        }

        // 터치 차단 해제
        SetBlockTouchOutsideHole(false);

        _tutorialAnimator.SetTrigger(Close);

        _currentSpecTutorialList = null;
        _currentStrategy = null;
    }

    /// <summary>
    /// 다음 튜토리얼로 진행
    /// </summary>
    private void ProceedToNext()
    {
        if (_tutorialListIndex + 1 >= _currentSpecTutorialList.Count)
        {
            Debug.LogColor("튜토리얼 종료", "green");
            _onTutorialCloseRequested?.Invoke();
        }
        else
        {
            _tutorialListIndex++;
            ShowNextTutorial(_currentSpecTutorialList[_tutorialListIndex]);
        }
    }

    /// <summary>
    /// 이전 상태 복구 (버튼 원위치 등)
    /// </summary>
    private void RestorePreviousState()
    {
        if (_actionContext.OriginalParent != null && _actionContext.TargetUIObj != null)
        {
            _actionContext.TargetUIObj.transform.SetParent(_actionContext.OriginalParent);
            _actionContext.TargetUIObj.transform.SetSiblingIndex(_actionContext.OriginalSiblingIndex);
            _actionContext.TargetUIObj.transform.localPosition = _actionContext.OriginalPosition;

            _actionContext.TargetUIObj = null;
            _actionContext.OriginalParent = null;
        }
    }

    /// <summary>
    /// 전략 인스턴스 가져오기
    /// </summary>
    private ITutorialActionStrategy GetStrategy(TutorialActionType actionType)
    {
        if (_strategies.TryGetValue(actionType, out var strategy))
        {
            return strategy;
        }

        Debug.LogWarning($"[TutorialController] 알 수 없는 ActionType: {actionType}, NONE 전략 사용");
        return _strategies[TutorialActionType.NONE];
    }

    /// <summary>
    /// "x,y" 형식의 좌표 문자열을 Vector3로 파싱
    /// </summary>
    private Vector3 ParseCoordinate(string coordinate)
    {
        if (string.IsNullOrEmpty(coordinate))
            return Vector3.zero;

        var parts = coordinate.Split(',');
        if (parts.Length < 2)
            return Vector3.zero;

        float.TryParse(parts[0].Trim(), out float x);
        float.TryParse(parts[1].Trim(), out float y);

        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// 말풍선 위치 계산 - 캐릭터 TutorialTarget이 있으면 해당 캐릭터 기준, 없으면 coordinate 사용
    /// </summary>
    private Vector3 CalculateDialogueBubblePosition(TutorialDialogue tutorial)
    {
        // prefab_id로 TutorialTarget 찾기
        var characterTarget = CookApps.AutoBattler.TutorialTargetRegistry.Find(tutorial.prefab_id.ToString());

        if (characterTarget != null)
        {
            // 캐릭터 월드 좌표를 UI 로컬 좌표로 변환
            Vector3 worldPosition = characterTarget.transform.position;
            Vector3 localPosition = WorldToCanvasLocalPosition(worldPosition);

            // coordinate의 y값 사용 (y는 기존 설정값 유지)
            var coordY = ParseCoordinate(tutorial.coordinate).y;

            // x는 캐릭터 위치 + offset, y는 coordinate에서 지정한 값 사용
            return new Vector3(localPosition.x + _characterBubbleOffsetX, coordY, 0);
        }

        // TutorialTarget이 없으면 기존 coordinate 사용
        return ParseCoordinate(tutorial.coordinate);
    }

    /// <summary>
    /// 월드 좌표를 캔버스 로컬 좌표로 변환
    /// </summary>
    private Vector3 WorldToCanvasLocalPosition(Vector3 worldPosition)
    {
        Camera cam = _actionContext?.MainCamera;
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            return Vector3.zero;
        }

        Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0)
        {
            return Vector3.zero;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            screenPosition,
            _tutorialCanvas.worldCamera,
            out var localPoint);

        return new Vector3(localPoint.x, localPoint.y, 0);
    }

    #region World Arrow

    /// <summary>
    /// 3D 월드 타겟 화살표 위치 업데이트
    /// </summary>
    private void UpdateWorldArrowPosition()
    {
        if (_worldArrowRectTransform == null || _actionContext == null)
            return;

        // 현재 사용하지 않음 - 월드 화살표 비활성화
        _worldArrowRectTransform.gameObject.SetActive(false);
    }

    #endregion

    #region Mask Animation

    private void UpdateMaskPosition()
    {
        // 전략에서 마스크 업데이트 건너뛰기 요청 시 (SPAWN_ENEMY, TOAST_MESSAGE 등)
        if (_actionContext?.SkipMaskUpdate == true)
        {
            return;
        }

        var targetUnmaskObj = _actionContext?.TargetUnmaskObj;

        if (targetUnmaskObj == null || !targetUnmaskObj.activeInHierarchy)
        {
            if (_currentState != AnimationState.Shrinking && _currentState != AnimationState.IdleAfterShrinking)
            {
                ChangeState(AnimationState.Shrinking);
            }
        }
        else
        {
            if (_currentState != AnimationState.Growing && _currentState != AnimationState.IdleAfterGrowing)
            {
                ChangeState(AnimationState.Growing);
            }

            Vector2 targetUvPosition = CalculateTargetUvPosition(targetUnmaskObj);

            // 스무딩 처리
            if (_currentUvPosition != targetUvPosition)
            {
                _lerpUvPosition = _currentUvPosition;
                _currentUvPosition = targetUvPosition;
            }

            _lerpUvPosition = Vector2.Lerp(_lerpUvPosition, _currentUvPosition, Time.deltaTime * 5f);

            if (Vector2.Distance(_lerpUvPosition, _currentUvPosition) < 0.01f)
            {
                _lerpUvPosition = _currentUvPosition;
            }

            float aspectRatio = (float)Screen.width / Screen.height;
            _maskMaterial.SetFloat(AspectRatio, aspectRatio);
            _maskMaterial.SetVector(HoleCenter, new Vector4(_lerpUvPosition.x, _lerpUvPosition.y, 0, 0));
        }
    }

    private Vector2 CalculateTargetUvPosition(GameObject targetObj)
    {
        if (CurrentSpecTutorial == null)
        {
            return _currentUvPosition;
        }

        // FOCUS_UI: UI 전용 (RectTransform 필수)
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.FOCUS_UI)
        {
            var targetRectTransform = targetObj.GetComponent<RectTransform>();
            if (targetRectTransform != null)
            {
                return GetNormalizedPosition(_canvasRectTransform, targetRectTransform);
            }
            Debug.LogWarning("[TutorialController] FOCUS_UI 대상 오브젝트에 RectTransform이 없습니다.");
            return _currentUvPosition;
        }

        // FOCUS_OBJECT, CHARACTER_PLACEMENT_UI: UI(RectTransform) 또는 3D 오브젝트
        if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.FOCUS_OBJECT ||
            CurrentSpecTutorial.tutorial_action_type == TutorialActionType.CHARACTER_PLACEMENT_UI)
        {
            var targetRectTransform = targetObj.GetComponent<RectTransform>();
            if (targetRectTransform != null)
            {
                return GetNormalizedPosition(_canvasRectTransform, targetRectTransform);
            }
            // RectTransform 없으면 3D 월드 좌표 사용
            return CalculateWorldPositionUV(targetObj.transform.position);
        }

        // 3D 월드 오브젝트 (CHARACTER_PLACEMENT 등)
        return CalculateWorldPositionUV(targetObj.transform.position);
    }

    /// <summary>
    /// 3D 월드 좌표를 마스크 UV 좌표로 변환
    /// </summary>
    private Vector2 CalculateWorldPositionUV(Vector3 worldPosition)
    {
        Camera cam = _actionContext?.MainCamera;
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            Debug.LogWarning("[TutorialController] 카메라를 찾을 수 없습니다.");
            return _currentUvPosition;
        }

        Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0)
        {
            Debug.LogWarning("Target object is behind the camera.");
            return _currentUvPosition;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            screenPosition,
            null,
            out var localPoint);

        return new Vector2(
            (localPoint.x + (_canvasRectTransform.rect.width * 0.5f)) / _canvasRectTransform.rect.width,
            (localPoint.y + (_canvasRectTransform.rect.height * 0.5f)) / _canvasRectTransform.rect.height);
    }

    private void ChangeState(AnimationState newState)
    {
        if (_currentState == newState)
        {
            return;
        }

        _currentState = newState;

        if (newState == AnimationState.Growing)
        {
            float currentRadius = _maskMaterial.GetFloat(HoleRadius);
            DOTween.To(() => currentRadius, x =>
            {
                currentRadius = x;
                _maskMaterial.SetFloat(HoleRadius, currentRadius);
            }, CurrentSpecTutorial.hole_radius, 0.8f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _currentState = AnimationState.IdleAfterGrowing;
            });
        }
        else if (newState == AnimationState.Shrinking)
        {
            float currentRadius = _maskMaterial.GetFloat(HoleRadius);
            DOTween.To(() => currentRadius, x =>
            {
                currentRadius = x;
                _maskMaterial.SetFloat(HoleRadius, currentRadius);
            }, 0.0f, 0.8f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _currentState = AnimationState.IdleAfterShrinking;
            });
        }
    }

    private Vector2 GetNormalizedPosition(RectTransform canvasRect, RectTransform rect)
    {
        // UI 요소의 월드 좌표에서 중심점 계산
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) / 2f;

        // 월드 좌표 → 스크린 좌표
        Camera cam = _tutorialCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        // 스크린 좌표 → 캔버스 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            screenPoint,
            cam,
            out var localPoint);

        // 정규화
        return new Vector2(
            (localPoint.x + (_canvasRectTransform.rect.width * 0.5f)) / _canvasRectTransform.rect.width,
            (localPoint.y + (_canvasRectTransform.rect.height * 0.5f)) / _canvasRectTransform.rect.height);
    }

    #endregion

    private enum AnimationState
    {
        Idle,
        Growing,
        Shrinking,
        IdleAfterGrowing,
        IdleAfterShrinking,
    }
}
