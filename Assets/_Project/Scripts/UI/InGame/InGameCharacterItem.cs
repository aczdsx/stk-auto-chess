using System;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameCharacterItem : MonoBehaviour
{
    public CharacterStatData StatData => _statData;
    public bool IsFocusSlot => _focusObj.activeSelf;
    [SerializeField] private Image _image;
    [SerializeField] private Image _SynergyImage;
    [SerializeField] private Image _SynergyClassImage;
    [SerializeField] private TextMeshProUGUI _lvText;
    [SerializeField] private GameObject _body;
    [SerializeField] private GameObject _emptySlotObj;
    [SerializeField] private GameObject _focusObj;
    [SerializeField] private Image _focusImage;
    [SerializeField] private Animation _dropFxAnimation;
    private Action<CharacterStatData> _onSelected;
    private CharacterStatData _statData;

    public void SetData(CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _statData = characterStat;
        bool isExsist = _statData != null;

        _body.SetActive(isExsist);
        _emptySlotObj.SetActive(!isExsist);
        if (_body.activeSelf)
        {
            _image.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(_statData.Spec.prefab_id);
            _SynergyImage.sprite = ImageManager.Instance.GetSynergySprite(_statData.Spec.element_type);
            _SynergyClassImage.sprite = ImageManager.Instance.GetPositionSprite(_statData.Spec.character_position_type);
            _lvText.text = $"{_statData.Level}";
        }
        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        if (_statData != null)
            _onSelected.Invoke(_statData);
    }

    public void SetFocusCharacter(int prefabID = 0)
    {
        bool isActiveFocus = prefabID != 0;
        if (isActiveFocus)
            _focusImage.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(prefabID);
        _focusObj.SetActive(isActiveFocus);
    }

    public void PlayDropFx()
    {
        _dropFxAnimation.Play();
    }
}
