using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Cookapps.Stkauto.V1;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStatePvpReady : StateReadyBase
{
    private UserPVPBattleDeckList _pvpBattleDeckList;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _pvpBattleDeckList = data as UserPVPBattleDeckList;

        //[TODO] PVP 사운드, 비네트
        SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter0);
        InGameMain.GetInGameMain().SetVignette(0);
    }

    public override async void StateInit(object target)
    {;
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        
        foreach (var pvpCharacter in _pvpBattleDeckList.PvpCharacterDecks)
        {
            var statData = new CharacterStatData(pvpCharacter.Id, pvpCharacter.Lv, pvpCharacter.EffectCodeInfo, _pvpBattleDeckList.EffectCodeInfos);
            
            int2 coordinate = new int2(pvpCharacter.PosX, pvpCharacter.PosY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }
        
        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(8.5f, new Vector3(0, 0f, -10), 1.0f).Forget();

        var battleDeckList = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.PVP);

        foreach (var character in battleDeckList)
        {
            var characterData = UserDataManager.Instance.GetUserCharacter(character.CharacterId);
            Debug.LogColor($"기존 배치 캐릭터 추가 : {character.CharacterId}");
            var characterStat = new CharacterStatData(characterData.CharacterId, characterData.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            int x = character.PositionTileX;
            int y = character.PositionTileY;

            int2 coordinate = new int2(x, y);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        await UniTask.WhenAll(addCharacterTasks);
        InGameMain.GetInGameMain().InitReadyStateUI(battleDeckList);
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
