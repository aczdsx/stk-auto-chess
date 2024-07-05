using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameBottomCharacterUI : MonoBehaviour
{
    [SerializeField]
    private CAButton _startButton;

    [SerializeField]
    private CAButton _CommanderSkillButton;

    [SerializeField]
    private CAButton _statisticButton;

    [SerializeField]
    private Transform _characterSelectedTransform;

    [SerializeField]
    private Transform _rightTransform;

    [SerializeField]
    private Image _returnImage;

    [SerializeField]
    private InGameCharacterItem _ingameCharacterItemPrefab;

    [SerializeField]
    private Transform _inGameCharacterItemTransform;

    [SerializeField]
    private GameObject _readyUIObj;

    [SerializeField]
    private CommanderSkillUI _commanderSkillUI;

    [SerializeField]
    private TextMeshProUGUI _characterCountText;

    [SerializeField]
    private ParticleSystem _commanderFx;

    [SerializeField]
    private SkillTooltipPopup _skillTooltipPopup;

    private List<InGameCharacterItem> _characterItemList = new List<InGameCharacterItem>();
    private List<CharacterStatData> _characterStats;
    private bool _isRunningAddCharacter;
    private bool _isOpenCommanderSkill;

    protected void Awake()
    {
        _isOpenCommanderSkill = InGameResourceHolder.Chapter >= SpecDataManager.Instance.GetFirstCommanderSkillChapter();
        _commanderSkillUI.gameObject.SetActive(_isOpenCommanderSkill);

        _startButton?.onClick.AddListener(OnStartButtonClicked);
        _CommanderSkillButton?.onClick.AddListener(OnClickCommanderSkillButton);
        _statisticButton?.onClick.AddListener(OnClickStatisticButton);
    }

    private void OnStartButtonClicked()
    {
        // 전투 인원 0명 검사
        if (InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count == 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_CHAR_NOT_SET");
            return;
        }

        // 전투 인원 최대 인원 미배치 검사
        var userLevelData = SpecDataManager.Instance.SpecAccountLevelExp.Get(UserDataManager.Instance.UserBasicData.Level);
        if (InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count < userLevelData.squad_count)
        {
            bool isAvailableCharacter = _characterItemList.Exists(l => l.StatData != null);
            if (isAvailableCharacter)
            {
                string contentText = LanguageManager.Instance.GetLanguageText("SYSTEM_MSG_MAX_CHARACTER_ALERT");

                SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
                newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", StartInGameBattle);

                SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();

                return;
            }
        }

        // 지휘자 스킬 장착 확인
        if (_isOpenCommanderSkill)
        {
            var equippedCommanderSkill = UserDataManager.Instance.GetEquippedCommanderSkill();
            if (equippedCommanderSkill == 0)
            {
                string contentText = LanguageManager.Instance.GetLanguageText("MSG_ALERT_EQUIP_COMMAND_SKILL");

                SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
                newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", StartInGameBattle);

                SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();

                return;
            }
        }

        StartInGameBattle();
    }

    private void StartInGameBattle()
    {
        _readyUIObj.SetActive(false);

        InGameMainFlowManager.Instance.AddNextState<FlowStateStageStart>();
        SetCommanderSkill();
        InGameMain.GetInGameMain().SetCombatUI();


        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

        // HideCharacterSelectUI(() =>
        // {
        // });
    }

    private void OnClickStatisticButton()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(this).Forget();

        Preference.SavePreference(Pref.STATISTIC, true);
    }

    private void OnClickCommanderSkillButton()
    {
        if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)
            return;

        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

        SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>().Forget();
    }

    public void ChangeStatisticsButtonActiveState(bool isOn)
    {
        _statisticButton.gameObject.SetActive(isOn);
    }

    public void AddCharacter(List<UserCharacterBattleDeck> battleDeckList)
    {
        foreach (var battleDeck in battleDeckList)
            _characterStats.RemoveAll(l => l.CharacterId == battleDeck.CharacterId);
        UpdateData();
        InGameMain.GetInGameMain().SetInGameTopUI();
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
            bool isExist = _characterItemList.Exists(l => l.StatData != null && l.StatData.CharacterId == characterStat.CharacterId);
            if (!isExist)
            {
                var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                _characterItemList.Add(characterItem);
                characterItem.SetData(this, characterStat, AddCharacterToTile);
                _characterStats.Add(characterStat);
            }
        }
    }

    public void SetCommanderSkillUI(int id)
    {
        var image = ImageManager.Instance.GetCommanderSkillSprite(id);
        if (image != null)
        {
            _commanderSkillUI.SetIcon(image);
            InGameMain.GetInGameMain().SetCommanderFx(false);
        }
        else
        {
            InGameMain.GetInGameMain().SetCommanderFx(true);
        }

    }

    public void InitData()
    {
        _characterItemList.Clear();
        BMUtil.RemoveChildObjects(_inGameCharacterItemTransform);
        _characterStats = new List<CharacterStatData>();
        // _characterStats.Add(new CharacterStatData(130201, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(130601, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(130402, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(140101, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(140402, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(140103, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        //
        // _characterStats.Add(new CharacterStatData(130301, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(140301, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(140601, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(140502, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        // _characterStats.Add(new CharacterStatData(130501, 1, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));

        SetCommanderSkillUI(UserDataManager.Instance.UserCommanderSkillData.EquippedCommanderSkillId);
        var userCharacters = UserDataManager.Instance.GetAllUserCharacterList();
        foreach (var character in userCharacters)
        {
            _characterStats.Add(new CharacterStatData(character.CharacterId, character.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        }

        foreach (var characterStat in _characterStats)
        {
            bool isExist = _characterItemList.Exists(l => l.StatData != null && l.StatData.CharacterId == characterStat.CharacterId);
            if (!isExist)
            {
                var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                _characterItemList.Add(characterItem);
                characterItem.SetData(this, characterStat, AddCharacterToTile);
            }
        }
    }

    public void SetCommanderSkill()
    {
        int equippedCommanderSkill = UserDataManager.Instance.GetEquippedCommanderSkill();
        if (equippedCommanderSkill != 0)
        {
            var data = SpecDataManager.Instance.GetCommanderSkillData(equippedCommanderSkill);
            InGameCommanderManager.Instance.SetCommanderSkillData(data);
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

    public void ShowSKillTooltip(CharacterStatData statData)
    {
        if (statData == null) return;
        if (_skillTooltipPopup == null) return;

        var specSkillList = SpecDataManager.Instance.GetSkillDataListByPrefabID(statData.Spec.prefab_id);
        if (specSkillList != null && specSkillList.Count > 0)
        {
            var skillData = specSkillList[0];

            _skillTooltipPopup.gameObject.SetActive(true);

            _skillTooltipPopup.SetSkillToolTipPopup(skillData);
        }
    }

    public void CloseSkillTooltip()
    {
        _skillTooltipPopup?.gameObject.SetActive(false);
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

            InGameMain.GetInGameMain().SetInGameTopUI();
            SetCharacterCountText();
        }

        _isRunningAddCharacter = false;
    }

    public void SetCommanderSkillCoolTime(float elapsedTime, float durationTime)
    {
        _commanderSkillUI.UpdateCommanderSkillCoolTime(elapsedTime, durationTime);
    }

    public void SetIconColor(float fadeAlpha)
    {
        _commanderSkillUI.SetIconColor(fadeAlpha);
    }

    public void SetCharacterCountText()
    {
        int characterCount = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count;
        int maximumCount = SpecDataManager.Instance.SpecAccountLevelExp
            .Get(UserDataManager.Instance.UserBasicData.Level).squad_count;

        string colorCode = characterCount == 0 ? "#CA6E71" : "#C5C5B2";
        _characterCountText.text = $"<color={colorCode}>{characterCount}</color>/{maximumCount}";
    }

    public void SetCommanderFx(bool isActive)
    {
        _commanderFx.gameObject.SetActive(isActive);
        if (isActive)
            _commanderFx.Play();
    }

    public void SetFocusCharacter(SpecCharacter spec)
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

    public void UnSetFocusCharacter(bool isDropFx)
    {
        foreach (var characterItem in _characterItemList)
        {
            if (characterItem.IsFocusSlot)
            {
                characterItem.SetFocusCharacter(null);
                if(isDropFx)
                    characterItem.PlayDropFx();
                return;
            }
        }
    }

    public void OpenStatisticPop()
    {
        bool isOpenStatisticPop = Preference.LoadPreference(Pref.STATISTIC, false);
        if (isOpenStatisticPop)
            SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(this).Forget();
    }
}
