using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;
using Random = Unity.Mathematics.Random;

public class InGameBottomUI : MonoBehaviour
{
    public bool IsSpeedUpRedDot => _speedUpRedDot != null && _speedUpRedDot.activeSelf;
    [SerializeField] protected CAButton _startButton;
    [SerializeField] protected CAButton _statisticButton;
    [SerializeField] protected CAButton _recommendButton;
    [SerializeField] protected CAButton _speedUpButton;

    [SerializeField] protected List<CAButton> _CommanderSkillButtonList;
    [SerializeField] protected Transform _characterSelectedTransform;
    [SerializeField] protected Transform _rightTransform;
    [SerializeField] protected Image _returnImage;
    [SerializeField] protected InGameCharacterItem _ingameCharacterItemPrefab;
    [SerializeField] protected Transform _inGameCharacterItemTransform;
    [SerializeField] protected GameObject _characterListBody;
    [SerializeField] protected GameObject _readyUIObj;
    [SerializeField] protected GameObject _commanderSkillObj;
    [SerializeField] protected List<CommanderSkillUI> _commanderSkillUIList;
    [SerializeField] protected TextMeshProUGUI _characterCountText;
    [SerializeField] protected ParticleSystem _commanderFx;
    [SerializeField] protected GameObject _characterTipObj;

    [SerializeField] protected GameObject _speedUpObjOn;
    [SerializeField] protected GameObject _speedUpObjOff;

    [SerializeField] protected GameObject _recommendObjOn;
    [SerializeField] protected GameObject _recommendObjOff;

    [SerializeField] protected GameObject _speedUpRedDot;
    [SerializeField] protected ScrollRect _scrollRect;

    [SerializeField] protected ParticleSystem _stageBattleFx;

    protected List<InGameCharacterItem> _characterItemList = new List<InGameCharacterItem>();
    protected bool _isOpenCommanderSkill;
    protected SpecUserGrade _specUserGrade;
    protected Type _combatType;

    private List<CharacterStatData> _characterStats;
    private bool _isRunningAddCharacter;
    private bool _isRunningRecommend;
    private bool _isStartRunningProcess = false;

    protected void Awake()
    {
        var latestClearUserStageID = UserDataManager.Instance.GetLatestClearUserStageID();
        _isOpenCommanderSkill = latestClearUserStageID >= SpecDataManager.Instance.GetFirstCommanderSkillChapter();
        _commanderSkillObj.SetActive(_isOpenCommanderSkill);
        _isStartRunningProcess = false;

        _specUserGrade =
            SpecDataManager.Instance.SpecUserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
        if (_specUserGrade != null)
        {
            for (int i = 0; i < _CommanderSkillButtonList.Count; i++)
            {
                bool isOpen = i < _specUserGrade.maximum_commander_skill_count;
                _CommanderSkillButtonList[i].gameObject.SetActive(isOpen);
                if (isOpen)
                {
                    int index = i;
                    _CommanderSkillButtonList[i]?.onClick.AddListener(() => OnClickCommanderSkillButton(index));
                }
            }
        }
    }

    protected void OnStartButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        if (IsCheckStartBattle() && !_isRunningRecommend)
            StartInGameBattle(_combatType);
    }

    protected void OnClickRecommend()
    {
        RecommendAction();
    }

    protected void OnClickSpeedUp()
    {
        var userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
        int missionOrder = 6;
        if (userGuideMissionData.MissionId <= missionOrder)
        {
            var missionData = SpecDataManager.Instance.GetGuideMissionDataByOrder(missionOrder);
            string msg = LanguageManager.Instance.GetLanguageText("MSG_CONTENT_UNLOCK");
            string titleName = LanguageManager.Instance.GetLanguageText(missionData.name_token);
            ToastManager.Instance.ShowToast(string.Format(msg, titleName));
            return;
        }

        bool isSpeedUp = Preference.LoadPreference(Pref.IS_SPEED_UP, false);
        Preference.SavePreference(Pref.IS_SPEED_UP, !isSpeedUp);
        InGameMainFlowManager.Instance.SetInGameSpeed(!isSpeedUp);

        UpdateSpeedUpBtn(!isSpeedUp);
    }

    private async void RecommendAction()
    {
        if (_isRunningRecommend == false && _recommendObjOn.activeSelf)
        {
            _recommendObjOff.SetActive(true);
            _recommendObjOn.SetActive(false);

            _isRunningRecommend = true;
            var charactersOnField = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).ToList();
            InGameObjectManager.Instance.ClearSynergyFx();
            foreach (var character in charactersOnField)
            {
                character.CurrentTile.SetUnoccupied();
                InGameObjectManager.Instance.RemoveCharacterFromField(character);
                InGameMain.GetInGameMain().ReturnCharacterUI(character);
            }

            _characterStats = _characterStats
                .OrderByDescending(stat => stat.Level)
                .ThenByDescending(stat => stat.CharacterID)
                .ToList();

            var userGrade =
                SpecDataManager.Instance.SpecUserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
            int maximumCharacterCount = userGrade.maximum_character_count;

            int addCharacterCount = _characterItemList.Count >= maximumCharacterCount
                ? maximumCharacterCount
                : _characterItemList.Count;
            List<InGameCharacterItem> selectedCharacterItemList = _characterItemList.GetRange(0, addCharacterCount);
            List<CharacterStatData> statDataList = selectedCharacterItemList.Select(item => item.StatData).ToList();

            foreach (var statData in statDataList)
            {
                _characterStats.RemoveAll(l => l.CharacterId == statData.CharacterId);
            }

            UpdateData();

            await AddCharacterToTile(statDataList);

            InGameManager.Instance.UpdateSynergyAndAttr();
            SetCharacterCountText();

            _isRunningRecommend = false;
        }
    }


    protected virtual bool IsCheckStartBattle()
    {
        return false;
    }

    protected void StartInGameBattle(Type stateType)
    {
        if (!_isStartRunningProcess)
        {
            _isStartRunningProcess = true;
            _readyUIObj.SetActive(false);
            InGameMainFlowManager.Instance.AddNextState(stateType);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);
        }
    }

    protected void OnClickStatisticButton()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(this).Forget();

        Preference.SavePreference(Pref.STATISTIC, true);
    }

    private void OnClickCommanderSkillButton(int index)
    {
        if (InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase)
            return;

        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>(index).Forget();
    }

    public void ChangeStatisticsButtonActiveState(bool isOn)
    {
        if (_statisticButton)
            _statisticButton.gameObject.SetActive(isOn);
        if (_speedUpButton)
            _speedUpButton.gameObject.SetActive(isOn);
    }

    public void InitReadyStateUI(Type combatType, List<UserCharacterBattleDeck> battleDeckList)
    {
        _combatType = combatType;
        foreach (var battleDeck in battleDeckList)
            _characterStats.RemoveAll(l => l.CharacterId == battleDeck.CharacterId);
        UpdateData();
        InGameManager.Instance.UpdateSynergyAndAttr();
        SetCharacterCountText();
    }

    public void CheckNewCharacter()
    {
        List<CharacterStatData> priorUserCharacters = _characterStats.ToList();
        List<CharacterStatData> userCharacters = new List<CharacterStatData>();
        foreach (var character in UserDataManager.Instance.GetAllUserCharacterList())
        {
            userCharacters.Add(new CharacterStatData(character.CharacterId, character.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            priorUserCharacters.Add(character.GetCharacterStat());
        }

        List<CharacterStatData> uniqueCharacters = userCharacters
            .Where(uc => priorUserCharacters.All(cs => cs.CharacterId != uc.CharacterId))
            .ToList();

        foreach (var characterStat in uniqueCharacters)
        {
            bool isExist = _characterItemList.Exists(l =>
                l.StatData != null && l.StatData.CharacterId == characterStat.CharacterId);
            if (!isExist)
            {
                var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                _characterItemList.Add(characterItem);
                characterItem.SetData(this, characterStat, AddCharacterToTile);
                characterItem.SetAlert();
                _characterStats.Add(characterStat);
                PlayStageBattleFx();
            }
        }
    }

    public void SetAlertBottomCharacter(int characterID)
    {
        var item = _characterItemList.Find(l => l.StatData != null && l.StatData.CharacterId == characterID);
        if (item)
            item.SetAlert();
    }

    public void SetCommanderSkillUI(int index, int id)
    {
        var image = ImageManager.Instance.GetCommanderSkillSprite(id);
        if (image != null)
        {
            _commanderSkillUIList[index].SetIcon(image);
            _commanderSkillUIList[index].SetCommanderFx(false);
            //요기다ㅏㄱ 해당 커맨더스킬 레벨 기입.
        }
        else
        {
            _commanderSkillUIList[index].SetCommanderFx(true);
        }
    }

    public virtual void InitData()
    {
        _characterItemList.Clear();
        BMUtil.RemoveChildObjects(_inGameCharacterItemTransform);
        _characterStats = new List<CharacterStatData>();
        UserDataManager userDataManagerInstance = UserDataManager.Instance;

        for (int i = 0; i < _commanderSkillUIList.Count; i++)
            SetCommanderSkillUI(i, userDataManagerInstance.GetEquippedCommanderSkillID(i));


        var userCharacters = userDataManagerInstance.GetAllUserCharacterList();
        foreach (var character in userCharacters)
        {
            _characterStats.Add(new CharacterStatData(character.CharacterId, character.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        }

        _characterStats = _characterStats
            .OrderByDescending(stat => stat.Level)
            .ThenByDescending(stat => stat.CharacterID)
            .ToList();

        foreach (var characterStat in _characterStats)
        {
            bool isExist = _characterItemList.Exists(l =>
                l.StatData != null && l.StatData.CharacterId == characterStat.CharacterId);
            if (!isExist)
            {
                var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                _characterItemList.Add(characterItem);
                characterItem.SetData(this, characterStat, AddCharacterToTile);
            }
        }
    }

    public void InitCommanderSkill()
    {
        UserDataManager userDataManagerInstance = UserDataManager.Instance;
        SpecDataManager specDataManagerInstance = SpecDataManager.Instance;
        for (int i = 0; i < _commanderSkillUIList.Count; i++)
        {
            int equippedCommanderSkillID = userDataManagerInstance.GetEquippedCommanderSkillID(i);
            if (equippedCommanderSkillID != 0)
            {
                var userSkillLevel = userDataManagerInstance.GetUserCommanderSkillLevel(equippedCommanderSkillID);
                var dataList = specDataManagerInstance.GetCommanderSkillDataList(equippedCommanderSkillID);
                CommanderSkillData skillData = InGameCommanderManager.Instance.InitCommanderSkillData(dataList[userSkillLevel - 1]);

                string preferenceKey = $"COMMANDER_AUTO_{(int)(i + 1)}";

                if (Enum.TryParse(preferenceKey, out Pref prefEnum))
                {
                    _commanderSkillUIList[i].SetData(skillData, prefEnum);
                }
            }
        }
    }

    public void InitSpeedUpSetting()
    {
        bool isSpeedUp = Preference.LoadPreference(Pref.IS_SPEED_UP, false);
        InGameMainFlowManager.Instance.SetInGameSpeed(isSpeedUp);

        UpdateSpeedUpBtn(isSpeedUp);
    }

    public void UpdateData()
    {
        for (int i = 0; i < _characterItemList.Count; i++)
        {
            if (i < _characterStats.Count)
            {
                _characterItemList[i].SetData(this, _characterStats[i], AddCharacterToTile);
            }
            else
            {
                _characterItemList[i].SetData(this, null, null);
            }
        }

        PlayStageBattleFx();
    }

    public void HideCharacterSelectUI(Action continuation)
    {
        Vector3 startPos = _characterSelectedTransform.transform.position;
        Vector3 endPos = new Vector3(startPos.x, startPos.y - 300, startPos.z);

        Vector3 rightStartPos = _rightTransform.transform.position;
        Vector3 rightEndPos = new Vector3(rightStartPos.x, rightStartPos.y - 150, rightStartPos.z);

        int completedAnimations = 0;

        Action onComplete = () =>
        {
            completedAnimations++;

            if (completedAnimations >= 2)
            {
                continuation?.Invoke();
            }
        };

        PrimeTweenExtensions.MoveTo(_characterSelectedTransform, endPos, 0.5f, PrimeTween.Ease.Linear)
            .OnComplete(onComplete);

        PrimeTweenExtensions.MoveTo(_rightTransform, rightEndPos, 0.5f, PrimeTween.Ease.Linear)
            .OnComplete(onComplete);
    }

    public void ReturnObjectActive(bool active)
    {
        _returnImage.gameObject.SetActive(active);
    }

    public void ReturnObjectColorChange(bool active)
    {
        _returnImage.color = (active) ? Color.green : Color.white;
    }

    public void ReturnCharacter(CharacterController controller)
    {
        _characterStats.Add(controller.GetCharacterStat());
        _characterStats = _characterStats.OrderByDescending(stat => stat.Level).ToList();

        UpdateData();
        SetCharacterCountText();
    }

    public virtual void ReturnObstacle(CharacterController controller)
    {
    }

    private async void AddCharacterToTile(CharacterStatData statData)
    {
        if (_isRunningAddCharacter)
            return;

        _isRunningAddCharacter = true;

        var userGrade =
            SpecDataManager.Instance.SpecUserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
        if (userGrade.maximum_character_count <=
            InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_OVER_COUNT_CHARACTER");
        }
        else
        {
            _characterStats.RemoveAll(l => l.CharacterId == statData.CharacterId);
            UpdateData();

            Debug.Log($"AddBoardCharacter: {statData.CharacterId}");
            var ingameTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(statData.Spec);
            int2 pos = new int2(ingameTile.X, ingameTile.Y);

            await UniTask.WhenAll(new[]
            {
                InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Player,
                    typeof(CharacterStateReady), true, HpBarType.Synergy),
            });

            InGameManager.Instance.UpdateSynergyAndAttr();
            SetCharacterCountText();
        }

        _isRunningAddCharacter = false;
    }

    private async UniTask AddCharacterToTile(List<CharacterStatData> statDataList)
    {
        if (_isRunningAddCharacter)
            return;

        _isRunningAddCharacter = true;

        var userGrade =
            SpecDataManager.Instance.SpecUserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
        if (userGrade.maximum_character_count <=
            InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_OVER_COUNT_CHARACTER");
        }
        else
        {
            foreach (var statData in statDataList)
            {
                var inGameTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(statData.Spec);
                int2 pos = new int2(inGameTile.X, inGameTile.Y);

                await UniTask.WhenAll(new[]
                {
                    InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Player,
                        typeof(CharacterStateReady), true, HpBarType.Synergy),
                });
            }
        }

        _isRunningAddCharacter = false;
    }


    public void UpdateCommanderSkillCoolTime()
    {
        foreach (var skillUI in _commanderSkillUIList)
        {
            if (skillUI.Data != null)
            {
                skillUI.UpdateCommanderSkillCoolTime();
                // bool isActiveCoolTime = skillUI.Data.ElapsedTime > skillUI.Data.DurationTime;
                // InGameMain.GetInGameMain().SetCommanderFx(isActiveCoolTime);
            }
        }
    }

    public void SetCharacterCountText()
    {
        var userGrade =
            SpecDataManager.Instance.SpecUserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);

        int characterCount = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count;
        int maximumCount = userGrade.maximum_character_count;

        string colorCode = characterCount == 0 ? "#CA6E71" : "#C5C5B2";
        _characterCountText.text = $"<color={colorCode}>{characterCount}</color>/{maximumCount}";

        bool isAvailableRecommend = maximumCount != characterCount;

        if (_recommendObjOff != null)
            _recommendObjOff.SetActive(!isAvailableRecommend);
        if (_recommendObjOn != null)
            _recommendObjOn.SetActive(isAvailableRecommend);
    }

    public void SetFocusCharacterUI(SpecCharacter spec)
    {
        foreach (var characterItem in _characterItemList)
        {
            if (characterItem.StatData == null)
            {
                characterItem.SetFocusCharacter(spec);
                //RearrangeCharacterList();
                ScrollTo(characterItem.transform as RectTransform);
                return;
            }
        }
    }

    private void RearrangeCharacterList()
    {
        _characterItemList.Sort((item1, item2) => item2.GetDisplayLv().CompareTo(item1.GetDisplayLv()));

        for (int i = 0; i < _characterItemList.Count; i++)
        {
            _characterItemList[i].transform.SetSiblingIndex(i);
        }
    }

    public void ScrollTo(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform content = _scrollRect.content;

        Vector2 pos = (Vector2)_scrollRect.transform.InverseTransformPoint(content.position)
                     - (Vector2)_scrollRect.transform.InverseTransformPoint(target.position);

        content.anchoredPosition = pos;
    }

    public void UnSetFocusCharacterUI(bool isDropFx)
    {
        foreach (var characterItem in _characterItemList)
        {
            if (characterItem.IsFocusSlot)
            {
                characterItem.SetFocusCharacter(null);
                if (isDropFx)
                {
                    characterItem.PlayDropFx();
                }
                else
                {
                    RearrangeCharacterList();
                }

                return;
            }
        }
    }

    private void PlayStageBattleFx()
    {
        if (InGameManager.Instance.SpecStage != null)
        {
            if (_stageBattleFx && InGameManager.Instance.SpecStage.chapter_id == 1)
            {
                var isPlayFx = _characterStats.Count == 0;
                _stageBattleFx.gameObject.SetActive(isPlayFx);
                if (isPlayFx)
                    _stageBattleFx.Play();
            }
        }
    }

    private void UpdateSpeedUpBtn(bool isSpeedUp)
    {
        if (_speedUpRedDot)
        {
            var userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
            int minMissionOrder = 7;
            int maxMissionOrder = 9;
            bool isGuide = userGuideMissionData.MissionId >= minMissionOrder && userGuideMissionData.MissionId <= maxMissionOrder;

            _speedUpRedDot.SetActive(!isSpeedUp && isGuide);
        }

        if (_speedUpObjOn)
            _speedUpObjOn.SetActive(isSpeedUp);
        if (_speedUpObjOff)
            _speedUpObjOff.SetActive(!isSpeedUp);
    }

    [ContextMenu("User Simple Deck Json Data Test")]
    public void UserSimpleDeckJsonDataTest()
    {
        int rankingPoint = 1000 + UnityEngine.Random.Range(-50, 50);
        var tierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, rankingPoint);
        int averageLv = 5;
        int playerLv = 15;

        var characterControllers = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
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

        UserPVPBattleSimpleData simpleData = new UserPVPBattleSimpleData();
        UserPVPBattleDetailData detailData = new UserPVPBattleDetailData();
        simpleData.PlayerId = "DUMMY_1";
        simpleData.Nickname = "무명기사";
        simpleData.PlayerLv = playerLv + UnityEngine.Random.Range(-2, 2);
        simpleData.RankPoint = rankingPoint;
        simpleData.RankId = tierData.ranking_id;
        simpleData.ServerId = 1;

        detailData.PlayerId = "DUMMY_1";
        detailData.Nickname = "무명기사";
        detailData.PlayerLv = simpleData.PlayerLv;
        detailData.RankPoint = rankingPoint;
        detailData.RankId = tierData.ranking_id;
        detailData.ServerId = 1;
        detailData.PvpDeckList = new UserPVPBattleDeckList();
        foreach (var character in characterControllers)
        {
            int lv = averageLv + UnityEngine.Random.Range(-3, 3);
            {
                UserPVPCharacterSimpleDeck deck = new UserPVPCharacterSimpleDeck();
                deck.Id = character.SpecCharacter.character_id;
                deck.Lv = lv;
                simpleData.SimpleDeckList.Add(deck);
                CharacterStatData data = new CharacterStatData(deck.Id, deck.Lv);
                simpleData.BattlePoint += (int)data.GetAttrValue();
            }

            {

                UserPVPCharacterBattleDeck newUserBattleDeck = new UserPVPCharacterBattleDeck();

                newUserBattleDeck.Id = character.CharacterId;
                newUserBattleDeck.Lv = lv;
                newUserBattleDeck.PosX = character.CurrentTile.X;
                newUserBattleDeck.PosY = character.CurrentTile.Y;

                detailData.PvpDeckList.PvpCharacterDecks.Add(newUserBattleDeck);
                CharacterStatData data = new CharacterStatData(newUserBattleDeck.Id, newUserBattleDeck.Lv);
                detailData.BattlePoint += (int)data.GetAttrValue();
            }
        }


        detailData.PvpDeckList.PvpObstacleDecks.AddRange(obstacleDeck);


        var result = JsonConvert.SerializeObject(simpleData);
        UnityEngine.Debug.Log("SIMPLE");
        UnityEngine.Debug.Log("SIMPLE");
        UnityEngine.Debug.Log(result);


        var result2 = JsonConvert.SerializeObject(detailData);
        UnityEngine.Debug.Log("DETAIL");
        UnityEngine.Debug.Log("DETAIL");
        UnityEngine.Debug.Log(result2);
    }

    [Serializable]
    public class TestSimpleBattleDeckData
    {
        public string p_id;
        public string nickname;
        public int u_lv;
        public int battle_point;
        public int rank_id;
        public int rank_point;

        public List<TestSimpleCharacterData> char_list;
    }

    [Serializable]
    public class TestSimpleCharacterData
    {
        public int id;
        public int lv;
    }
}