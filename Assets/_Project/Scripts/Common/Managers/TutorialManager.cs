using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
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

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        ClearTutorial();
        base.OnDestroy();
    }

    #region 아웃게임 튜토리얼


    /// <summary>
    /// 아웃게임 튜토리얼 확인 및 초기화
    /// </summary>
    private GuideMissionInfo _pendingGuideMissionInfo;

    public async UniTask<bool> CheckAndInitTutorialWithGuideMissionInfo(GuideMissionInfo info)
    {
        if(true) return false;
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
