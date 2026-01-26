using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
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
    private IDisposable _guideMissionSubscription;

    public bool IsTutorialCanvasEnabled
    {
        get
        {
            return _canvas.enabled;
        }
        set
        {
            _canvas.enabled = value;
        }
    }
    private TutorialController _tutorialController;
    private GameObject _tutorialCanvasInstance;

    private List<TutorialDialogue> _specTutorialDataList = new();
    public bool HasTutorialStage => _specTutorialDataList is { Count: > 0 } && _specTutorialDataList[0].tutorial_id > 0;
    public bool IsTutorial => _canvas != null;

    protected override void OnDestroy()
    {
        _guideMissionSubscription?.Dispose();
        _guideMissionSubscription = null;
        ClearTutorial();
        base.OnDestroy();
    }

    /// <summary>
    /// 가이드 미션 변경 구독
    /// </summary>
    public void SubscribeGuideMissionChanged()
    {
#if !_SJHONG_TEST_
        var guideMissionBridge = new GuideMissionDataBridge();
        _guideMissionSubscription = guideMissionBridge.OnMissionIdChanged
            .Subscribe(OnGuideMissionIdChanged);
#endif
    }

    /// <summary>
    /// 가이드 미션 ID 변경 시 호출
    /// 튜토리얼 시작은 GuideMissionSlot의 RewardResultPopup 닫힘 콜백에서 처리
    /// </summary>
    private void OnGuideMissionIdChanged(uint newMissionId)
    {
        Debug.LogColor($"[TutorialManager] 가이드 미션 변경 감지: {newMissionId}", "cyan");
    }

    /// <summary>
    /// 아웃게임 튜토리얼 시작 시도 (Lobby 진입 또는 가이드 미션 변경 시 호출)
    /// </summary>
    public async UniTask<bool> TryStartOutgameTutorial()
    {
        var guideMissionBridge = new GuideMissionDataBridge();
        var guideMissionId = (int)guideMissionBridge.GuideMissionId;

        if (guideMissionId == 0)
        {
            Debug.LogColor("[TutorialManager] 가이드 미션이 없습니다.", "yellow");
            return false;
        }

        var specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(guideMissionId);
        if (specGuideMissionData == null)
        {
            Debug.LogColor($"[TutorialManager] 가이드 미션 스펙 데이터가 없습니다. id: {guideMissionId}", "yellow");
            return false;
        }

        if (ServerDataManager.Instance.GuideMission.IsCompleted || ServerDataManager.Instance.GuideMission.IsGoalReached)
        {
            return false;
        }

        var result = await CheckAndInitTutorialWithGuideMissionInfo(specGuideMissionData);
        if (result)
        {
            // 새 가이드 미션 시작 트리거 발동
            HandleTutorialAction(TutorialTriggerType.GUIDE_START, guideMissionId.ToString());
        }

        return result;
    }

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

        var result = await CheckAndInitTutorial(info.tutorial_id);
        if (result)
        {
            _pendingGuideMissionInfo = info;  // 완료 대기
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
        var specTutorialDataList = SpecDataManager.Instance.GetTutorialDialogueList(tutorialID);
        if (specTutorialDataList == null || specTutorialDataList.Count == 0)
        {
            Debug.LogColor($"튜토리얼 데이터가 없습니다. tutorialID: {tutorialID}", "red");
            return false;
        }

        var guideMissionId = specTutorialDataList[0].guide_mission_id;

        if (guideMissionId != ServerDataManager.Instance.GuideMission.Data.GuideMissionId)
        {
            Debug.LogColor($"튜토리얼 데이터가 없습니다. guideMissionId: {guideMissionId}", "red");
            return false;
        }

        _specTutorialDataList = new List<TutorialDialogue>(specTutorialDataList);

        Debug.LogColor($"튜토리얼 초기화: {tutorialID}, 스텝 수: {_specTutorialDataList.Count}", "green");

        foreach (var tutorial in _specTutorialDataList)
        {
            Debug.LogColor($"튜토리얼 데이터: {tutorial.tutorial_trigger_type}, key: {tutorial.tutorial_trigger_key}, type: {tutorial.tutorial_action_type}, action_key: {tutorial.tutorial_action_key}, id: {tutorial.id}", "green");
        }
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
        DontDestroyOnLoad(_tutorialCanvasInstance);
        _canvas = _tutorialCanvasInstance.GetComponent<Canvas>();
        _tutorialController = _tutorialCanvasInstance.GetComponentInChildren<TutorialController>();

        if (_canvas == null || _tutorialController == null)
        {
            Debug.LogError("튜토리얼 Canvas 또는 Controller가 없습니다!");
            DestroyTutorialCanvas();
            return;
        }

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
        if (!IsTutorial || _specTutorialDataList != null && _specTutorialDataList.Count == 0)
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

        // 매칭된 튜토리얼 중 가장 작은 seq 찾기
        int minMatchedSeq = turnTutorialList.Min(t => t.seq);

        // 해당 seq보다 작은 모든 튜토리얼 스킵 (제거)
        int skippedCount = _specTutorialDataList.RemoveAll(l => l.seq < minMatchedSeq);
        if (skippedCount > 0)
        {
            Debug.LogColor($"[Tutorial] seq {minMatchedSeq} 이전 튜토리얼 {skippedCount}개 스킵됨", "yellow");
        }

        // 매칭된 튜토리얼 제거
        _specTutorialDataList.RemoveAll(
            l => l.tutorial_trigger_type == tutorialTriggerType && l.tutorial_trigger_key == key);


        IsTutorialCanvasEnabled = true;
        _tutorialController.SetTutorialAsync(turnTutorialList, isLongShow).Forget();

        return true;
    }

    public bool HandleTutorialClose(Action action = null)
    {
        // TODO 가이드 미션 플래그 하나 올리기

        if (!IsTutorial)
        {
            return false;
        }

        // 튜토리얼 Controller 정리 (CurrentSpecTutorialList 등 초기화)
        _tutorialController?.ClearTutorial();

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

        // if (_specTutorialDataList.Count == 0)
        // {
        //     // 모든 다이얼로그 소모 완료 → 가이드 미션 완료 처리
        //     if (_pendingGuideMissionInfo != null)
        //     {
        //         var guideMissionBridge = new GuideMissionDataBridge();
        //         guideMissionBridge.AddAction(_pendingGuideMissionInfo.guide_mission_type, 1);
        //         _pendingGuideMissionInfo = null;
        //     }
        //     ClearTutorial();
        // }

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
    }

    #endregion
}
