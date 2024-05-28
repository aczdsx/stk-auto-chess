using System;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameCharacterItem : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _lvText;
    [SerializeField] private GameObject _body;
    private Action<CharacterStatData> _onSelected;
    private CharacterStatData _statData;

    public void SetData(CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _statData = characterStat;
        _body.SetActive(_statData != null);
        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        _onSelected.Invoke(_statData);
        Debug.Log("OnClickItem");
    }
}
