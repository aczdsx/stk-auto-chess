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

    private Vector3 _currentTargetPosition;

    private Vector2 _currentUvPosition;
    private bool _isAnimating; // 애니메이션 실행 여부 플래그
    private bool _isSmoothing;
    private Vector3 _lerpTargetPosition;
    private Vector2 _lerpUvPosition;
    private Transform _originalParent; // 버튼의 원래 Parent 저장소
    private Vector3 _originalPosition; // 버튼의 원래 위치 저장소
    private int _originalSiblingIndex; // 버튼의 원래 Sibling Index 저장소
    private GameObject _targetUIObj;
    private GameObject _targetUnmaskObj;
    private int tutorialListIndex;
    public TutorialDialogue CurrentSpecTutorial { get; private set; }
    
    // Manager와의 통신을 위한 콜백 (Manager가 주입)
    private Action _onTutorialCloseRequested;

    protected void Update()
    {
        if (_targetUnmaskObj == null || !_targetUnmaskObj.activeInHierarchy)
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

            Vector2 targetUvPosition;

            if (CurrentSpecTutorial != null && CurrentSpecTutorial.tutorial_action_type == TutorialActionType.FOCUS_OBJECT)
            {
                var targetRectTransform = _targetUnmaskObj.GetComponent<RectTransform>();
                if (targetRectTransform == null)
                {
                    Debug.LogWarning("UnMaskUI 대상 오브젝트에 RectTransform이 없습니다.");
                    return;
                }

                targetUvPosition = GetNormalizedPosition(_canvasRectTransform, targetRectTransform);
            }
            else
            {
                Vector3 worldPosition = _targetUnmaskObj.transform.position;
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

                if (screenPosition.z < 0)
                {
                    Debug.LogWarning("Target object is behind the camera.");
                    return;
                }

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPosition, null,
                    out localPoint);

                targetUvPosition = new Vector2(
                    (localPoint.x + (_canvasRectTransform.rect.width * 0.5f)) / _canvasRectTransform.rect.width,
                    (localPoint.y + (_canvasRectTransform.rect.height * 0.5f)) / _canvasRectTransform.rect.height);
            }

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

            float aspectRatio = (float) Screen.width / Screen.height;
            _maskMaterial.SetFloat(AspectRatio, aspectRatio);
            _maskMaterial.SetVector(HoleCenter, new Vector4(_lerpUvPosition.x, _lerpUvPosition.y, 0, 0));
        }
    }

    public void OnClickDimmedBG()
    {
        if (_nextObj.activeSelf)
        {
            TutorialActionType currentActionType = _currentSpecTutorialList[tutorialListIndex].tutorial_action_type;
            bool checkError = currentActionType == TutorialActionType.FORCED_TOUCH_UI && _targetUIObj == null;

            if (currentActionType == TutorialActionType.NONE ||
                currentActionType == TutorialActionType.FOCUS_OBJECT || checkError)
            {
                if (tutorialListIndex + 1 >= _currentSpecTutorialList.Count)
                {
                    _onTutorialCloseRequested?.Invoke();
                }
                else
                {
                    tutorialListIndex++;
                    ShowNextTutorial(_currentSpecTutorialList[tutorialListIndex]);
                }
            }
        }
    }

    /// <summary>
    /// Manager가 Controller를 초기화할 때 호출. 콜백을 주입받음
    /// </summary>
    public void Initialize(Action onTutorialCloseRequested)
    {
        _onTutorialCloseRequested = onTutorialCloseRequested;
    }

    public void SetTutorial(List<TutorialDialogue> specTutorialList, bool isLongShow)
    {
        _currentSpecTutorialList = specTutorialList;
        tutorialListIndex = 0;
        _targetUnmaskObj = null;

        ShowNextTutorial(_currentSpecTutorialList[tutorialListIndex]);
        _tutorialAnimator.SetTrigger(isLongShow ? LongShow : Show);
    }

    public void OnNextObj()
    {
        switch (CurrentSpecTutorial.tutorial_action_type)
        {
            case TutorialActionType.NONE:
                _nextObj.SetActive(true);
                _arrowRectTransform.gameObject.SetActive(false);
                break;
            case TutorialActionType.FORCED_TOUCH_UI:
                // Target Obj 세팅
                _targetUIObj = GameObject.Find(CurrentSpecTutorial.tutorial_action_key);
                if (_targetUIObj == null)
                {
                    _nextObj.SetActive(true);
                }
                else
                {
                    _originalParent = _targetUIObj.transform.parent;
                    _originalSiblingIndex = _targetUIObj.transform.GetSiblingIndex();
                    _originalPosition = _targetUIObj.transform.localPosition;
                    _targetUIObj.transform.SetParent(_targetSpawnTransform, true);

                    // Arrow Obj 세팅
                    _arrowRectTransform.gameObject.SetActive(true);
                    Vector3 arrowTargetPosition = _targetUIObj.transform.localPosition;
                    _arrowRectTransform.localPosition = new Vector3(arrowTargetPosition.x,
                        arrowTargetPosition.y + CurrentSpecTutorial.arrow_yPos, arrowTargetPosition.z);
                }

                break;
            case TutorialActionType.FOCUS_OBJECT:
                _nextObj.SetActive(true);
                break;
        }
    }

    public void ShowNextTutorial(TutorialDialogue specTutorial)
    {
        CurrentSpecTutorial = specTutorial;
        // desc_key를 언어 토큰으로 사용하여 텍스트 가져오기 (DialoguePopup 방식 참고)
        string tutorialText = LanguageManager.Instance.GetLanguageText(CurrentSpecTutorial.desc_key);
        _descText.text = tutorialText;
        _nextObj.SetActive(false);
        _targetUnmaskObj = null;
        if (_originalParent != null && _targetUIObj != null)
        {
            _targetUIObj.transform.SetParent(_originalParent);
            _targetUIObj.transform.SetSiblingIndex(_originalSiblingIndex);
            _targetUIObj.transform.localPosition = _originalPosition;
        }

        var targetPosition = new Vector3(0, CurrentSpecTutorial.popup_yPos, 0);

        _bodyRectTransform.DOLocalMove(targetPosition, 0.6f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            Debug.Log("Movement Complete!");
        });

        switch (CurrentSpecTutorial.tutorial_action_type)
        {
            case TutorialActionType.NONE:
                _arrowRectTransform.gameObject.SetActive(false);
                break;
            case TutorialActionType.FORCED_TOUCH_UI:
                break;
            case TutorialActionType.FOCUS_OBJECT:
                _arrowRectTransform.gameObject.SetActive(false);
                _targetUnmaskObj = GameObject.Find(CurrentSpecTutorial.tutorial_action_key);
                break;
        }
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

        _targetUnmaskObj = null;
        switch (_currentSpecTutorialList[tutorialListIndex].tutorial_action_type)
        {
            case TutorialActionType.NONE:
                _nextObj.SetActive(false);
                break;
            case TutorialActionType.FORCED_TOUCH_UI:
                _arrowRectTransform.gameObject.SetActive(false);

                if (_originalParent != null && _targetUIObj != null)
                {
                    _targetUIObj.transform.SetParent(_originalParent);
                    _targetUIObj.transform.SetSiblingIndex(_originalSiblingIndex);
                    _targetUIObj.transform.localPosition = _originalPosition;
                }

                break;
            case TutorialActionType.FOCUS_OBJECT:
                _nextObj.SetActive(false);
                break;
        }

        _tutorialAnimator.SetTrigger(Close);

        _currentSpecTutorialList = null;
    }

    private void ChangeState(AnimationState newState)
    {
        if (_currentState == newState)
        {
            return; // 같은 상태로는 전환하지 않음
        }

        _currentState = newState;

        if (newState == AnimationState.Growing)
        {
            // DOTween으로 HoleRadius 증가
            float currentRadius = _maskMaterial.GetFloat(HoleRadius);
            DOTween.To(() => currentRadius, x =>
            {
                currentRadius = x;
                _maskMaterial.SetFloat(HoleRadius, currentRadius);
            }, CurrentSpecTutorial.hole_radius, 0.8f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _currentState = AnimationState.IdleAfterGrowing; // Grow 완료 후 Idle 상태로 전환
            });
        }
        else if (newState == AnimationState.Shrinking)
        {
            // DOTween으로 HoleRadius 감소
            float currentRadius = _maskMaterial.GetFloat(HoleRadius);
            DOTween.To(() => currentRadius, x =>
            {
                currentRadius = x;
                _maskMaterial.SetFloat(HoleRadius, currentRadius);
            }, 0.0f, 0.8f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _currentState = AnimationState.IdleAfterShrinking; // Shrink 완료 후 Idle 상태로 전환
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

        // Vector2 anchorRectSize = (rect.anchorMax - rect.anchorMin) * canvasRect.rect.size;
        // Vector2 anchorBase = rect.anchorMin * canvasRect.rect.size;
        // Vector2 pivotOffset = anchorRectSize * rect.pivot;
        // Vector2 finalLocalPos = anchorBase + pivotOffset + rect.anchoredPosition -
        //                         (canvasRect.rect.size * canvasRect.pivot);
        // Vector2 posFromBottomLeft = finalLocalPos + (canvasRect.rect.size * canvasRect.pivot);
        //
        // // Canvas 사이즈로 나누어 0~1 사이로 정규화
        // float normalizedX = posFromBottomLeft.x / canvasRect.rect.size.x;
        // float normalizedY = posFromBottomLeft.y / canvasRect.rect.size.y;
        //
        // return new Vector2(normalizedX, normalizedY);
    }

    private enum AnimationState
    {
        Idle,
        Growing,
        Shrinking,
        IdleAfterGrowing,
        IdleAfterShrinking,
    }
}
