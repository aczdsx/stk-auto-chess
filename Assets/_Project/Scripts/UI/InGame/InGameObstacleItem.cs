using System;
using System.Linq;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InGameObstacleItem : MonoBehaviour
{
    private const float LONG_PRESS_DURATION = 0.5f;

    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _body;
    [SerializeField] private GameObject _emptySlotObj;
    [SerializeField] private GameObject _focusObj;
    [SerializeField] private Image _focusImage;
    [SerializeField] private Animation _dropFxAnimation;
    [SerializeField] private TextMeshProUGUI _focusText;

    private Action<SpecObstacle> _onSelected;

    private InGameBottomUI _parentUI;
    private SpecObstacle _specObstacle;

    // 롱탭 기능 관련
    private bool _isShowLongPressFunc = false;
    private bool _isPressing = false;
    private float _pressTime;

    public void SetData(InGameBottomUI parent, SpecObstacle specObstacle, Action<SpecObstacle> onSelected)
    {
        _parentUI = parent;
        _specObstacle = specObstacle;
        bool isExsist = _specObstacle != null;

        _body.SetActive(isExsist);
        _emptySlotObj.SetActive(!isExsist);
        if (_body.activeSelf)
        {
            _image.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(specObstacle.obstacle_id); // [TODO] 장애물 스프라이트로 변경
        }
        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        if (_specObstacle != null && !_isShowLongPressFunc)
            _onSelected.Invoke(_specObstacle);

        _isShowLongPressFunc = false;
    }

    public void SetFocusCharacter(SpecCharacter spec)
    {
        bool isActiveFocus = spec != null;
        if (isActiveFocus)
        {
            _focusImage.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(spec.prefab_id);
            _focusText.text = UserDataManager.Instance.GetUserCharacter(spec.character_id).Level.ToString("n0");
        }
        _focusObj.SetActive(isActiveFocus);
    }

    public void PlayDropFx()
    {
        _dropFxAnimation.Play();
    }
}
