using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;
using Random = UnityEngine.Random;

public class FlowStateLobbyCombat : StateCombatBase
{
    private List<CharacterController> _playerCharacters;
    private List<CharacterController> _enemyCharacters;

    private int _maxEnemySpawnCount = 5;

    // 카메라 설정 값
    private const float _initialOrthoSize = 5f;
    private readonly Vector3 _initialCameraPosition = new Vector3(-5.11f, 0.81f, -10f);
    private readonly Vector3 _targetCameraPosition = new Vector3(0, 2.0f, -10);
    private const float _targetOrthoSize = 7.5f;
    private const float _cameraTweenDuration = 1.0f;

    private bool _canSpawnEnemy;
    private Camera _mainCamera;
    private Camera _characterCamera;

    private MotionHandle _cameraMoveTween;
    private MotionHandle _mainCameraSizeTween;
    private MotionHandle _characterCameraSizeTween;

    public override void StateInit(object target)
    {
        _playerCharacters = ListPool<CharacterController>.Get();
        _enemyCharacters = ListPool<CharacterController>.Get();
        _canSpawnEnemy = false;

        _maxEnemySpawnCount = SpecDataManager.Instance.GetGameConfig<int>("max_idle_battle_monster_count");

        // 카메라 참조 캐싱
        _mainCamera = MainCameraHolder.MainCamera;
        _characterCamera = ObjectRegistry.GetObject<RegisteredObject>(RegistryKey.CharacterCamera).GetComponent<Camera>();

        // 초기 카메라 설정
        _mainCamera.transform.position = _initialCameraPosition;
        _mainCamera.transform.rotation = Quaternion.Euler(34f, 45f, 0f);
        _mainCamera.orthographicSize = _initialOrthoSize;
        _characterCamera.orthographicSize = _initialOrthoSize;
    }

    public override void StateStart()
    {
        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        await UniTask.Delay(750);
        
        var userCharacters = new List<Tech.Hive.V1.CharacterData>();
        ServerDataManager.Instance.Character.GetAllCharacters(userCharacters);

        // seq 기준으로 내림차순 정렬
        userCharacters.Sort((a, b) =>
        {
            var aData = SpecDataManager.Instance.GetCharacterData((int)a.CharacterId);
            var bData = SpecDataManager.Instance.GetCharacterData((int)b.CharacterId);

            if (aData == null && bData == null) return 0;
            if (aData == null) return 1;
            if (bData == null) return -1;

            return bData.seq.CompareTo(aData.seq);
        });

        int count = 0;
        foreach (var character in userCharacters)
        {
            var characterStat = new CharacterStatData((int)character.CharacterId, (int)character.Level, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());
            if (characterStat.Spec == null)
            {
                continue;
            }

            InGameTile ingameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
            int2 coordinate = new int2(ingameTile.X, ingameTile.Y);

            InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate, AllianceType.Player,
                typeof(CharacterStateIdle), false).Forget();
            count++;
            if (count >= 5)
                break;
            //await UniTask.Delay(210);
        }

        // 캐릭터 소환 후 1초 대기
        await UniTask.Delay(500);

        // 카메라 Tween 이동 시작
        _cameraMoveTween = LMotion.Create(_mainCamera.transform.position, _targetCameraPosition, _cameraTweenDuration)
            .WithEase(Ease.OutQuad)
            .BindToPosition(_mainCamera.transform);
        _mainCameraSizeTween = LMotion.Create(_mainCamera.orthographicSize, _targetOrthoSize, _cameraTweenDuration)
            .WithEase(Ease.OutQuad)
            .Bind(v => _mainCamera.orthographicSize = v);
        _characterCameraSizeTween = LMotion.Create(_characterCamera.orthographicSize, _targetOrthoSize, _cameraTweenDuration)
            .WithEase(Ease.OutQuad)
            .Bind(v => _characterCamera.orthographicSize = v);

        // 카메라 이동과 동시에 몬스터 소환 시작
        _canSpawnEnemy = true;
    }

    private float elapsedTime = 0f;
    private float interval = 1f;

    public override void StateRunning(float dt)
    {
        if (!_canSpawnEnemy) return;

        elapsedTime += dt;

        if (elapsedTime >= interval)
        {
            elapsedTime = 0f;
            SpawnEnemy().Forget();
            interval = Random.Range(1f, 4f);
        }
    }

    private async UniTask SpawnEnemy()
    {
        if (InGameObjectManager.Instance.EnemiesInPlaygroundForUpdate.Count >= _maxEnemySpawnCount) return;

        List<StageMonster> monsters =
            SpecDataManager.Instance.GetStageMonsterList(InGameManager.Instance.SpecStage.chapter_id, 1,
                DifficultyType.NORMAL);

        StageMonster randomMonster = monsters[Random.Range(0, monsters.Count)];

        if (randomMonster != null)
        {
            Debug.LogColor($"monster 추가 : {randomMonster.monster_id}");
            var statData = new CharacterStatData(randomMonster.monster_id, randomMonster.monster_lv,
                0, randomMonster.multiple_hp);

            InGameTile ingameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Enemy);
            if (ingameTile != null)
            {
                int2 coordinate = new int2(ingameTile.X, ingameTile.Y);
                InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Enemy,
                    typeof(CharacterStateIdle), false).Forget();
            }
        }
    }

    public override void StateEnd(bool isForced)
    {
        _canSpawnEnemy = false;

        // 카메라 Tween 정리
        _cameraMoveTween.TryCancel();
        _mainCameraSizeTween.TryCancel();
        _characterCameraSizeTween.TryCancel();

        ListPool<CharacterController>.Release(_playerCharacters);
        ListPool<CharacterController>.Release(_enemyCharacters);
    }
}
