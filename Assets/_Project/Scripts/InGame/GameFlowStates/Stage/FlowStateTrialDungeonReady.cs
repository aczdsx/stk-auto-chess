using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateTrialDungeonReady : StateReadyBase
{
    private DungeonBabelInfo _specDungeonTrial;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specDungeonTrial = data as DungeonBabelInfo;

        //[TODO] 시련 던전 사운드, 비네트
        SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter0);
        InGameMain.GetInGameMain().SetVignette(0);
    }

    public override async void StateInit(object target)
    {
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<DungeonBabelMonster> monsters =
            SpecDataManager.Instance.GetSpecDungeonMonsterDataList(_specDungeonTrial.dungeon_type, _specDungeonTrial.dungeon_id);

        float monsterMultipleHp = 1.0f;
        foreach (var monster in monsters)
        {
            monsterMultipleHp = monster.multiple_hp;
            Debug.LogColor($"monster 추가 : {monster.monster_id}");
            var statData = new CharacterStatData(monster.monster_id, monster.monster_lv, monster.multiple_atk,
                monster.multiple_hp);
        
            string[] coordinates = monster.coordinate.Split(',');
            int x = int.Parse(coordinates[0]);
            int y = int.Parse(coordinates[1]);
            int2 coordinate = new int2(x, y);
        
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        if (InGameManager.Instance.SpecDungeonTrial.dungeon_map_id == 1)
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(8.5f,
                new Vector3(-1f, 0f, -10), 1.0f).Forget();
        else
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(8.5f,
                new Vector3(0f, 1.5f, -10), 1.0f).Forget();

        var deckData = ServerDataManager.Instance.Deck.GetDeck(InGameResourceHolder.InGameType);
        var battleDeckList = deckData != null
            ? new List<DeckCharacterPlacement>(deckData.CharacterPlacements)
            : new List<DeckCharacterPlacement>();
        List<ObfuscatorInt> tileIDList = _specDungeonTrial.obstacle_grid_id.Select(x => new ObfuscatorInt(x)).ToList();

        // 장애물 타일과 겹치는 캐릭터 제거
        for (int i = battleDeckList.Count - 1; i >= 0; i--)
        {
            var placement = battleDeckList[i];
            var tile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(placement.GridX, placement.GridY));
            if (tile != null && tileIDList.Exists(t => t.Value == tile.View.ID))
            {
                battleDeckList.RemoveAt(i);
            }
        }

        foreach (var placement in battleDeckList)
        {
            var characterData = ServerDataManager.Instance.Character.GetCharacter(placement.CharacterId);
            if (characterData == null) continue;

            Debug.LogColor($"기존 배치 캐릭터 추가 : {characterData.CharacterId}");
            var characterStat = new CharacterStatData((int)characterData.CharacterId, (int)characterData.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            int x = placement.GridX;
            int y = placement.GridY;

            int2 coordinate = new int2(x, y);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        await UniTask.WhenAll(addCharacterTasks);
        InGameMain.GetInGameMain().InitReadyStateUI(battleDeckList);

        InGameManager.Instance.OnFlowStateStageReadyStart();
        
        StartDrawingLinesAsync(2.0f).Forget();

        // if (_specDungeonTrial.order == 2)
        // {
        //     InGameMain.GetInGameMain().SetAlertBottomCharacter(140601);
        // }
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
