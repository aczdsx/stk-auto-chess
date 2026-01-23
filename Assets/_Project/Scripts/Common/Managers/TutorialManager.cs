using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 씬별 튜토리얼 관리자.
/// - CheckAndInitTutorial: 씬 진입 시 튜토리얼 존재 여부 확인 및 초기화
/// - StartChapter1TutorialSequence: 챕터1 아웃게임 튜토리얼 시퀀스 시작
/// - HandleTutorialAction: 트리거로 튜토리얼 액션 처리
/// - HandleTutorialClose: 튜토리얼 종료 처리
/// - ClearTutorial: 모든 가이드 종료 시 정리
/// </summary>
public class TutorialManager : SingletonMonoBehaviour<TutorialManager>
{
    private const string TUTORIAL_CANVAS_PREFAB_PATH = "Prefabs/UI/Tutorial/InGameTutorialCanvas";

    private Canvas _canvas = null;

    public bool IsTutorialCanvasEnabled
    {
        get
        {
            if(_canvas == null) return false;
            return _canvas.enabled;
        }
        set
        {
            if(_canvas == null) return;
            _canvas.enabled = value;
        }
    }
    private TutorialController _tutorialController;
    private GameObject _tutorialCanvasInstance;

    private List<TutorialDialogue> _specTutorialDataList = new();
    public bool HasTutorialStage => _specTutorialDataList is { Count: > 0 } && _specTutorialDataList[0].tutorial_id > 0;
    public bool IsTutorial => _canvas != null;

    // 로비 준비 상태 관리
    private bool _isLobbyReady;
    private GuideMissionInfo _pendingLobbyTutorial;  // 로비 준비 대기 중인 튜토리얼
    private IDisposable _missionIdChangedSubscription;

    protected override void Awake()
    {
        base.Awake();
        SubscribeGuideMissionChanged();
        SubscribeUITransitionEvent();
    }

    protected override void OnDestroy()
    {
        _missionIdChangedSubscription?.Dispose();
        UnsubscribeUITransitionEvent();
        ClearTutorial();
        base.OnDestroy();
    }

    private void SubscribeUITransitionEvent()
    {
        SceneUILayerManager.OnUITransitionEvent += OnUITransitionEvent;
    }

    private void UnsubscribeUITransitionEvent()
    {
        SceneUILayerManager.OnUITransitionEvent -= OnUITransitionEvent;
    }

    /// <summary>
    /// UI 전환 이벤트 핸들러 - 팝업이 닫힐 때 STAY_DEFAULT_LOBBY 트리거 체크
    /// </summary>
    private void OnUITransitionEvent(UILayerTransition transition, string key, UILayer layer, object data)
    {
        // 팝업/모달이 닫힐 때만 체크
        if (transition != UILayerTransition.ExitFinished) return;
        if (layer.UILayerType is not (UILayerType.Popup or UILayerType.Modal)) return;

        // 다음 프레임에 체크 (UI Stack이 완전히 정리된 후)
        TryTriggerStayDefaultLobbyDelayed().Forget();
    }

    private async UniTask TryTriggerStayDefaultLobbyDelayed()
    {
        await UniTask.Yield();
        TryTriggerStayDefaultLobby();
    }

    #region 전역 가이드 미션 감시

    /// <summary>
    /// 가이드 미션 ID 변경 전역 구독
    /// </summary>
    private void SubscribeGuideMissionChanged()
    {
        var guideMissionBridge = new GuideMissionDataBridge();
        _missionIdChangedSubscription = guideMissionBridge.OnMissionIdChanged
            .Subscribe(missionId => OnGuideMissionIdChanged((int)missionId));
    }

    /// <summary>
    /// 가이드 미션 ID 변경 시 호출
    /// </summary>
    private void OnGuideMissionIdChanged(int guideMissionId)
    {
        var specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(guideMissionId);
        if (specGuideMissionData == null)
        {
            return;
        }

        // 튜토리얼 ID가 없으면 스킵
        if (specGuideMissionData.tutorial_id <= 0)
        {
            return;
        }

        // 이미 완료된 미션이면 스킵
        if (IsClearedGuideMission(guideMissionId))
        {
            return;
        }

        // 이미 튜토리얼 진행 중이면 스킵
        if (_pendingGuideMissionInfo != null)
        {
            return;
        }

        Debug.LogColor($"[TutorialManager] 가이드 미션 변경 감지: {guideMissionId}", "cyan");

        // 로비가 준비되어 있으면 즉시 실행, 아니면 대기
        if (_isLobbyReady)
        {
            StartLobbyTutorialAsync(specGuideMissionData).Forget();
        }
        else
        {
            _pendingLobbyTutorial = specGuideMissionData;
            Debug.LogColor($"[TutorialManager] 로비 준비 대기 중: {guideMissionId}", "yellow");
        }
    }

    /// <summary>
    /// 로비 기본 UI 준비 완료 알림 (LobbyMain.OnPostEnter에서 호출)
    /// </summary>
    public void NotifyLobbyReady()
    {
        _isLobbyReady = true;
        Debug.LogColor("[TutorialManager] 로비 준비 완료", "cyan");

        // 대기 중인 튜토리얼이 있으면 실행 시도
        TryTriggerStayDefaultLobby();
    }

    /// <summary>
    /// STAY_DEFAULT_LOBBY 트리거 시도
    /// - 로비 기본 UI 상태일 때만 발동
    /// - 대기 중인 튜토리얼이 있어야 함
    /// </summary>
    public void TryTriggerStayDefaultLobby()
    {
        if (!IsLobbyDefaultState())
        {
            Debug.LogColor("[TutorialManager] STAY_DEFAULT_LOBBY 대기 중 (팝업 열림)", "yellow");
            return;
        }

        if (_pendingLobbyTutorial == null)
        {
            return;
        }

        Debug.LogColor($"[TutorialManager] STAY_DEFAULT_LOBBY 트리거 발동: {_pendingLobbyTutorial.id}", "green");

        var pending = _pendingLobbyTutorial;
        _pendingLobbyTutorial = null;
        if(pending.id > 101) {return;}
        StartLobbyTutorialAsync(pending).Forget();
    }

    /// <summary>
    /// 로비 퇴장 알림 (LobbyMain.OnPreExit에서 호출)
    /// </summary>
    public void NotifyLobbyExit()
    {
        _isLobbyReady = false;
        _pendingLobbyTutorial = null;
        Debug.LogColor("[TutorialManager] 로비 퇴장", "cyan");
    }

    /// <summary>
    /// 로비 기본 UI 상태인지 확인
    /// - 로비가 준비된 상태
    /// - Popup/Modal 타입 UI가 없는 상태
    /// </summary>
    public bool IsLobbyDefaultState()
    {
        if (!_isLobbyReady) return false;

        // Popup, Modal 타입 UI가 있는지 확인 (Cover, Overlay 제외)
        var popupOrModalUIs = SceneUILayerManager.Instance.GetUIRoutes(isContainCover: false, isContainOverlay: false);
        return popupOrModalUIs.Length == 0;
    }

    /// <summary>
    /// 로비 튜토리얼 시작
    /// </summary>
    private async UniTask StartLobbyTutorialAsync(GuideMissionInfo info)
    {
        var result = await CheckAndInitTutorialWithGuideMissionInfo(info);
        if (!result)
        {
            return;
        }

        // 로비 전용 초기 트리거 (필요 시)
        // STAY_DEFAULT_LOBBY 트리거는 튜토리얼 데이터에 정의된 대로 자동 발동됨
    }

    #endregion

    #region 아웃게임 튜토리얼


    /// <summary>
    /// 아웃게임 튜토리얼 확인 및 초기화
    /// </summary>
    private GuideMissionInfo _pendingGuideMissionInfo;

    public async UniTask<bool> CheckAndInitTutorialWithGuideMissionInfo(GuideMissionInfo info)
    {

        if (IsClearedGuideMission(info.id))
        {
            return false;
        }

        if (_pendingGuideMissionInfo != null)
        {
            return false;
        }

        var result = await CheckAndInitTutorial(info.tutorial_id);
        if (result)
        {
            _pendingGuideMissionInfo = info;  // 완료 대기

            // 첫 번째 트리거 자동 발동
            var firstTriggerType = _specTutorialDataList.First();
            HandleTutorialAction(firstTriggerType.tutorial_trigger_type, firstTriggerType.tutorial_trigger_key);
        }

        return result;
    }


    /// <summary>
    /// 아웃게임 튜토리얼 완료 여부 확인
    /// </summary>
    public bool IsClearedGuideMission(int guideMissionId)
    {
        var guideMissionBridge = new GuideMissionDataBridge();
        return guideMissionId < guideMissionBridge.GuideMissionId;
    }

    #endregion

    #region 인게임 튜토리얼

    public async UniTask<bool> CheckAndInitTutorial(int tutorialID)
    {
        _specTutorialDataList = SpecDataManager.Instance.GetTutorialDialogueList(tutorialID);
        if (_specTutorialDataList == null || _specTutorialDataList.Count == 0)
        {
            Debug.LogColor($"튜토리얼 데이터가 없습니다. tutorialID: {tutorialID}", "red");
            return false;
        }

        Debug.LogColor($"튜토리얼 초기화: {tutorialID}, 스텝 수: {_specTutorialDataList.Count}", "green");
        await CreateTutorialCanvas();
        return true;
    }

    #endregion

    #region Canvas 관리

    private async UniTask CreateTutorialCanvas()
    {
        if (_tutorialCanvasInstance != null)
        {
            return;
        }

        var handle = Addressables.InstantiateAsync(TUTORIAL_CANVAS_PREFAB_PATH);
        await handle;

        if (!handle.IsValid() || handle.Result == null)
        {
            Debug.LogError($"튜토리얼 Canvas 로드 실패: {TUTORIAL_CANVAS_PREFAB_PATH}");
            return;
        }

        _tutorialCanvasInstance = handle.Result;
        _canvas = _tutorialCanvasInstance.GetComponent<Canvas>();
        _tutorialController = _tutorialCanvasInstance.GetComponentInChildren<TutorialController>();

        if (_canvas == null || _tutorialController == null)
        {
            Debug.LogError("튜토리얼 Canvas 또는 Controller가 없습니다!");
            DestroyTutorialCanvas();
            return;
        }

        // 튜토리얼 Canvas를 최상위로 설정하여 다른 UI 위에 표시
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 9999;

        _tutorialController.Initialize(() => HandleTutorialClose());
        IsTutorialCanvasEnabled = false;
    }

    private void DestroyTutorialCanvas()
    {
        if (_tutorialCanvasInstance != null)
        {
            _tutorialController?.ClearTutorial();
            Addressables.ReleaseInstance(_tutorialCanvasInstance);
            _tutorialCanvasInstance = null;
            _canvas = null;
            _tutorialController = null;
        }
    }

    #endregion

    #region 튜토리얼 액션 처리

    public bool IsTutorialAction(TutorialTriggerType tutorialTriggerType)
    {
        if (!IsTutorial || _specTutorialDataList.Count == 0)
        {
            return false;
        }
        return _specTutorialDataList.Find(l => l.tutorial_trigger_type == tutorialTriggerType) != null;
    }

    public bool HandleTutorialAction(TutorialTriggerType tutorialTriggerType, string key, bool isLongShow = false)
    {
        if (!IsTutorial || _specTutorialDataList.Count == 0)
        {
            Debug.LogColor($"튜토리얼이 초기화되지 않았습니다. type: {tutorialTriggerType}", "red");
            return false;
        }

        if (_canvas == null || _tutorialController == null)
        {
            Debug.LogWarning("튜토리얼 Canvas가 없습니다. CheckAndInitTutorial을 먼저 호출하세요.");
            return false;
        }

        var turnTutorialList = _specTutorialDataList.FindAll(
            l => l.tutorial_trigger_type == tutorialTriggerType && l.tutorial_trigger_key == key);

        if (turnTutorialList.Count == 0)
        {
            return false;
        }

        Debug.LogColor($"튜토리얼 액션 실행: {tutorialTriggerType}, key: {key}, count: {turnTutorialList.Count}", "green");

        _specTutorialDataList.RemoveAll(
            l => l.tutorial_trigger_type == tutorialTriggerType && l.tutorial_trigger_key == key);


        IsTutorialCanvasEnabled = true;
        _tutorialController.SetTutorial(turnTutorialList, isLongShow);

        return true;
    }

    public bool HandleTutorialClose(Action action = null)
    {
        // TODO 가이드 미션 플래그 하나 올리기

        if (!IsTutorial)
        {
            return false;
        }

        if (_canvas != null)
        {
            IsTutorialCanvasEnabled = false;
        }
        action?.Invoke();

        // 인게임 전용 핸들러 처리
        TutorialActionSpawnEnemy.ResumeGameIfPaused();
        TutorialSkillReadyHandler.ResumeAndActivateSkill();
        TutorialEnemyDeadAllHandler.ResumeAndEndCombat();
        TutorialSkillReadyHandler.TryProcessDeferredSkillReady();

        if (_specTutorialDataList.Count == 0)
        {
            // 모든 다이얼로그 소모 완료 → 가이드 미션 완료 처리
            if (_pendingGuideMissionInfo != null)
            {
                var guideMissionBridge = new GuideMissionDataBridge();
                guideMissionBridge.AddAction(GuideMissionType.CLEAR_TUTORIAL, 1);
            }
            ClearTutorial();
        }

        return true;
    }

    public void ClearTutorial()
    {
        Debug.LogColor("튜토리얼 정리", "green");
        if (_canvas != null)
        {
            IsTutorialCanvasEnabled = false;
        }
        DestroyTutorialCanvas();
        _specTutorialDataList.Clear();
        _pendingGuideMissionInfo = null;
    }

    #endregion
}
