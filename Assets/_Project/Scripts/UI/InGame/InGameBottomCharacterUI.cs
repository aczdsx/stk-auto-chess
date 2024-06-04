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
    [SerializeField] private Transform _characterSelectedTransform;
    [SerializeField] private Transform _rightTransform;
    [SerializeField] private Image _returnImage;
    [SerializeField] private List<InGameCharacterItem> _characterItemList;

    private List<CharacterStatData> _characterStats;
    private Action _onNewCharacter;
    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        HideCharacterSelectUI(() =>
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageStart>();
        });
    }

    public void InitData(Action onNewCharacter)
    {
        _onNewCharacter = onNewCharacter;

        // [TODO] 더미 요소 나중에 제거 필요
        _characterStats = new List<CharacterStatData>();
        _characterStats.Add(new CharacterStatData(40101, 10));
        _characterStats.Add(new CharacterStatData(30601, 10));
        _characterStats.Add(new CharacterStatData(40402, 10));

        var userCharacters = UserDataManager.Instance.GetAllUserCharacters();
        foreach (var character in userCharacters)
        {
            _characterStats.Add(new CharacterStatData(character.CharacterId, character.Level));
        }

        UpdateData();
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
        Debug.Log($"AddBoardCharacter: {statData.CharacterId}");
        var ingameTile = InGameObjectManager.Instance.InGameGrid.GetEmptyTile();
        int2 pos = new int2(ingameTile.X, ingameTile.Y);

        await UniTask.WhenAll(new[]
        {
            InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Player,
                typeof(CharacterStateReady)),
        });

        _characterStats.RemoveAll(l => l.CharacterId == statData.CharacterId);

        UpdateData();
        _onNewCharacter.Invoke();
    }
}
