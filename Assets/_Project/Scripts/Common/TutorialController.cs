using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    private static readonly int Show = Animator.StringToHash("Show");
    private static readonly int LongShow = Animator.StringToHash("LongShow");
    private static readonly int Close = Animator.StringToHash("Close");
    private static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
    private static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
    private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");

    [SerializeField] private RectTransform _canvasRectTransform;

    [SerializeField] private RectTransform _bodyRectTransform;
    [SerializeField] private RectTransform _arrowRectTransform;
    [SerializeField] private Animator _tutorialAnimator;
    [SerializeField] private GameObject _nextObj;
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private Transform _targetSpawnTransform;

    public Material _maskMaterial;

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
        { TutorialActionType.FOCUS_OBJECT, new TutorialActionFocusObject() }
    };

    protected void Update()
    {
        UpdateMaskPosition();
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
    }

    private void InitializeContext()
    {
        _actionContext = new TutorialActionContext
        {
            NextObj = _nextObj,
            ArrowRectTransform = _arrowRectTransform,
            TargetSpawnTransform = _targetSpawnTransform
        };
    }

    public void SetTutorial(List<TutorialDialogue> specTutorialList, bool isLongShow)
    {
        _currentSpecTutorialList = specTutorialList;
        _tutorialListIndex = 0;
        _actionContext.TargetUnmaskObj = null;

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
        // 이전 전략 정리 (버튼 원위치 등)
        RestorePreviousState();

        CurrentSpecTutorial = specTutorial;
        _actionContext.CurrentTutorial = specTutorial;

        // 텍스트 설정
        string tutorialText = LanguageManager.Instance.GetLanguageText(CurrentSpecTutorial.desc_key);
        _descText.text = tutorialText;
        _nextObj.SetActive(false);
        _actionContext.TargetUnmaskObj = null;

        // 말풍선 위치 이동
        var targetPosition = new Vector3(0, CurrentSpecTutorial.popup_yPos, 0);
        _bodyRectTransform.DOLocalMove(targetPosition, 0.6f).SetEase(Ease.OutQuad);

        // 전략 선택 및 실행
        _currentStrategy = GetStrategy(CurrentSpecTutorial.tutorial_action_type);
        _currentStrategy?.OnShow(_actionContext);
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

    #region Mask Animation

    private void UpdateMaskPosition()
    {
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
        if (CurrentSpecTutorial != null && CurrentSpecTutorial.tutorial_action_type == TutorialActionType.FOCUS_OBJECT)
        {
            var targetRectTransform = targetObj.GetComponent<RectTransform>();
            if (targetRectTransform == null)
            {
                Debug.LogWarning("UnMaskUI 대상 오브젝트에 RectTransform이 없습니다.");
                return _currentUvPosition;
            }

            return GetNormalizedPosition(_canvasRectTransform, targetRectTransform);
        }
        else
        {
            Vector3 worldPosition = targetObj.transform.position;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            if (screenPosition.z < 0)
            {
                Debug.LogWarning("Target object is behind the camera.");
                return _currentUvPosition;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPosition, null,
                out var localPoint);

            return new Vector2(
                (localPoint.x + (_canvasRectTransform.rect.width * 0.5f)) / _canvasRectTransform.rect.width,
                (localPoint.y + (_canvasRectTransform.rect.height * 0.5f)) / _canvasRectTransform.rect.height);
        }
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
        float normalizedX = (rect.localPosition.x + (_canvasRectTransform.sizeDelta.x * 0.5f)) /
                            _canvasRectTransform.sizeDelta.x;
        float normalizedY = (rect.localPosition.y + (_canvasRectTransform.sizeDelta.y * 0.5f)) /
                            _canvasRectTransform.sizeDelta.y;

        if (CurrentSpecTutorial.tutorial_action_key == "PointMileStoneUI")
        {
            // normalizedY += 0.5f;
        }
        else
        {
            normalizedY -= 0.46f;
        }

        return new Vector2(normalizedX, normalizedY);
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
