using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateInGameTestReady : StateReadyBase
{
    private InGameTestConfig _testConfig;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _testConfig = data as InGameTestConfig;

        if (_testConfig == null)
        {
            Debug.LogError("FlowStateInGameTestReady: TestConfig is null!");
        }
    }

    public override async void StateInit(object target)
    {
        // 디버그 UI 생성
        InGameTestDebugUI.Create();

        var addCharacterTasks = new List<UniTask<CharacterController>>();

        // 카메라 설정
        var inGameCamera = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera);
        if (inGameCamera != null)
        {
            inGameCamera.SetCameraSize(_testConfig.CameraSize, _testConfig.CameraPosition, 1.0f).Forget();
        }

        // 적 캐릭터 배치
        foreach (var enemy in _testConfig.EnemyCharacters)
        {
            if (enemy.CharacterId <= 0) continue;

            Debug.LogColor($"[Test] 적 추가 : {enemy.CharacterId} at ({enemy.GridX}, {enemy.GridY})", "yellow");
            var statData = new CharacterStatData(
                enemy.CharacterId,
                enemy.Level,
                enemy.MultipleAtk,
                enemy.MultipleHp
            );

            int2 coordinate = new int2(enemy.GridX, enemy.GridY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady),
                true,
                HpBarType.Synergy | HpBarType.HpBar
            ));
        }

        // 내 캐릭터 배치
        foreach (var player in _testConfig.PlayerCharacters)
        {
            if (player.CharacterId <= 0) continue;

            Debug.LogColor($"[Test] 플레이어 추가 : {player.CharacterId} at ({player.GridX}, {player.GridY})", "cyan");
            var statData = new CharacterStatData(
                player.CharacterId,
                player.Level,
                player.MultipleAtk,
                player.MultipleHp
            );

            int2 coordinate = new int2(player.GridX, player.GridY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady),
                true,
                HpBarType.Synergy | HpBarType.HpBar
            ));
        }

        await UniTask.WhenAll(addCharacterTasks);

        // UI가 있으면 초기화
        var inGameMain = InGameMain.GetInGameMain();
        if (inGameMain != null)
        {
            inGameMain.InitReadyStateUI(new List<Tech.Hive.V1.DeckCharacterPlacement>());
        }

        StartDrawingLinesAsync(2.0f).Forget();
    }

    private async UniTaskVoid AutoStartCombatAsync()
    {
        Debug.LogColor("[Test] 1초 후 전투 시작...", "cyan");
        // 1초 대기 후 자동 전투 시작
        await UniTask.Delay(1000);
        InGameMainFlowManager.Instance.AddNextState<FlowStateInGameTestCombat>((_testConfig));
    }

    public override void StateStart()
    {
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
