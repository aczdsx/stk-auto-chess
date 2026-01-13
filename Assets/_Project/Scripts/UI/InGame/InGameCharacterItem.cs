using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;

public class InGameCharacterItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private const float LONG_PRESS_DURATION = 0.5f;

    public CharacterStatData StatData => _statData;
    public bool IsFocusSlot => _focusObj.activeSelf;
    [SerializeField] private Image _image;
    [SerializeField] private Image _SynergyImage;
    [SerializeField] private Image _SynergyClassImage;
    [SerializeField] private SpriteLoader _imageSpriteLoader;
    [SerializeField] private SpriteLoader _SynergyImageSpriteLoader;
    [SerializeField] private SpriteLoader _SynergyClassImageSpriteLoader;
    [SerializeField] private TextMeshProUGUI _lvText;
    [SerializeField] private GameObject _body;
    [SerializeField] private GameObject _emptySlotObj;
    [SerializeField] private GameObject _focusObj;
    [SerializeField] private Image _focusImage;
    [SerializeField] private SpriteLoader _focusImageSpriteLoader;
    [SerializeField] private Animation _dropFxAnimation;
    [SerializeField] private TextMeshProUGUI _focusText;
    [SerializeField] private ParticleSystem _guideFx;
    [SerializeField] private TextMeshProUGUI _characterPositionTypeText;

    private Action<CharacterStatData> _onSelected;
    private CharacterStatData _statData;

    private InGameBottomUI _parentUI;

    // 롱탭 기능 관련
    private bool _isShowLongPressFunc = false;
    private bool _isPressing = false;
    private float _pressTime;

    public void SetData(InGameBottomUI parent, CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _parentUI = parent;
        _statData = characterStat;
        bool isExsist = _statData != null;

        _body.SetActive(isExsist);
        _emptySlotObj.SetActive(!isExsist);
        if (_body.activeSelf)
        {//14663506
            _imageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(_statData.Spec.prefab_id)).Forget();
            _SynergyImageSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_statData.Spec.character_element_type)).Forget();
            _SynergyClassImageSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_statData.Spec.character_stella_type)).Forget();
            _characterPositionTypeText.text = _statData.Spec.character_position_type.ToString();
            _lvText.text = $"{_statData.Level}";
        }
        else
        {
            _lvText.text = $"0";
        }

        if (_guideFx)
            _guideFx.gameObject.SetActive(false);

        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        if (_statData != null && !_isShowLongPressFunc)
        {
            if (_guideFx != null)
            {
                if (_guideFx.gameObject.activeSelf && _statData.CharacterID == 130301)
                {
                    // var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(SynergyType.WATER);
                    // if (specSynergyDataList != null && specSynergyDataList.Count > 0)
                    // {
                    //     var filteredSynergyDataList = specSynergyDataList.Where(l => l.grade != 0).ToList();
                    //     SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipInGamePopup>((filteredSynergyDataList, 2, specSynergyDataList[1], specSynergyDataList[2])).Forget();
                    // }
                }
            }

            _onSelected.Invoke(_statData);
        }

        _isShowLongPressFunc = false;
    }

    public void SetFocusCharacter(CharacterInfo spec)
    {
        bool isActiveFocus = spec != null;
        if (isActiveFocus)
        {
            var userCharacter = ServerDataManager.Instance.Character.GetCharacter(spec.id);
            _focusImageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(spec.prefab_id)).Forget();
            _focusText.text = userCharacter?.Level.ToString("n0") ?? "0";
            _lvText.text = userCharacter?.Level.ToString("n0") ?? "0";
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
        if (_guideFx)
        {
            _guideFx.gameObject.SetActive(true);
            _guideFx.Play();
        }
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