using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameBottomCharacterUI : MonoBehaviour
{
    [SerializeField] private CAButton _startButton;
    [SerializeField] private CAButton _CommanderSkillButton;
    [SerializeField] private Transform _characterSelectedTransform;
    [SerializeField] private Transform _rightTransform;
    [SerializeField] private Image _returnImage;
    [SerializeField] private InGameCharacterItem _ingameCharacterItemPrefab;
    [SerializeField] private Transform _inGameCharacterItemTransform;

    [SerializeField] private GameObject _readyUIObj;

    [SerializeField] private CommanderSkillUI _commanderSkillUI;

    private List<InGameCharacterItem> _characterItemList = new List<InGameCharacterItem>();
    private List<CharacterStatData> _characterStats;
    private Action _onNewCharacter;
    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnStartButtonClicked);
        _CommanderSkillButton?.onClick.AddListener(OnClickCommanderSkillButton);
    }

    private void OnStartButtonClicked()
    {
        _readyUIObj.SetActive(false);
        HideCharacterSelectUI(() =>
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageStart>();
            SetCommanderSkill();
        });
    }

    private void OnClickCommanderSkillButton()
    {
        SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>().Forget();
    }

    public void InitData(Action onNewCharacter)
    {
        _onNewCharacter = onNewCharacter;

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

        var userCharacters = UserDataManager.Instance.GetAllUserCharacterList();
        foreach (var character in userCharacters)
        {
            _characterStats.Add(new CharacterStatData(character.CharacterId, character.Level, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()));
        }

        foreach (var characterStat in _characterStats)
        {
            bool isExist = _characterItemList.Exists(l => l.StatData.CharacterId == characterStat.CharacterId);
            if (!isExist)
            {
                var characterItem = Instantiate(_ingameCharacterItemPrefab, _inGameCharacterItemTransform);
                _characterItemList.Add(characterItem);
                characterItem.SetData(characterStat, AddCharacterToTile);
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
                _characterItemList[i].SetData(_characterStats[i], AddCharacterToTile);
            }
            else
            {
                _characterItemList[i].SetData(null, null);
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
    }

    private async void AddCharacterToTile(CharacterStatData statData)
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
        _onNewCharacter.Invoke();
    }

    public void SetCommanderSkillUI(float durationTime)
    {
        _commanderSkillUI.UpdateCommanderSkillCoolTime(durationTime);
    }

    public void SetIconColor(float fadeAlpha)
    {
        _commanderSkillUI.SetIconColor(fadeAlpha);
    }
}
