using System.Collections;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CommanderSkillUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CommanderSkillInGameData Data => _data;

    [SerializeField] private Image _iconImage;
    [SerializeField] private SpriteLoader _iconSpriteLoader;
    [SerializeField] private Image _coolTimeImage;
    [SerializeField] private TextMeshProUGUI _coolTimeText;
    [SerializeField] private GameObject _guideObj;
    [SerializeField] private ParticleSystem _activeFx;
    [SerializeField] private GameObject _autoObj;
    [SerializeField] private GameObject _autoActiveObj;

    private CommanderSkillInGameData _data;
    private Pref _preference;

    public void OnClickAuto()
    {
        if (_autoObj != null && _autoObj.activeSelf)
        {
            bool isActive = !_autoActiveObj.activeSelf;
            Preference.SavePreference(_preference, isActive);
            SetActiveAuto(isActive);
        }
    }

    public void SetIcon(string spriteName)
    {
        _iconSpriteLoader.SetSprite(spriteName).Forget();
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

    public void SetData(CommanderSkillInGameData data, Pref pref)
    {
        _data = data;
        _preference = pref;

        bool isActiveAuto = Preference.LoadPreference(_preference, false);
        SetActiveAuto(isActiveAuto);

        var guideMission = ServerDataManager.Instance.GuideMission;
        bool _isCanAutoSkill = guideMission.GuideMissionId >= 28;

        _autoObj.SetActive(_isCanAutoSkill);
    }

    public void SetActiveAuto(bool isActive)
    {
        _autoActiveObj.SetActive(isActive);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InGameCommanderManager.Instance != null)
        {
            InGameCommanderManager.Instance.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InGameCommanderManager.Instance != null)
        {
            InGameCommanderManager.Instance.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InGameCommanderManager.Instance != null)
        {
            InGameCommanderManager.Instance.OnEndDrag(eventData);
        }
    }


}
