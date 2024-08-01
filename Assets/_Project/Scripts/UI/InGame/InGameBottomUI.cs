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

public class InGameBottomUI : MonoBehaviour
{
    [SerializeField] protected CAButton _startButton;
    [SerializeField] protected CAButton _statisticButton;
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
    
    protected List<InGameCharacterItem> _characterItemList = new List<InGameCharacterItem>();
    protected bool _isOpenCommanderSkill;
    protected SpecUserGrade _specUserGrade;
    protected Type _combatType;
    
    private List<CharacterStatData> _characterStats;
    private bool _isRunningAddCharacter;

    protected void Awake()
    {
        var latestClearUserStageID = UserDataManager.Instance.GetLatestClearUserStageID();
        _isOpenCommanderSkill = latestClearUserStageID >= SpecDataManager.Instance.GetFirstCommanderSkillChapter();
        _commanderSkillObj.SetActive(_isOpenCommanderSkill);

        _specUserGrade = SpecDataManager.Instance.SpecUserGrade.Get(1); // [TODO] 현재 등급 가져오기
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

        if (IsCheckStartBattle())
            StartInGameBattle(_combatType);
    }

    protected virtual bool IsCheckStartBattle()
    {
        return false;
    }

    protected void StartInGameBattle(Type stateType)
    {
        _readyUIObj.SetActive(false);
        InGameMainFlowManager.Instance.AddNextState(stateType);
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);
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
        _statisticButton.gameObject.SetActive(isOn);
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
                _characterStats.Add(characterStat);
            }
        }
    }

    public void SetCommanderSkillUI(int index, int id)
    {
        var image = ImageManager.Instance.GetCommanderSkillSprite(id);
        if (image != null)
        {
            _commanderSkillUIList[index].SetIcon(image);
            _commanderSkillUIList[index].SetCommanderFx(false);
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

        for (int i = 0; i < _commanderSkillUIList.Count; i++)
            SetCommanderSkillUI(i, UserDataManager.Instance.GetEquippedCommanderSkill(i));

        var userCharacters = UserDataManager.Instance.GetAllUserCharacterList();
        foreach (var character in userCharacters)
        {
            _characterStats.Add(new CharacterStatData(character.CharacterId, character.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        }

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
        for (int i = 0; i < _commanderSkillUIList.Count; i++)
        {
            int equippedCommanderSkill = UserDataManager.Instance.GetEquippedCommanderSkill(i);
            if (equippedCommanderSkill != 0)
            {
                var data = SpecDataManager.Instance.GetCommanderSkillData(equippedCommanderSkill);
                CommanderSkillData skillData = InGameCommanderManager.Instance.InitCommanderSkillData(data);
                _commanderSkillUIList[i].SetData(skillData);
            }
        }
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
        var userLevelData =
            SpecDataManager.Instance.SpecAccountLevelExp.Get(UserDataManager.Instance.UserBasicData.Level);

        if (userLevelData.squad_count <= InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count)
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
        int characterCount = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count;
        int maximumCount = SpecDataManager.Instance.SpecAccountLevelExp
            .Get(UserDataManager.Instance.UserBasicData.Level).squad_count;

        string colorCode = characterCount == 0 ? "#CA6E71" : "#C5C5B2";
        _characterCountText.text = $"<color={colorCode}>{characterCount}</color>/{maximumCount}";
    }

    public void SetFocusCharacterUI(SpecCharacter spec)
    {
        foreach (var characterItem in _characterItemList)
        {
            if (characterItem.StatData == null)
            {
                characterItem.SetFocusCharacter(spec);
                return;
            }
        }
    }

    public void UnSetFocusCharacterUI(bool isDropFx)
    {
        foreach (var characterItem in _characterItemList)
        {
            if (characterItem.IsFocusSlot)
            {
                characterItem.SetFocusCharacter(null);
                if (isDropFx)
                    characterItem.PlayDropFx();
                return;
            }
        }
    }
    
    [ContextMenu("User Deck Json Data Test")]
    public void UserDeckJsonDataTest()
    {
        // 기본 정보
        UserPVPBattleDetailData newUserPvpBattleData = new UserPVPBattleDetailData();
        newUserPvpBattleData.PlayerId = UserDataManager.Instance.UserBasicData.PlayerId;
        newUserPvpBattleData.ServerId = 1;
        newUserPvpBattleData.RankId = 1001;
        newUserPvpBattleData.RankPoint = 1100;
        newUserPvpBattleData.Ranking = 1234;
        newUserPvpBattleData.Nickname = "STK_JSON_TESTER";
        newUserPvpBattleData.PlayerLv = 30;

        newUserPvpBattleData.PvpDeckList = new UserPVPBattleDeckList();
        
        // 덱 정보 (캐릭터)
        
        
        // 덱 정보 (장애물)
        UserPVPObstacleBattleDeck obstacle1 = new UserPVPObstacleBattleDeck();
        obstacle1.Id = 1001;
        obstacle1.PosX = 100;
        obstacle1.PosY = 100;
        
        UserPVPObstacleBattleDeck obstacle2 = new UserPVPObstacleBattleDeck();
        obstacle1.Id = 2001;
        obstacle1.PosX = 200;
        obstacle1.PosY = 200;
        
        UserPVPObstacleBattleDeck obstacle3 = new UserPVPObstacleBattleDeck();
        obstacle1.Id = 3001;
        obstacle1.PosX = 300;
        obstacle1.PosY = 300;
        
        newUserPvpBattleData.PvpDeckList.PvpObstacleDecks.Add(obstacle1);
        newUserPvpBattleData.PvpDeckList.PvpObstacleDecks.Add(obstacle2);
        newUserPvpBattleData.PvpDeckList.PvpObstacleDecks.Add(obstacle3);

        var result = JsonConvert.SerializeObject(newUserPvpBattleData);
        UnityEngine.Debug.Log("<<UserDeckJsonDataTest>> RESULT :: ");
        UnityEngine.Debug.Log(result);
    }
    
    [ContextMenu("User Simple Deck Json Data Test")]
    public void UserSimpleDeckJsonDataTest()
    {
        TestSimpleBattleDeckData newSimplePvpBattleData = new TestSimpleBattleDeckData();
        newSimplePvpBattleData.p_id = UserDataManager.Instance.UserBasicData.PlayerId;
        newSimplePvpBattleData.rank_id = 1001;
        newSimplePvpBattleData.rank_point = 1100;
        newSimplePvpBattleData.nickname = "STK_JSON_TESTER";
        newSimplePvpBattleData.u_lv = 30;
        newSimplePvpBattleData.battle_point = 12345;

        newSimplePvpBattleData.char_list = new List<TestSimpleCharacterData>();

        for (int i = 0; i < 8; ++i)
        {
            TestSimpleCharacterData newCharacter = new TestSimpleCharacterData();
            newCharacter.id = 1001 + i;
            newCharacter.lv = 1 + i;
            
            newSimplePvpBattleData.char_list.Add(newCharacter);
        }
        
        var result = JsonConvert.SerializeObject(newSimplePvpBattleData);
        UnityEngine.Debug.Log("<<UserSimpleDeckJsonDataTest>> RESULT :: ");
        UnityEngine.Debug.Log(result);
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
