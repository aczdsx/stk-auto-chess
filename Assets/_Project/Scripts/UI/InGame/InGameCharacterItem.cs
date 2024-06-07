using System;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameCharacterItem : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Image _SynergyImage;
    [SerializeField] private Image _SynergyClassImage;
    [SerializeField] private TextMeshProUGUI _lvText;
    [SerializeField] private GameObject _body;
    private Action<CharacterStatData> _onSelected;
    private CharacterStatData _statData;

    public void SetData(CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _statData = characterStat;
        _body.SetActive(_statData != null);
        if (_body.activeSelf)
        {
            _image.sprite = ImageManager.Instance.GetCharacterSubIllustSprite(_statData.Spec.prefab_id);
            _SynergyImage.sprite = ImageManager.Instance.GetSynergySprite(_statData.Spec.element_type);
            _SynergyClassImage.sprite = ImageManager.Instance.GetClassSprite(_statData.Spec.character_position_type);
            _lvText.text = $"{_statData.Level}";
        }
        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        if (_statData != null)
            _onSelected.Invoke(_statData);
    }
}
