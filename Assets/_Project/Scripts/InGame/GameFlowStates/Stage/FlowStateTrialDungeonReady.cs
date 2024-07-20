using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateTrialDungeonReady : StateReadyBase
{
    private SpecDungeonTrial _specDungeonTrial;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specDungeonTrial = data as SpecDungeonTrial;

        //[TODO] 시련 던전 사운드, 비네트
        SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter0);
        InGameMain.GetInGameMain().SetVignette(0);
    }

    public override async void StateInit(object target)
    {;
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<SpecDungeonMonster> monsters =
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

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(8.5f, new Vector3(0, 0f, -10), 1.0f).Forget();

        var battleDeckList = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.TRIAL);
        List<ObfuscatorInt> tileIDList = _specDungeonTrial.obstacle_grid_id.ToList();

        battleDeckList.RemoveAll(l =>
        {
            return tileIDList.Exists(t =>
                t.Value == InGameObjectManager.Instance.InGameGrid.GetTile(new int2(l.PositionTileX, l.PositionTileY))
                    .View
                    .ID);
        });

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
