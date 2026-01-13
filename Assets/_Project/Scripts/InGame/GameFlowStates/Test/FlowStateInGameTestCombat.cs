using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateInGameTestCombat : StateCombatBase
{
    private InGameTestConfig _testConfig;
    private List<CharacterController> _characters;
    private bool _isEndCombat;
    private bool _isWin;
    private float _remainingTime;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _testConfig = data as InGameTestConfig;
    }

    public override void StateInit(object target)
    {
        _characters = ListPool<CharacterController>.Get();
        _isEndCombat = false;
        _isWin = false;

        InGameSynergyManager.Instance.ClearSynergyFx();

        // UI가 있으면 초기화
        var inGameMain = InGameMain.GetInGameMain();
        if (inGameMain != null)
        {
            inGameMain.SetActiveObjectMover(false);
            inGameMain.InitCombatStateUI();
        }

        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        var inGameStage = InGameObjectManager.Instance.InGameStage;
        if (inGameStage != null)
        {
            inGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);
        }

        var inGameCamera = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera);
        if (inGameCamera != null)
        {
            inGameCamera.SetCameraSize(6.0f, new Vector3(0, 3.0f, -10), 1.0f).Forget();
        }

        InGameObjectManager.Instance.ClearTargetLine();

        // 전투 시간 초기화
        _remainingTime = _testConfig?.BattleTimeLimit ?? 60f;
    }

    public override void StateStart()
    {
        // 시너지 아이템 체크
        InGameSynergyManager.Instance.CheckAndHandleNotAppliedItemsBeforeCombat();

        // Player 캐릭터 전투 시작 콜백
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        // Enemy 캐릭터 전투 시작 콜백
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        // 팀 효과 코드 콜백
        {
            var effectCodes = InGameManager.Instance.TeamEcc.GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        StartCombatAsync().Forget();
    }

    private async UniTask StartCombatAsync()
    {
        // 서버 통신 생략 - 테스트 모드이므로 바로 전투 시작
        Debug.LogColor("[Test] 전투 시작 (서버 통신 생략)");

        // 전투 시작 전 1초 대기
        await UniTask.Delay(1000);

        // 모든 캐릭터 락 해제
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, _characters);
        foreach (CharacterController charac in _characters)
        {
            charac.AddNextState<CharacterStateIdle>();
            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, _characters);
        foreach (CharacterController charac in _characters)
        {
            charac.AddNextState<CharacterStateIdle>();
            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Neutral, _characters);
        foreach (CharacterController charac in _characters)
        {
            charac.AddNextState<CharacterStateIdle>();
        }
    }

    public override void StateRunning(float dt)
    {
        if (_isEndCombat)
            return;

        // 시간 감소
        _remainingTime -= dt;

        // 플레이어 전멸 체크
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, _characters);
        if (_characters.Count == 0)
        {
            _isEndCombat = true;
            _isWin = false;
            Debug.LogColor("[Test] 플레이어 전멸!", "red");
        }

        // 적 전멸 체크
        if (!_isEndCombat)
        {
            InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, _characters);
            if (_characters.Count == 0)
            {
                _isEndCombat = true;
                _isWin = true;
                Debug.LogColor("[Test] 적 전멸!", "green");
            }
        }

        // 시간 초과 체크
        if (!_isEndCombat && _remainingTime <= 0)
        {
            Debug.LogColor("[Test] 시간 초과!", "red");
            _isEndCombat = true;
            _isWin = false;
        }

        if (_isEndCombat)
        {
            InGameManager.Instance.IsInGameCombat = false;
            HandleCombatEnd().Forget();
        }
    }

    public override void StateEnd(bool isForced)
    {
        // 시너지 효과 코드 제거
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.RemoveSynergyEffectCodeALL();
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.RemoveSynergyEffectCodeALL();
        }

        ListPool<CharacterController>.Release(_characters);
        _characters = null;
    }

    private async UniTask HandleCombatEnd()
    {
        if (_isWin)
        {
            Debug.LogColor("[Test] 승리!", "green");
        }
        else
        {
            Debug.LogColor("[Test] 패배!", "red");
        }

        // 슬로우 모션 효과
        InGameMainFlowManager.Instance.SetPlaySpeed(0.4f);
        await UniTask.Delay(1500);
        InGameMainFlowManager.Instance.SetInGameSpeed(false);

        // 재시작 대기
        float restartDelay = _testConfig?.RestartDelay ?? 2f;
        Debug.LogColor($"[Test] {restartDelay}초 후 재시작...");
        await UniTask.Delay((int)(restartDelay * 1000));

        // 자동 재시작
        RestartTest();
    }

    private void RestartTest()
    {
        Debug.LogColor("[Test] 테스트 재시작");

        // 기존 캐릭터 정리
        InGameObjectManager.Instance.ClearAllCharactersInField(AllianceType.Player);
        InGameObjectManager.Instance.ClearAllCharactersInField(AllianceType.Enemy);
        InGameObjectManager.Instance.ClearAllCharactersInField(AllianceType.Neutral);

        // 시너지/이펙트 정리
        InGameSynergyManager.Instance.ClearSynergyFx();

        // 상태만 전환 (Initialize 재호출 안 함)
        InGameMainFlowManager.Instance.AddNextState<FlowStateInGameTestReady>((_testConfig));
    }
}
