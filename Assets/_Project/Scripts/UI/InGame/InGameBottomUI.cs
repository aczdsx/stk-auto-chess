using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using R3;
using Tech.Hive.V1;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;
using Random = Unity.Mathematics.Random;

public class InGameBottomUI : MonoBehaviour
{
    public bool IsSpeedUpRedDot => _speedUpRedDot != null && _speedUpRedDot.activeSelf;
    [SerializeField] protected CAButton _startButton;
    [SerializeField] protected CAButton _statisticButton;
    [SerializeField] protected CAButton _recommendButton;
    [SerializeField] protected CAButton _speedUpButton;
    [SerializeField] protected CAButton _filterButton;

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
    protected UserGrade _specUserGrade;
    protected Type _combatType;
    protected object _combatStateData;

    private List<CharacterStatData> _characterStats;
    private List<CharacterStatData> _allCharacterStats = new(); // 필터링 전 전체 목록
    private HashSet<SynergyType> _selectedElementFilters = new(); // 속성 필터
    private HashSet<SynergyType> _selectedStellaFilters = new(); // 성군 필터
    private bool _isRunningAddCharacter;
    private bool _isRunningRecommend;
    private bool _isStartRunningProcess = false;

    protected virtual void Awake()
    {
        var latestClearUserStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
        _isOpenCommanderSkill = latestClearUserStageID >= SpecDataManager.Instance.GetFirstCommanderSkillChapter();
        _commanderSkillObj.SetActive(_isOpenCommanderSkill);
        _isStartRunningProcess = false;

        _specUserGrade =
            SpecDataManager.Instance.UserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
        if (_specUserGrade != null)
        {
            for (int i = 0; i < _CommanderSkillButtonList.Count; i++)
            {
                bool isOpen = i < _specUserGrade.maximum_commander_skill_count;
                _CommanderSkillButtonList[i].gameObject.SetActive(isOpen);
                if (isOpen)
                {
                    _CommanderSkillButtonList[i]?.OnClickAsObservable().Subscribe((this, i), (_, state) => state.Item1.OnClickCommanderSkillButton(state.i)).AddTo(this);
                }
            }
        }

        // 필터 버튼 바인딩
        Debug.Log($"[Filter] _filterButton is null: {_filterButton == null}");
        if (_filterButton != null)
        {
            Debug.Log("[Filter] _filterButton binding success");
            _filterButton.OnClickAsObservable().Subscribe(this, (_, self) =>
            {
                Debug.Log("[Filter] _filterButton clicked!");
                self.OpenFilterPopup();
            }).AddTo(this);
        }
    }

    protected async UniTask OnStartButtonClickedAsync()
    {
        var isCheck = await IsCheckStartBattle();
        if (isCheck && !_isRunningRecommend)
            StartInGameBattle(_combatType);
    }

    protected void OnClickRecommend()
    {
        RecommendAction();
    }

    protected void OnClickSpeedUp()
    {
        var guideMission = ServerDataManager.Instance.GuideMission;
        int missionOrder = 6;
        if (guideMission.GuideMissionId <= missionOrder)
        {
            var missionData = SpecDataManager.Instance.GetGuideMissionDataByOrder(missionOrder);
            string msg = LanguageManager.Instance.GetDefaultText("MSG_CONTENT_UNLOCK");
            string titleName = LanguageManager.Instance.GetDefaultText(missionData.name_token);
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
            // InGameObjectManager.Instance.ClearSynergyFx();
            foreach (var character in charactersOnField)
            {
                character.CurrentTile.SetUnoccupied();
                InGameObjectManager.Instance.RemoveCharacterFromField(character);
                InGameMain.GetInGameMain().ReturnCharacterUI(character);
            }

            // 필터 적용 후 정렬
            RefreshFilteredList();

            var userGrade =
                SpecDataManager.Instance.UserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
            int maximumCharacterCount = userGrade.maximum_character_count;

            int addCharacterCount = _characterItemList.Count >= maximumCharacterCount
                ? maximumCharacterCount
                : _characterItemList.Count;
            List<InGameCharacterItem> selectedCharacterItemList = _characterItemList.GetRange(0, addCharacterCount);
            List<CharacterStatData> statDataList = selectedCharacterItemList.Select(item => item.StatData).ToList();

            foreach (var statData in statDataList)
            {
                _allCharacterStats.RemoveAll(l => l.CharacterId == statData.CharacterId);
            }
            RefreshFilteredList();

            UpdateData();

            await AddCharacterToTile(statDataList);

            InGameManager.Instance.UpdateSynergyAndAttr();
            SetCharacterCountText();
            UpdatePreviewSynergyEffectCode();
            _isRunningRecommend = false;
        }
    }


    protected virtual UniTask<bool> IsCheckStartBattle()
    {
        return UniTask.FromResult(false);
    }

    protected void StartInGameBattle(Type stateType)
    {
        if (!_isStartRunningProcess)
        {
            _isStartRunningProcess = true;
            _readyUIObj.SetActive(false);
            InGameMainFlowManager.Instance.AddNextState(stateType, _combatStateData);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);
        }
    }

    protected void OnClickStatisticButton()
    {
        SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(this).Forget();

        Preference.SavePreference(Pref.STATISTIC, true);
    }

    private void OnClickCommanderSkillButton(int index)
    {
        if (InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase)
            return;

        SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>(index).Forget();
    }

    public void ChangeStatisticsButtonActiveState(bool isOn)
    {
        if (_statisticButton)
            _statisticButton.gameObject.SetActive(isOn);
        if (_speedUpButton)
            _speedUpButton.gameObject.SetActive(isOn);
    }

    public void InitReadyStateUI(Type combatType, List<DeckCharacterPlacement> battleDeckList, object stateData = null)
    {
        _combatType = combatType;
        _combatStateData = stateData;
        _isStartRunningProcess = false;
        foreach (var battleDeck in battleDeckList)
        {
            // CharacterUid로 캐릭터 데이터를 찾아서 SpecCharacterIndex 비교
            var characterData = ServerDataManager.Instance.Character.GetCharacter(battleDeck.CharacterId);
            if (characterData != null)
            {
                _allCharacterStats.RemoveAll(l => l.CharacterId == characterData.CharacterId);
            }
        }
        RefreshFilteredList();
        UpdateData();
        InGameManager.Instance.UpdateSynergyAndAttr();
        SetCharacterCountText();
        UpdatePreviewSynergyEffectCode();
    }

    public void CheckNewCharacter()
    {
        List<CharacterStatData> priorUserCharacters = _allCharacterStats.ToList();
        List<CharacterStatData> userCharacters = new List<CharacterStatData>();
        var allCharacters = new List<CharacterData>();
        ServerDataManager.Instance.Character.GetAllCharacters(allCharacters);
        foreach (var character in allCharacters)
        {
            userCharacters.Add(new CharacterStatData((int)character.CharacterId, (int)character.Level,
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
                // 전체 목록에 추가
                _allCharacterStats.Add(characterStat);

                // 필터 통과 시에만 UI에 추가
                if (PassFilter(characterStat))
                {
                    var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                    _characterItemList.Add(characterItem);
                    characterItem.SetData(this, characterStat, AddCharacterToTile);
                    characterItem.SetAlert();
                    _characterStats.Add(characterStat);
                    PlayStageBattleFx();

                    // 튜토리얼 중이면 새 캐릭터 아이템에 TutorialTarget 등록
                    if (TutorialManager.Instance.HasTutorialStage)
                    {
                        RegisterCharacterItemForTutorial(characterItem, characterStat.CharacterId);
                    }
                }
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
        var spriteName = SpriteNameParser.GetCommanderSkillSprite(id);
        if (!string.IsNullOrEmpty(spriteName))
        {
            _commanderSkillUIList[index].SetIcon(spriteName);
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
        _allCharacterStats = new List<CharacterStatData>();
        UserDataManager userDataManagerInstance = UserDataManager.Instance;

        for (int i = 0; i < _commanderSkillUIList.Count; i++)
            SetCommanderSkillUI(i, ServerDataManager.Instance.CommanderSkill.GetEquippedCommanderSkillId(i));


        var userCharacters = new List<Tech.Hive.V1.CharacterData>();
        ServerDataManager.Instance.Character.GetAllCharacters(userCharacters);
        foreach (var character in userCharacters)
        {
            var characterStat = new CharacterStatData((int)character.CharacterId, (int)character.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());
            _allCharacterStats.Add(characterStat);
        }

        // CP(전투력) 기준 내림차순 정렬
        _allCharacterStats = _allCharacterStats
            .OrderByDescending(stat => stat.GetAttrValueCP())
            .ToList();

        // 필터 적용하여 _characterStats 갱신
        RefreshFilteredList();

        foreach (var characterStat in _characterStats)
        {
            bool isExist = _characterItemList.Exists(l =>
                l.StatData != null && l.StatData.CharacterId == characterStat.CharacterId);
            if (!isExist)
            {
                var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                _characterItemList.Add(characterItem);
                characterItem.SetData(this, characterStat, AddCharacterToTile);

                // 튜토리얼 중이면 새 캐릭터 아이템에 TutorialTarget 등록
                if (TutorialManager.Instance.HasTutorialStage)
                {
                    RegisterCharacterItemForTutorial(characterItem, characterStat.CharacterId);
                }
            }
        }

        // 튜토리얼 중이면 캐릭터 아이템에 TutorialTarget 등록
        if (TutorialManager.Instance.HasTutorialStage)
        {
            RegisterCharacterItemsForTutorial();
        }
    }

    public void InitCommanderSkill()
    {
        UserDataManager userDataManagerInstance = UserDataManager.Instance;
        SpecDataManager specDataManagerInstance = SpecDataManager.Instance;
        for (int i = 0; i < _commanderSkillUIList.Count; i++)
        {
            int equippedCommanderSkillID = ServerDataManager.Instance.CommanderSkill.GetEquippedCommanderSkillId(i);
            if (equippedCommanderSkillID != 0)
            {
                var userSkillLevel = ServerDataManager.Instance.CommanderSkill.GetUserCommanderSkillLevel(equippedCommanderSkillID);
                var dataList = specDataManagerInstance.GetCommanderSkillDataList(equippedCommanderSkillID);
                CommanderSkillInGameData skillData = InGameCommanderManager.Instance.InitCommanderSkillData(dataList[userSkillLevel - 1]);

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

    /// <summary>
    /// 스크린 좌표가 ScrollRect 영역 내에 있는지 확인
    /// </summary>
    public bool IsPointInScrollRect(Vector2 screenPosition)
    {
        if (_scrollRect == null) return false;

        RectTransform scrollRectTransform = _scrollRect.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(
            scrollRectTransform,
            screenPosition,
            null // Screen Space - Overlay Canvas의 경우 null
        );
    }

    /// <summary>
    /// ScrollRect의 RectTransform 반환 (외부에서 드롭 영역 체크용)
    /// </summary>
    public RectTransform GetScrollRectTransform()
    {
        return _scrollRect?.GetComponent<RectTransform>();
    }

    /// <summary>
    /// 드롭 영역 하이라이트 설정
    /// </summary>
    public void SetDropHighlight(bool active)
    {
        if (_returnImage == null) return;

        // 하이라이트 활성화 시 녹색 반투명, 비활성화 시 숨김
        _returnImage.gameObject.SetActive(active);
        if (active)
        {
            _returnImage.color = new Color(0.2f, 0.8f, 0.3f, 0.5f); // 녹색 반투명
        }
    }

    public void ReturnCharacter(CharacterController controller)
    {
        var stat = controller.GetCharacterStat();
        _allCharacterStats.Add(stat);
        // CP(전투력) 기준 내림차순 정렬
        _allCharacterStats = _allCharacterStats.OrderByDescending(s => s.GetAttrValueCP()).ToList();

        // 필터 적용 후 _characterStats 갱신
        RefreshFilteredList();
        UpdateData();
        SetCharacterCountText();
        InGameTouchManager.Instance.SelectedFirstTileID = -1;
    }

    /// <summary>
    /// UI 드래그로 보드에 배치할 때 리스트에서 캐릭터 제거
    /// </summary>
    public void RemoveCharacterFromList(CharacterStatData statData)
    {
        if (statData == null) return;

        _allCharacterStats.RemoveAll(l => l.CharacterId == statData.CharacterId);
        RefreshFilteredList();
        UpdateData();
        InGameManager.Instance.UpdateSynergyAndAttr();
        SetCharacterCountText();
        InGameTouchManager.Instance.SelectedFirstTileID = -1;
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
            SpecDataManager.Instance.UserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
        if (userGrade.maximum_character_count <=
            InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_OVER_COUNT_CHARACTER");
        }
        else
        {
            _allCharacterStats.RemoveAll(l => l.CharacterId == statData.CharacterId);
            RefreshFilteredList();
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
            InGameTouchManager.Instance.SelectedFirstTileID = -1;
        }

        _isRunningAddCharacter = false;
    }

    private async UniTask AddCharacterToTile(List<CharacterStatData> statDataList)
    {
        if (_isRunningAddCharacter)
            return;

        _isRunningAddCharacter = true;

        var userGrade =
            SpecDataManager.Instance.UserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
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
            SpecDataManager.Instance.UserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);

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

    public void SetFocusCharacterUI(CharacterInfo spec)
    {
        if (spec == null || spec.character_type == CharacterType.BATTLEITEM)
        {
            return;
        }

        foreach (var characterItem in _characterItemList)
        {
            if (characterItem.StatData == null)
            {
                // characterItem.SetFocusCharacter(spec);
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
                // characterItem.SetFocusCharacter(null);
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

    private void UpdatePreviewSynergyEffectCode(CharacterStatData statData = null)
    {
        if (statData != null)
        {
        }
        // else
        // {// recommend 시, Init시 들어옴.
        //     // stateCombatStepBase.TidyUpPreviewSynergy(AllianceType.Player);
        //     // stateCombatStepBase.AddSynergy(AllianceType.Player);
        // }


    }

    private void UpdateSpeedUpBtn(bool isSpeedUp)
    {
        if (_speedUpRedDot)
        {
            var guideMission = ServerDataManager.Instance.GuideMission;
            int minMissionOrder = 7;
            int maxMissionOrder = 9;
            bool isGuide = guideMission.GuideMissionId >= minMissionOrder && guideMission.GuideMissionId <= maxMissionOrder;

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
        // int rankingPoint = 1000 + UnityEngine.Random.Range(-50, 50);
        // var tierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, rankingPoint);
        // int averageLv = 5;
        // int playerLv = 15;
        //
        // var characterControllers = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
        // List<UserPVPObstacleBattleDeck> obstacleDeck = new();
        // var obstacleList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Wall);
        // foreach (var obstacle in obstacleList)
        // {
        //     UserPVPObstacleBattleDeck deck = new();
        //     deck.Id = obstacle.CharacterId;
        //     deck.PosX = obstacle.CurrentTile.X;
        //     deck.PosY = obstacle.CurrentTile.Y;
        //     obstacleDeck.Add(deck);
        // }
        //
        // var neutralList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral);
        // foreach (var obstacle in neutralList)
        // {
        //     UserPVPObstacleBattleDeck deck = new();
        //     deck.Id = obstacle.CharacterId;
        //     deck.PosX = obstacle.CurrentTile.X;
        //     deck.PosY = obstacle.CurrentTile.Y;
        //     obstacleDeck.Add(deck);
        // }
        //
        // UserPVPBattleSimpleData simpleData = new UserPVPBattleSimpleData();
        // UserPVPBattleDetailData detailData = new UserPVPBattleDetailData();
        // simpleData.PlayerId = "DUMMY_1";
        // simpleData.Nickname = "무명기사";
        // simpleData.PlayerLv = playerLv + UnityEngine.Random.Range(-2, 2);
        // simpleData.RankPoint = rankingPoint;
        // simpleData.RankId = tierData.ranking_id;
        // simpleData.ServerId = 1;
        //
        // detailData.PlayerId = "DUMMY_1";
        // detailData.Nickname = "무명기사";
        // detailData.PlayerLv = simpleData.PlayerLv;
        // detailData.RankPoint = rankingPoint;
        // detailData.RankId = tierData.ranking_id;
        // detailData.ServerId = 1;
        // detailData.PvpDeckList = new UserPVPBattleDeckList();
        // foreach (var character in characterControllers)
        // {
        //     int lv = averageLv + UnityEngine.Random.Range(-3, 3);
        //     {
        //         UserPVPCharacterSimpleDeck deck = new UserPVPCharacterSimpleDeck();
        //         deck.Id = character.SpecCharacter.character_id;
        //         deck.Lv = lv;
        //         simpleData.SimpleDeckList.Add(deck);
        //         CharacterStatData data = new CharacterStatData(deck.Id, deck.Lv);
        //         simpleData.BattlePoint += (int)data.GetAttrValue();
        //     }
        //
        //     {
        //
        //         UserPVPCharacterBattleDeck newUserBattleDeck = new UserPVPCharacterBattleDeck();
        //
        //         newUserBattleDeck.Id = character.CharacterId;
        //         newUserBattleDeck.Lv = lv;
        //         newUserBattleDeck.PosX = character.CurrentTile.X;
        //         newUserBattleDeck.PosY = character.CurrentTile.Y;
        //
        //         detailData.PvpDeckList.PvpCharacterDecks.Add(newUserBattleDeck);
        //         CharacterStatData data = new CharacterStatData(newUserBattleDeck.Id, newUserBattleDeck.Lv);
        //         detailData.BattlePoint += (int)data.GetAttrValue();
        //     }
        // }
        //
        //
        // detailData.PvpDeckList.PvpObstacleDecks.AddRange(obstacleDeck);
        //
        //
        // var result = JsonConvert.SerializeObject(simpleData);
        // UnityEngine.Debug.Log("SIMPLE");
        // UnityEngine.Debug.Log("SIMPLE");
        // UnityEngine.Debug.Log(result);
        //
        //
        // var result2 = JsonConvert.SerializeObject(detailData);
        // UnityEngine.Debug.Log("DETAIL");
        // UnityEngine.Debug.Log("DETAIL");
        // UnityEngine.Debug.Log(result2);
    }

    /// <summary>
    /// 튜토리얼용으로 모든 캐릭터 아이템에 TutorialTarget 등록
    /// 같은 캐릭터ID가 여러 개면 인덱스 추가 (130601_0, 130601_1)
    /// 유일하면 ID만 사용 (130601)
    /// </summary>
    private void RegisterCharacterItemsForTutorial()
    {
        // 유효한 캐릭터 아이템만 필터링
        var validItems = new List<InGameCharacterItem>();
        foreach (var item in _characterItemList)
        {
            if (item != null && item.StatData != null)
                validItems.Add(item);
        }

        // 캐릭터 ID별 개수 카운트
        var idCount = new Dictionary<int, int>();
        foreach (var item in validItems)
        {
            int charId = item.StatData.CharacterId;
            if (idCount.ContainsKey(charId))
                idCount[charId]++;
            else
                idCount[charId] = 1;
        }

        // 중복 캐릭터용 인덱스 추적
        var idIndex = new Dictionary<int, int>();

        foreach (var item in validItems)
        {
            int charId = item.StatData.CharacterId;

            var tutorialTarget = item.gameObject.GetComponent<TutorialTarget>();
            if (tutorialTarget == null)
            {
                tutorialTarget = item.gameObject.AddComponent<TutorialTarget>();
            }

            string targetId;
            if (idCount[charId] > 1)
            {
                // 중복 캐릭터: 인덱스 추가
                if (!idIndex.ContainsKey(charId))
                    idIndex[charId] = 0;

                targetId = $"Slot_{charId}_{idIndex[charId]++}";
            }
            else
            {
                // 유일한 캐릭터: ID만 사용
                targetId = $"Slot_{charId.ToString()}";
            }

            tutorialTarget.SetTargetId(targetId);
        }
    }

    /// <summary>
    /// 개별 캐릭터 아이템에 TutorialTarget 등록 (새로 추가된 캐릭터용)
    /// </summary>
    private void RegisterCharacterItemForTutorial(InGameCharacterItem item, int characterId)
    {
        if (item == null) return;

        var tutorialTarget = item.gameObject.GetComponent<TutorialTarget>();
        if (tutorialTarget == null)
        {
            tutorialTarget = item.gameObject.AddComponent<TutorialTarget>();
        }

        // 같은 ID가 이미 있는지 확인
        int existingCount = 0;
        foreach (var existingItem in _characterItemList)
        {
            if (existingItem != item && existingItem.StatData != null &&
                existingItem.StatData.CharacterId == characterId)
            {
                existingCount++;
            }
        }

        string targetId = existingCount > 0
            ? $"Slot_{characterId}_{existingCount}"
            : $"Slot_{characterId.ToString()}";

        tutorialTarget.SetTargetId(targetId);
    }

    #region Filter

    /// <summary>
    /// 필터 팝업 열기
    /// </summary>
    private void OpenFilterPopup()
    {
        Debug.Log("[Filter] OpenFilterPopup called!");
        var param = new FilterTooltipInIngamePopup.FilterParam(
            _selectedElementFilters,
            _selectedStellaFilters,
            ApplyFilter
        );
        Debug.Log("[Filter] Pushing FilterTooltipInIngamePopup...");
        SceneUILayerManager.Instance.PushUILayerAsync<FilterTooltipInIngamePopup>(param).Forget();
    }

    /// <summary>
    /// 필터 적용 (팝업에서 콜백으로 호출)
    /// </summary>
    public void ApplyFilter(HashSet<SynergyType> elements, HashSet<SynergyType> stellas)
    {
        _selectedElementFilters = new HashSet<SynergyType>(elements);
        _selectedStellaFilters = new HashSet<SynergyType>(stellas);
        RefreshFilteredList();
        UpdateData();
    }

    /// <summary>
    /// 필터 상태에 따라 _characterStats 갱신
    /// </summary>
    private void RefreshFilteredList()
    {
        // CP(전투력) 기준 내림차순 정렬
        _characterStats = _allCharacterStats
            .Where(PassFilter)
            .OrderByDescending(stat => stat.GetAttrValueCP())
            .ToList();
    }

    /// <summary>
    /// 필터 조건 통과 여부 확인 (AND 조건)
    /// </summary>
    private bool PassFilter(CharacterStatData stat)
    {
        // 속성 필터: 비어있으면 통과, 아니면 포함 여부 확인
        bool passElement = _selectedElementFilters.Count == 0
            || _selectedElementFilters.Contains(stat.Spec.character_element_type);

        // 성군 필터: 비어있으면 통과, 아니면 포함 여부 확인
        bool passStella = _selectedStellaFilters.Count == 0
            || _selectedStellaFilters.Contains(stat.Spec.character_stella_type);

        return passElement && passStella;
    }

    /// <summary>
    /// 필터 초기화
    /// </summary>
    public void ResetFilter()
    {
        _selectedElementFilters.Clear();
        _selectedStellaFilters.Clear();
        RefreshFilteredList();
        UpdateData();
    }

    #endregion

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