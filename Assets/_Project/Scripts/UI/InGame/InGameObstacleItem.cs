using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;

public class TestObstacle
{
    public int ID;
    // 기타 장애물 속성들...
}

public class InGameObstacleItem : MonoBehaviour
{
    private const float LONG_PRESS_DURATION = 0.5f;

    [SerializeField] private Image _image;
    [SerializeField] private SpriteLoader _imageSpriteLoader;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _body;
    [SerializeField] private GameObject _emptySlotObj;
    [SerializeField] private GameObject _focusObj;
    [SerializeField] private Image _focusImage;
    [SerializeField] private SpriteLoader _focusImageSpriteLoader;
    [SerializeField] private Animation _dropFxAnimation;
    [SerializeField] private TextMeshProUGUI _focusText;

    private Action<TestObstacle> _onSelected;

    private InGameBottomUI _parentUI;
    private TestObstacle _obstacleData;

    // 롱탭 기능 관련
    private bool _isShowLongPressFunc = false;
    private bool _isPressing = false;
    private float _pressTime;

    public void SetData(InGameBottomUI parent, TestObstacle obstacleData, Action<TestObstacle> onSelected)
    {
        _parentUI = parent;
        _obstacleData = obstacleData;
        bool isExsist = _obstacleData != null;

        // 임시처리
        _nameText.text = LanguageManager.Instance.GetLanguageText("OBSTACLE_COMMON_WALL");

        _body.SetActive(isExsist);
        _emptySlotObj.SetActive(!isExsist);
        if (_body.activeSelf)
        {
            _imageSpriteLoader.SetSprite(SpriteNameParser.GetObstacleInGamePortraitSprite(obstacleData.ID)).Forget();
        }
        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        if (_obstacleData != null && !_isShowLongPressFunc)
            _onSelected.Invoke(_obstacleData);

        _isShowLongPressFunc = false;
    }

    public void SetFocusCharacter(CharacterInfo spec)
    {
        bool isActiveFocus = spec != null;
        if (isActiveFocus)
        {
            _focusImageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(spec.prefab_id)).Forget();
            _focusText.text = CookApps.AutoBattler.ServerDataManager.Instance.Character.GetCharacter(spec.character_id)?.Level.ToString("n0") ?? "0";
        }
        _focusObj.SetActive(isActiveFocus);
    }

    public void PlayDropFx()
    {
        _dropFxAnimation.Play();
    }
}
