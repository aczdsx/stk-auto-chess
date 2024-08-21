using System;
using System.Linq;
using CookApps.AutoBattler;
using Cookapps.Stkauto.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InGameCharacterItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private const float LONG_PRESS_DURATION = 0.5f;

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
    [SerializeField] private TextMeshProUGUI _focusText;
    [SerializeField] private ParticleSystem _guideFx;

    private Action<CharacterStatData> _onSelected;
    private CharacterStatData _statData;

    private InGameBottomUI _parentUI;

    // 롱탭 기능 관련
    private bool _isShowLongPressFunc = false;
    private bool _isPressing = false;
    private float _pressTime;

    public void SetData(InGameBottomUI parent, CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _guideFx.gameObject.SetActive(false);
        _parentUI = parent;
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
        else
        {
            _lvText.text = $"0";
        }
        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        if (_statData != null && !_isShowLongPressFunc)
            _onSelected.Invoke(_statData);

        _isShowLongPressFunc = false;
    }

    public void SetFocusCharacter(SpecCharacter spec)
    {
        bool isActiveFocus = spec != null;
        if (isActiveFocus)
        {
            var userCharacter = UserDataManager.Instance.GetUserCharacter(spec.character_id);
            _focusImage.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(spec.prefab_id);
            _focusText.text = userCharacter.Level.ToString("n0");
            _lvText.text = userCharacter.Level.ToString("n0");
        }
        else
        {
            _focusText.text = "0";
            _lvText.text = "0";
        }
        _focusObj.SetActive(isActiveFocus);
    }

    public void PlayDropFx()
    {
        _dropFxAnimation.Play();
    }

    // 롱탭 동작 시 실행할 함수
    public void OnLongPress()
    {
        Debug.Log("######## Long Pressed ########");

        InGameMain.GetInGameMain().ShowSKillTooltip(_statData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressing = true;
        _pressTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressing = false;

        InGameMain.GetInGameMain().CloseSkillTooltip();
    }

    public int GetDisplayLv()
    {
        int level;
        if (int.TryParse(_lvText.text, out level))
        {
            return level;
        }
        else
        {
            return 0; 
        }
    }
    
    public void SetAlert()
    {
        _guideFx.gameObject.SetActive(true);
        _guideFx.Play();
    }

    void Update()
    {
        if (_isPressing && (Time.time - _pressTime) >= LONG_PRESS_DURATION)
        {
            _isShowLongPressFunc = true;
            _isPressing = false;
            OnLongPress();
        }
    }
}
