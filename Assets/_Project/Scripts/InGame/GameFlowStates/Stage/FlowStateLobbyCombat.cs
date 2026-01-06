using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;
using Random = Unity.Mathematics.Random;

public class FlowStateLobbyCombat : StateCombatBase
{
    private List<CharacterController> _playerCharacters;
    private List<CharacterController> _enemyCharacters;

    private int _maxEnemySpawnCount = 5;

    public override void StateInit(object target)
    {
        _playerCharacters = ListPool<CharacterController>.Get();
        _enemyCharacters = ListPool<CharacterController>.Get();

        _maxEnemySpawnCount = SpecDataManager.Instance.GetGameConfig<int>("max_idle_battle_monster_count");
    }

    public override void StateStart()
    {
        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        var userCharacters = UserDataManager.Instance.GetAllUserCharacterList();
        
        // seq 기준으로 내림차순 정렬
        userCharacters.Sort((a, b) =>
        {
            var aData = SpecDataManager.Instance.GetCharacterData(a.CharacterId);
            var bData = SpecDataManager.Instance.GetCharacterData(b.CharacterId);
            
            // GetAllUserCharacterList()에서 이미 필터링되지만 안전을 위해 null 체크
            if (aData == null && bData == null) return 0;
            if (aData == null) return 1;
            if (bData == null) return -1;
            
            return bData.seq.CompareTo(aData.seq); // 내림차순
        });
            
        int count = 0;
        foreach (var character in userCharacters)
        {
            var characterStat = new CharacterStatData(character.CharacterId, character.Level, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());
            if (characterStat.Spec == null)
            {
                continue;
            }

            InGameTile ingameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
            int2 coordinate = new int2(ingameTile.X, ingameTile.Y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate, AllianceType.Player,
                typeof(CharacterStateIdle), false));
            count++;
            if (count >= 5)
                break;
            await UniTask.Delay(210);
        }

        // 전투 시작 후 1초는 대기
        await UniTask.Delay(700);
    }

    private float elapsedTime = 0f;
    private float interval = 1f;

    public override void StateRunning(float dt)
    {
        elapsedTime += dt;

        if (elapsedTime >= interval)
        {
            elapsedTime = 0f;


            SpawnEnemy().Forget();

            interval = UnityEngine.Random.Range(1f, 4f);
        }
    }

    private async UniTask SpawnEnemy()
    {
        if (InGameObjectManager.Instance.EnemiesInPlaygroundForUpdate.Count >= _maxEnemySpawnCount) return;

        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<StageMonster> monsters =
            SpecDataManager.Instance.GetStageMonsterList(InGameManager.Instance.SpecStage.chapter_id, 1,
                DifficultyType.NORMAL);

        System.Random random = new System.Random();
        StageMonster randomMonster = monsters[random.Next(monsters.Count)];

        if (randomMonster != null)
        {
            Debug.LogColor($"monster 추가 : {randomMonster.monster_id}");
            var statData = new CharacterStatData(randomMonster.monster_id, randomMonster.monster_lv,
                0, randomMonster.multiple_hp);

            InGameTile ingameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Enemy);
            if (ingameTile != null)
            {
                int2 coordinate = new int2(ingameTile.X, ingameTile.Y);

                addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Enemy,
                    typeof(CharacterStateIdle), false));
            }
        }
    }

    public override void StateEnd(bool isForced)
    {
        ListPool<CharacterController>.Release(_playerCharacters);
        ListPool<CharacterController>.Release(_enemyCharacters);
    }
}
