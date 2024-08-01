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

public class FlowStatePvpDefenseReady : StateReadyBase
{
    private UserPVPBattleDetailData _pvpBattleDeckList;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _pvpBattleDeckList = data as UserPVPBattleDetailData;

        //[TODO] PVP 사운드, 비네트
        SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter0);
        InGameMain.GetInGameMain().SetVignette(0);
    }

    public override async void StateInit(object target)
    {;
        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(7.0f, new Vector3(-5, 2.5f, -10), 1.0f).Forget();
        
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        var battleDeckList = new List<UserCharacterBattleDeck>();
        
        foreach (var character in _pvpBattleDeckList.PvpDeckList.PvpCharacterDecks)
        {
            UserCharacterBattleDeck battleDeck = new UserCharacterBattleDeck();
            battleDeck.CharacterId = character.Id;
            battleDeck.PositionTileX = character.PosX;
            battleDeck.PositionTileY = character.PosY;
            battleDeckList.Add(battleDeck);
            
            var characterData = UserDataManager.Instance.GetUserCharacter(character.Id);
            Debug.LogColor($"기존 배치 캐릭터 추가 : {character.Id}");
            var characterStat = new CharacterStatData(characterData.CharacterId, characterData.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            int x = character.PosX;
            int y = character.PosY;

            int2 coordinate = new int2(x, y);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }
        
        foreach (var obstacleDeck in _pvpBattleDeckList.PvpDeckList.PvpObstacleDecks)
        {
            var specObstacleDataList = SpecDataManager.Instance.GetSpecObstacleList(obstacleDeck.Id);
            if (specObstacleDataList.Count > 0)
            {
                if (specObstacleDataList[0].obstacle_type == ObstacleType.WALL)
                {
                    var grid = InGameObjectManager.Instance.GetInGameTile(
                        new int2(obstacleDeck.PosX, obstacleDeck.PosY));
                    addCharacterTasks.Add(
                        InGameObjectManager.Instance.AddNonStatObstacleToField(grid.View.ID, obstacleDeck.Id,
                            AllianceType.Wall));
                }

                if (specObstacleDataList[0].obstacle_type == ObstacleType.NEUTRAL_WALL)
                {
                    var statData = new CharacterStatData(specObstacleDataList[0].obstacle_id, 1, 1, 1);

                    var tile = InGameObjectManager.Instance.GetInGameTile(specObstacleDataList[0].obstacle_id);
                    int2 coordinate = new int2(tile.X, tile.Y);

                    addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate,
                        AllianceType.Neutral,
                        typeof(CharacterStateReady), false, HpBarType.None));
                }
            } 
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
