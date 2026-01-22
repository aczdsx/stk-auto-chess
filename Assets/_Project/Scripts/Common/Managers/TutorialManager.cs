using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static TutorialConstants;

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
    private TutorialController _tutorialController;
    private GameObject _tutorialCanvasInstance;

    private List<TutorialDialogue> _specTutorialDataList = new();
    public bool HasTutorialStage => _specTutorialDataList is { Count: > 0 } && _specTutorialDataList[0].tutorial_id > 0;
    public bool IsTutorial => _canvas != null;

    // 테스트용: 게임 시작 시마다 초기화되는 메모리 딕셔너리
    private readonly Dictionary<int, bool> _outgameTutorialCompleted = new();

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
    /// 현재 진행해야 할 챕터1 튜토리얼 가져오기
    /// 모두 완료했으면 None 반환
    /// </summary>
    public Chapter1Tutorial GetCurrentChapter1Tutorial()
    {
        var current = CHAPTER1_FIRST;
        while (current != Chapter1Tutorial.None)
        {
            if (!IsOutgameTutorialCompleted((int)current))
            {
                return current;
            }
            current = GetNextChapter1Tutorial(current);
        }
        return Chapter1Tutorial.None;
    }

    /// <summary>
    /// 챕터1 튜토리얼 시퀀스 시작
    /// LobbyMain에서 호출
    /// </summary>
    /// <returns>튜토리얼이 시작되었으면 true</returns>
    public async UniTask<bool> StartChapter1TutorialSequence()
    {
        var currentTutorial = GetCurrentChapter1Tutorial();
        if (currentTutorial == Chapter1Tutorial.None)
        {
            Debug.LogColor("챕터1 튜토리얼 모두 완료", "yellow");
            return false;
        }

        // 튜토리얼 초기화
        var initialized = await CheckAndInitOutgameTutorial((int)currentTutorial);
        #if _SJHONG_TEST_
        MyDebug.MyLog($"currentTutorial : {currentTutorial}\n var initialized = await CheckAndInitOutgameTutorial((int)currentTutorial); : {initialized}");
        #endif
        if (!initialized)
        {
            return false;
        }

        // 해당 튜토리얼의 트리거 발동
        var triggerType = GetChapter1TriggerType(currentTutorial);
        #if _SJHONG_TEST_
        MyDebug.MyLog($"triggerType : {triggerType}");
        #endif
        if (triggerType != TutorialTriggerType.NONE)
        {
            #if _SJHONG_TEST_
            MyDebug.MyLog($"HandleTutorialAction(triggerType, 0);");
            #endif
            HandleTutorialAction(triggerType, "0"); // ! 외부에 넣어줘야 하는게 아닌가 싶은데.
        }

        return true;
    }

    /// <summary>
    /// 아웃게임 튜토리얼 확인 및 초기화
    /// </summary>
    public async UniTask<bool> CheckAndInitOutgameTutorial(int tutorialId)
    {
        if (!IsOutgameTutorial(tutorialId))
        {
            Debug.LogWarning($"아웃게임 튜토리얼 ID가 아닙니다. tutorialId: {tutorialId}");
            return false;
        }

        if (IsOutgameTutorialCompleted(tutorialId))
        {
            Debug.LogColor($"이미 완료된 아웃게임 튜토리얼입니다. tutorialId: {tutorialId}", "yellow");
            return false;
        }

        var result = await CheckAndInitTutorial(tutorialId);
        if (result)
        {
            SetOutgameTutorialCompleted(tutorialId);
        }

        return result;
    }

    /// <summary>
    /// 아웃게임 튜토리얼 완료 여부 확인
    /// </summary>
    public bool IsOutgameTutorialCompleted(int tutorialId)
    {
        return _outgameTutorialCompleted.TryGetValue(tutorialId, out var completed) && completed;
    }

    /// <summary>
    /// 아웃게임 튜토리얼 완료 처리
    /// </summary>
    public void SetOutgameTutorialCompleted(int tutorialId)
    {
        _outgameTutorialCompleted[tutorialId] = true;
        Debug.LogColor($"아웃게임 튜토리얼 완료 (메모리): {tutorialId}", "green");
    }

    private static string GetOutgameTutorialKey(int tutorialId) => $"OutgameTutorial_{tutorialId}";

    /// <summary>
    /// 챕터1 튜토리얼에 해당하는 TriggerType
    /// </summary>
    private static TutorialTriggerType GetChapter1TriggerType(Chapter1Tutorial tutorial)
    {
        return tutorial switch
        {
            Chapter1Tutorial.HubbleIntro => TutorialTriggerType.ENTER_ELPIS,
            Chapter1Tutorial.Observation10 => TutorialTriggerType.HUBBLE_EXPANSION_COMPLETE,
            Chapter1Tutorial.DormitoryRepair => TutorialTriggerType.SUMMON_CHARACTER,
            Chapter1Tutorial.UnitGrowth => TutorialTriggerType.BUILDING_COMPLETE,
            _ => TutorialTriggerType.NONE
        };
    }

    #endregion

    #region 인게임 튜토리얼

    /// <summary>
    /// 스테이지 진입 시 튜토리얼 확인 및 초기화 (인게임용)
    /// </summary>
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

        _tutorialController.Initialize(() => HandleTutorialClose());
        _canvas.enabled = false;
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

        turnTutorialList.ForEach(e => Debug.Log(JsonConvert.SerializeObject(e)));

        if (turnTutorialList.Count == 0)
        {
            return false;
        }

        Debug.LogColor($"튜토리얼 액션 실행: {tutorialTriggerType}, key: {key}, count: {turnTutorialList.Count}", "green");

        _specTutorialDataList.RemoveAll(
            l => l.tutorial_trigger_type == tutorialTriggerType && l.tutorial_trigger_key == key);


        _canvas.enabled = true;
        _tutorialController.SetTutorial(turnTutorialList, isLongShow);

        return true;
    }

    public bool HandleTutorialClose(Action action = null)
    {
        if (!IsTutorial)
        {
            return false;
        }

        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
        action?.Invoke();

        // 인게임 전용 핸들러 처리
        TutorialActionSpawnEnemy.ResumeGameIfPaused();
        TutorialSkillReadyHandler.ResumeAndActivateSkill();
        TutorialEnemyDeadAllHandler.ResumeAndEndCombat();
        TutorialSkillReadyHandler.TryProcessDeferredSkillReady();

        if (_specTutorialDataList.Count == 0)
        {
            ClearTutorial();
        }

        return true;
    }

    public void ClearTutorial()
    {
        Debug.LogColor("튜토리얼 정리", "green");
        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
        DestroyTutorialCanvas();
        _specTutorialDataList.Clear();
    }

    #endregion
}
