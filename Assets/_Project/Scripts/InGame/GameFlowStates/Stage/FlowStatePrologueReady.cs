using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStatePrologueReady : StateReadyBase
{
    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        //[TODO] 프롤로그 사운드 추가 필요
        // SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM),
        //     $"snd_bgm_chapter{_specStage.chapter_id - 1}"));
        InGameMain.GetInGameMain().SetVignette(0);
    }

    public override async void StateInit(object target)
    {
        var addCharacterTasks = new List<UniTask<CharacterController>>();

        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPosition(new Vector3(-20, 2.5f, -10));
        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(6.0f);
        // ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(8.5f,new Vector3(-1f, 0f, -10), 1.0f).Forget();

        SpawnPrologueCharacters(addCharacterTasks);  

        await UniTask.WhenAll(addCharacterTasks);

        InGameMainFlowManager.Instance.AddNextState<FlowStatePrologueCombat>();
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

    // 프롤로그 시나리오 캐릭터 소환
    private void SpawnPrologueCharacters(List<UniTask<CharacterController>> addCharacterTasks)
    {
        // 프롤로그 시나리오 캐릭터 ID 및 위치 정의
        // 클레이 (130101), 유니 (130201), 필리아 (130301), 아트레시아 (130401)
        // 마리에 (130501)는 나중에 합류하므로 초기에는 소환하지 않음

        // 기본 레벨 설정 (필요시 조정)
        int prologueCharacterLevel = 1;

        // 프롤로그 플레이어 캐릭터 위치 (플레이어 진영 앞쪽)
        var prologueCharacterPositions = new Dictionary<int, int2>
        {
            { 3404, new int2(2, 2) }, 
            { 2102, new int2(1, 2) }, // 유니
            { 2401, new int2(3, 2) }, // 필리아
            { 3401, new int2(2, 1) }  // 아트레시아 (중앙 앞)
        };

        // 플레이어 캐릭터 소환
        foreach (var kvp in prologueCharacterPositions)
        {
            int characterId = kvp.Key;
            int2 position = kvp.Value;

            Debug.LogColor($"프롤로그 캐릭터 추가 : {characterId} at ({position.x}, {position.y})");

            var characterStat = new CharacterStatData(characterId, prologueCharacterLevel,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, position,
                AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        //라플라스 마녀 소환 
        //[TODO] 라플라스 마녀의 실제 캐릭터 ID로 변경 필요
        int laplaceWitchId = 8501; // 임시 ID (Trial 던전 보스)
        int2 witchPosition = new int2(2, 10);

        Debug.LogColor($"라플라스 마녀 추가 : {laplaceWitchId} at ({witchPosition.x}, {witchPosition.y})");

        var witchStat = new CharacterStatData(laplaceWitchId, 5, 0.0f, 100.0f);

        addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(witchStat, witchPosition,
            AllianceType.Enemy,
            typeof(CharacterStateReady), true, HpBarType.Synergy));
    }
}