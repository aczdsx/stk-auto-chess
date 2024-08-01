using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class TestObstacle
{
    public int ID;
    public int UID;

    public TestObstacle(int id, int uid)
    {
        ID = id;
        UID = uid;
    }
}

public class InGameBottomPvpDefenseUI : InGameBottomUI
{
    [SerializeField] protected InGameObstacleItem _ingameObstacleItemPrefab;
    [SerializeField] protected Transform _inGameObstacleItemTransform;
    [SerializeField] protected GameObject _obstacleListBody;
    [SerializeField] protected CAButton _changeButton;
    [SerializeField] protected GameObject _obstacleTipObj;

    private List<InGameObstacleItem> _obstacleItemList = new List<InGameObstacleItem>();
    private List<TestObstacle> _obstacleDataList = new List<TestObstacle>();
    private bool _isRunningAddObstacle;

    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnPvPSaveButtonClicked);
        _changeButton?.onClick.AddListener(OnChangeButtonClicked);
    }

    public override void InitData()
    {
        //[TODO] stats로 체크하던 부분 item의 uid로 체크하도록 수정
        base.InitData();
        _obstacleItemList.Clear();
        BMUtil.RemoveChildObjects(_inGameObstacleItemTransform);

        _obstacleDataList.Clear();
        _obstacleDataList.Add(new(10001, 1));
        _obstacleDataList.Add(new(10001, 2));
        _obstacleDataList.Add(new(10001, 3));
        _obstacleDataList.Add(new(10001, 4));
        _obstacleDataList.Add(new(10001, 5));

        foreach (var obstacleData in _obstacleDataList)
        {
            var obstacleItem = Instantiate(_ingameObstacleItemPrefab, _inGameObstacleItemTransform);
            _obstacleItemList.Add(obstacleItem);
            obstacleItem.SetData(this, obstacleData, AddObstacleToTile);
        }
    }
    
    public override void ReturnObstacle(CharacterController controller)
    {
        //[TODO] 다시 obstacle 추가 수정 필요
        _obstacleDataList.Add(new(controller.CharacterId, controller.CharacterUId));
        UpdateObstacleData();
    }

    protected override bool IsCheckStartBattle()
    {
        return false;
    }

    private void OnPvPSaveButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        // string contentText = LanguageManager.Instance.GetLanguageText("MSG_ALERT_EQUIP_COMMAND_SKILL");
        string contentText = "방어덱을 저장하시겠습니까?(TEST)";

        SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
        newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", () => { PvPSaveProcess().Forget(); });

        SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();
    }

    private void OnChangeButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        _obstacleListBody.SetActive(!_obstacleListBody.activeSelf);
        _characterListBody.SetActive(!_characterListBody.activeSelf);

        _obstacleTipObj.SetActive(!_obstacleTipObj.activeSelf);
        _characterTipObj.SetActive(!_characterTipObj.activeSelf);
    }

    private async UniTask PvPSaveProcess()
    {
        var characterControllers = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);

        // obstacleDeck
        // IEnumerable<UserPVPCharacterBattleDeck>
        // 설치한 장애물 정보 가져오기
        List<UserPVPObstacleBattleDeck> obstacleDeck = new();
        var obstacleList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Wall);
        foreach (var obstacle in obstacleList)
        {
            UserPVPObstacleBattleDeck deck = new();
            deck.Id = obstacle.CharacterId;
            deck.PosX = obstacle.CurrentTile.X;
            deck.PosY = obstacle.CurrentTile.Y;
            obstacleDeck.Add(deck);
        }

        var neutralList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral);
        foreach (var obstacle in neutralList)
        {
            UserPVPObstacleBattleDeck deck = new();
            deck.Id = obstacle.CharacterId;
            deck.PosX = obstacle.CurrentTile.X;
            deck.PosY = obstacle.CurrentTile.Y;
            obstacleDeck.Add(deck);
        }

        await PVPManager.Instance.SavePVPProfileData(characterControllers, obstacleDeck);

        InGameManager.Instance.EndInGame();
        int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
        var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
        var transition = SceneTransition_FadeInOut.Create();
        await SceneLoading.GoToNextScene("Lobby", (int) specLastStageData.chapter_id, transition);
    }

    private async void AddObstacleToTile(TestObstacle obstacleData)
    {
        if (_isRunningAddObstacle)
            return;

        _isRunningAddObstacle = true;
        List<SpecObstacle> specObstacles = SpecDataManager.Instance.GetSpecObstacleList(obstacleData.ID);
        if (specObstacles.Count > 0)
        {
            _obstacleDataList.RemoveAll(l => l.UID == obstacleData.UID);
            // [TODO] 소모한 개념 추가 필요.
            UpdateObstacleData();

            var ingameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile();
            int2 pos = new int2(ingameTile.X, ingameTile.Y);

            if (specObstacles[0].obstacle_type == ObstacleType.WALL)
            {
                await UniTask.WhenAll(new[]
                {
                    InGameObjectManager.Instance.AddNonStatObstacleToField(ingameTile.View.ID, obstacleData.ID,
                        AllianceType.Wall)
                });
            }
            else
            {
                var statData = new CharacterStatData(obstacleData.ID, 1, 1, 1);
                await UniTask.WhenAll(new[]
                {
                    InGameObjectManager.Instance.AddCharacterToField(statData, pos,
                        AllianceType.Neutral,
                        typeof(CharacterStateReady), false, HpBarType.None)
                });
            }
        }

        _isRunningAddObstacle = false;
    }

    public void UpdateObstacleData()
    {
        for (int i = 0; i < _obstacleItemList.Count; i++)
        {
            if (i < _obstacleDataList.Count)
            {
                _obstacleItemList[i].SetData(this, _obstacleDataList[i], AddObstacleToTile);
            }
            else
            {
                _obstacleItemList[i].SetData(this, null, null);
            }
        }
    }
}