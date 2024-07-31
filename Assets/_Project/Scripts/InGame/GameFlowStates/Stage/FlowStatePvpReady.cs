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
        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(8.5f, new Vector3(0, 0f, -10), 1.0f).Forget();
        
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        
        // 상대 덱 설정
        foreach (var pvpCharacter in _pvpBattleDeckList.PvpDeckList.PvpCharacterDecks)
        {
            var statData = new CharacterStatData(pvpCharacter.Id, pvpCharacter.Lv, pvpCharacter.EffectCodeInfo, _pvpBattleDeckList.PvpDeckList.EffectCodeInfos);
            
            int2 coordinate = new int2(pvpCharacter.PosX, pvpCharacter.PosY);
            var newPos = InGameObjectManager.Instance.GetInOppositePosition(coordinate);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, newPos, AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }
        
        // 장애물 설치
        foreach (var obstacleDeck in _pvpBattleDeckList.PvpDeckList.PvpObstacleDecks)
        {
            var specObstacleDataList = SpecDataManager.Instance.GetSpecSynergyList(obstacleDeck.Id);
            if (specObstacleDataList.Count > 0)
            {
                if (specObstacleDataList[0].obstacle_type == ObstacleType.WALL)
                {
                    var newPos =
                        InGameObjectManager.Instance.GetInOppositePosition(new int2(obstacleDeck.PosX,
                            obstacleDeck.PosY));
                    var grid = InGameObjectManager.Instance.GetInGameTile(newPos);
                    addCharacterTasks.Add(
                        InGameObjectManager.Instance.AddObstacleToField(grid.View.ID, obstacleDeck.Id,
                            AllianceType.Wall));
                }

                if (specObstacleDataList[0].obstacle_type == ObstacleType.NEUTRAL_WALL)
                {
                    var statData = new CharacterStatData(specObstacleDataList[0].obstacle_id, 1, 1, 1);

                    var tile = InGameObjectManager.Instance.GetInGameTile(specObstacleDataList[0].obstacle_id);
                    var newPos =
                        InGameObjectManager.Instance.GetInOppositePosition(new int2(tile.X, tile.Y));

                    addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, newPos,
                        AllianceType.Neutral,
                        typeof(CharacterStateReady), false, HpBarType.None));
                }
            } 
        }
        
        // 내 덱 설정
        var currentDetailDeck = UserDataManager.Instance.GetCurrentPVPDetailProfileData(false);
        var battleDeckList = new List<UserCharacterBattleDeck>();
        foreach (var character in currentDetailDeck.PvpDeckList.PvpCharacterDecks)
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
        
        // [TODO] 내 방어덱 장애물 확인 후 설치 (우선 내 장애물 없음.)
        // List<UserPVPObstacleBattleDeck> datas = new();
        // foreach (var obstacleDeck in datas)
        // {
        //     var specObstacleDataList = SpecDataManager.Instance.GetSpecSynergyList(obstacleDeck.Id);
        //     if (specObstacleDataList.Count > 0)
        //     {
        //         if (specObstacleDataList[0].obstacle_type == ObstacleType.WALL)
        //         {
        //             var grid = InGameObjectManager.Instance.GetInGameTile(
        //                 new int2(obstacleDeck.PosX, obstacleDeck.PosY));
        //             addCharacterTasks.Add(
        //                 InGameObjectManager.Instance.AddObstacleToField(grid.View.ID, obstacleDeck.Id,
        //                     AllianceType.Wall));
        //         }
        //
        //         if (specObstacleDataList[0].obstacle_type == ObstacleType.NEUTRAL_WALL)
        //         {
        //             var statData = new CharacterStatData(specObstacleDataList[0].obstacle_id, 1, 1, 1);
        //
        //             var tile = InGameObjectManager.Instance.GetInGameTile(specObstacleDataList[0].obstacle_id);
        //             int2 coordinate = new int2(tile.X, tile.Y);
        //
        //             addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate,
        //                 AllianceType.Neutral,
        //                 typeof(CharacterStateReady), false, HpBarType.None));
        //         }
        //     } 
        // }

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
