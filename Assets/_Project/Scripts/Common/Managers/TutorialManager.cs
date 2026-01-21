using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 씬별 튜토리얼 관리자. 
/// - CheckAndInitTutorial: 씬 진입 시 튜토리얼 존재 여부 확인 및 초기화
/// - HandleTutorialAction: 인게임 트리거(GAME_START, CHARACTER_PLACEMENT 등)로 튜토리얼 액션 처리
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

    protected override void Awake()
    {
        base.Awake();
        // 씬별로 초기화되므로 DontDestroyOnLoad 하지 않음
        // 부모의 Awake에서 DontDestroyOnLoad가 호출되지만, 씬 전환 시 정리됨
    }

    protected override void OnDestroy()
    {
        // 씬 전환 시 정리
        ClearTutorial();
        base.OnDestroy();
    }

    /// <summary>
    /// 스테이지 진입 시 튜토리얼 확인 및 초기화
    /// </summary>
    public async UniTask<bool> CheckAndInitTutorial(int tutorialID)
    {
        _specTutorialDataList = SpecDataManager.Instance.GetTutorialDialogueList(tutorialID);
        if (_specTutorialDataList == null || _specTutorialDataList.Count == 0)
        {
            Debug.LogColor($"튜토리얼을 진행하는 스테이지가 아닙니다. tutorialID : {tutorialID}", "red");
            return false;
        }

        // 튜토리얼 Canvas 동적 생성
        Debug.LogColor($"튜토리얼 초기화: {tutorialID} {_specTutorialDataList.Count}", "green");
        foreach (var tutorialData in _specTutorialDataList)
        {
            Debug.LogColor($"튜토리얼 데이터: id : {tutorialData.id}, {tutorialData.tutorial_trigger_type} {tutorialData.tutorial_trigger_key}, {tutorialData.tutorial_action_type} {tutorialData.tutorial_action_key}", "green");
        }
        await CreateTutorialCanvas();
        return true;
    }

    /// <summary>
    /// 튜토리얼 Canvas 동적 생성 (TutorialController 포함)
    /// </summary>
    private async UniTask CreateTutorialCanvas()
    {
        if (_tutorialCanvasInstance != null)
        {
            return; // 이미 생성됨
        }

        var handle = Addressables.InstantiateAsync(TUTORIAL_CANVAS_PREFAB_PATH);
        await handle;

        if (!handle.IsValid() || handle.Result == null)
        {
            Debug.LogError($"튜토리얼 TutorialCanvas prefab 로드 실패: {TUTORIAL_CANVAS_PREFAB_PATH}");
            return;
        }

        _tutorialCanvasInstance = handle.Result;
        _canvas = _tutorialCanvasInstance.GetComponent<Canvas>();
        _tutorialController = _tutorialCanvasInstance.GetComponentInChildren<TutorialController>();

        if (_canvas == null)
        {
            Debug.LogError("튜토리얼 TutorialCanvas prefab에 Canvas가 없습니다!");
            DestroyTutorialCanvas();
            return;
        }

        if (_tutorialController == null)
        {
            Debug.LogError("튜토리얼 TutorialCanvas prefab에 TutorialController가 없습니다!");
            DestroyTutorialCanvas();
            return;
        }

        // Controller 초기화: Manager가 Controller에 콜백 주입
        _tutorialController.Initialize(() => HandleTutorialClose());

        // 초기에는 비활성화 상태로 생성
        _canvas.enabled = false;
    }

    /// <summary>
    /// 튜토리얼 Canvas 제거 (TutorialController 포함)
    /// </summary>
    private void DestroyTutorialCanvas()
    {
        Debug.LogColor("튜토리얼 Canvas 제거", "green");
        if (_tutorialCanvasInstance != null)
        {
            // TutorialController 정리 - 가이드가 다 비었을 때 호출됨
            if (_tutorialController != null)
            {
                _tutorialController.ClearTutorial();
            }

            // Addressables로 생성한 경우 Release
            Addressables.ReleaseInstance(_tutorialCanvasInstance);
            _tutorialCanvasInstance = null;
            _canvas = null;
            _tutorialController = null;
        }
    }

    public bool IsTutorialAction(TutorialTriggerType tutorialTriggerType)
    {
        if (!IsTutorial)
        {
            return false;
        }

        if (_specTutorialDataList.Count == 0)
        {
            Debug.LogColor($"튜토리얼을 진행하는 스테이지가 아닙니다. tutorialTriggerType : {tutorialTriggerType}", "red");
            return false;
        }

        return _specTutorialDataList.Find(l => l.tutorial_trigger_type == tutorialTriggerType) != null;
    }

    public bool HandleTutorialAction(TutorialTriggerType tutorialTriggerType, string key, bool isLongShow = false)
    {
        if (!IsTutorial)
        {
            Debug.LogColor($"튜토리얼을 진행하는 스테이지가 아닙니다. tutorialTriggerType : {tutorialTriggerType} key : {key}", "red");
            return false;
        }

        if (_specTutorialDataList.Count == 0)
        {
            Debug.LogColor($"튜토리얼을 진행하는 스테이지가 아닙니다. tutorialTriggerType : {tutorialTriggerType} key : {key}", "red");
            return false;
        }

        // Canvas가 없으면 생성 시도
        if (_canvas == null || _tutorialController == null)
        {
            Debug.LogWarning("튜토리얼 TutorialCanvas가 생성되지 않았습니다. CheckAndInitTutorial을 먼저 호출하세요.");
            return false;
        }

        List<TutorialDialogue> turnTutorialList =
            _specTutorialDataList.FindAll(l => l.tutorial_trigger_type == tutorialTriggerType && l.tutorial_trigger_key == key);

        if (turnTutorialList.Count == 0)
        {
            return false;
        }
        else
        {
            if (_specTutorialDataList[0].tutorial_trigger_type == tutorialTriggerType)
            {
                Debug.LogColor($"튜토리얼 타입 체크 통과: {tutorialTriggerType} {key} {turnTutorialList.Count}", "green");
            }
        }
        Debug.LogColor($"튜토리얼 처리: {tutorialTriggerType} {key} {turnTutorialList.Count}", "green");

        // 진행한 튜토리얼을 리스트에서 제거
        _specTutorialDataList.RemoveAll(l => l.tutorial_trigger_type == tutorialTriggerType && l.tutorial_trigger_key == key);

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

        // SPAWN_ENEMY로 인해 게임이 일시 정지된 경우 재생
        TutorialActionSpawnEnemy.ResumeGameIfPaused();

        // SKILL_READY 튜토리얼 완료 후 대기 스킬 발동 및 게임 재개
        TutorialSkillReadyHandler.ResumeAndActivateSkill();

        // ENEMY_DEAD_ALL 튜토리얼 완료 후 전투 종료 처리
        TutorialEnemyDeadAllHandler.ResumeAndEndCombat();

        // CHARACTER_DEAD 완료 후 보류된 SKILL_READY 튜토리얼 처리 시도
        TutorialSkillReadyHandler.TryProcessDeferredSkillReady();

        // 모든 튜토리얼이 완료되었는지 확인 (리스트가 비어있으면 완료)
        if (_specTutorialDataList.Count == 0)
        {
            ClearTutorial();
        }

        return true;
    }

    /// <summary>
    /// 모든 튜토리얼 완료 시 또는 스테이지 종료 시 튜토리얼 Canvas 제거
    /// </summary>
    public void ClearTutorial()
    {
        Debug.LogColor("모든 튜토리얼 완료 시 또는 스테이지 종료 시 튜토리얼 Canvas 제거", "green");
        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
        DestroyTutorialCanvas();
        _specTutorialDataList.Clear();
    }
}
