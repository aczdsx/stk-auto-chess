using System.Collections;
using System.Collections.Generic;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CommanderSkillUI : MonoBehaviour
{
    public CommanderSkillData Data => _data;

    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _coolTimeImage;
    [SerializeField] private TextMeshProUGUI _coolTimeText;
    [SerializeField] private GameObject _guideObj;
    [SerializeField] private ParticleSystem _activeFx;
    [SerializeField] private GameObject _autoObj;
    [SerializeField] private GameObject _autoActiveObj;

    private CommanderSkillData _data;
    private Pref _preference;

    public void OnClickAuto()
    {
        bool isActive = !_autoActiveObj.activeSelf;
        Preference.SavePreference(_preference, isActive);
        SetActiveAuto(isActive);
    }

    public void SetIcon(Sprite sprite)
    {
        _iconImage.sprite = sprite;
        _iconImage.color = Color.white;
    }

    public void UpdateCommanderSkillCoolTime()
    {
        float rate = _data.ElapsedTime / _data.DurationTime;
        _coolTimeText.text = _data.ElapsedTime >= _data.DurationTime ? "" : $"{_data.DurationTime - _data.ElapsedTime:F1}";
        _coolTimeImage.fillAmount = 1 - rate;

        _guideObj.SetActive(rate >= 1);
    }

    public void SetIconColor(float fadeAlpha)
    {
        if (_iconImage != null)
        {
            Color color = _iconImage.color;
            color.a = fadeAlpha;
            _iconImage.color = color;
        }
    }

    public void SetCommanderFx(bool isActive)
    {
        _activeFx.gameObject.SetActive(isActive);
        if (isActive)
            _activeFx.Play();
    }

    public void SetData(CommanderSkillData data, Pref pref)
    {
        _data = data;
        _preference = pref;

         bool isActiveAuto = Preference.LoadPreference(_preference, false);
        SetActiveAuto(isActiveAuto);
        _autoObj.SetActive(true);
    }

    public void SetAutoObj(bool isActive)
    {
        _autoActiveObj.SetActive(isActive);
    }
    
    public void SetActiveAuto(bool isActive)
    {
        _autoActiveObj.SetActive(isActive);
    }
}
