using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
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
    [SerializeField] protected TextMeshProUGUI _obstacleCountText;

    private List<InGameObstacleItem> _obstacleItemList = new List<InGameObstacleItem>();
    private List<TestObstacle> _obstacleDataList = new List<TestObstacle>();
    private bool _isRunningAddObstacle;
    
    private int _wall1ID = 10001;
    private int _wall2ID = 10002;
    
    private int uid = 0;
    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnPvPSaveButtonClicked);
        _changeButton?.onClick.AddListener(OnChangeButtonClicked);
        _statisticButton?.onClick.AddListener(OnClickStatisticButton);
        _recommendButton?.onClick.AddListener(OnClickRecommend);
        _speedUpButton?.onClick.AddListener(OnClickSpeedUp);
    }

    public override void InitData()
    {
        //[TODO] stats로 체크하던 부분 item의 uid로 체크하도록 수정
        base.InitData();
        _obstacleItemList.Clear();
        BMUtil.RemoveChildObjects(_inGameObstacleItemTransform);
        
        var pvpTierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, UserDataManager.Instance.UserPVP.RankPoint);
        

        int wall1Count = pvpTierData.wall_1;
        int wall2Count = pvpTierData.wall_2;
        // int locatedWall1 = UserDataManager.Instance.UserPVP.MyPvpDefenseDeckList.PvpObstacleDecks.Count(l => l.Id == _wall1ID);
        // int locatedWall2 = UserDataManager.Instance.UserPVP.MyPvpDefenseDeckList.PvpObstacleDecks.Count(l => l.Id == _wall2ID);
        
        _obstacleDataList.Clear();
        for (int i = 0; i < wall1Count; i++)
        {
            _obstacleDataList.Add(new(_wall1ID, uid));
            uid++;
        }
        for (int i = 0; i < wall2Count; i++)
        {
            _obstacleDataList.Add(new(_wall2ID, uid));
            uid++;
        }

        foreach (var obstacleData in _obstacleDataList)
        {
            var obstacleItem = Instantiate(_ingameObstacleItemPrefab, _inGameObstacleItemTransform);
            _obstacleItemList.Add(obstacleItem);
            obstacleItem.SetData(this, obstacleData, AddObstacleToTile);
        }

        foreach (UserPVPObstacleBattleDeck deck in InGameManager.Instance.UserPvpBattleDeckList.PvpDeckList.PvpObstacleDecks)
        {
            if (deck.Id == _wall1ID)
                _obstacleDataList.RemoveAt(_obstacleDataList.Count - 1);
            else
                _obstacleDataList.RemoveAt(_obstacleDataList.Count - 1);
        }
        UpdateObstacleData();
    }
    
    public override void ReturnObstacle(CharacterController controller)
    {
        if (controller.CharacterId == _wall1ID)
        {
            _obstacleDataList.Add(new(_wall1ID, uid));
            uid++;
        }
        else if (controller.CharacterId == _wall2ID)
        {
            _obstacleDataList.Add(new(_wall2ID, uid));
            uid++;
        }
        // _obstacleDataList.Add(new(controller.CharacterId, controller.CharacterUId));
        UpdateObstacleData();
    }

    protected override bool IsCheckStartBattle()
    {
        return false;
    }

    private void OnPvPSaveButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        
        if (InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count == 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_CHAR_NOT_SET");
            return;
        }
        
        string contentText = "방어덱을 저장하시겠습니까?";

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

        // 가이드 미션 체크
        GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.SET_PVP_DEF_DECK, 0, 1);
        
        InGameManager.Instance.EndInGame();
        int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
        var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
        var transition = SceneTransition_FadeInOut.Create();
        await SceneLoading.GoToNextScene("Lobby", (int) specLastStageData.chapter_id, transition);
        
        SceneUILayerManager.OnSceneLoadedEvent += OpenArenaMainPopupAction;
    }
    
    private void OpenArenaMainPopupAction(string scenename)
    {
        if (scenename == "Lobby")
        {
            SceneUILayerManager.Instance.PushUILayerAsync<ArenaMainPopup>().Forget();
        
            SceneUILayerManager.OnSceneLoadedEvent -= OpenArenaMainPopupAction;
        }
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

        SetObstacleCountText();
    }
    
    public void SetObstacleCountText()
    {
        int obstacleCount = _obstacleDataList.Count;
        int maximumCount = _obstacleItemList.Count;

        string colorCode = obstacleCount == 0 ? "#CA6E71" : "#C5C5B2";
        _obstacleCountText.text = $"<color={colorCode}>{obstacleCount}</color>/{maximumCount}";
    }
}