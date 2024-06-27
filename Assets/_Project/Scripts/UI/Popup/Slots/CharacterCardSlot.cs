using System.Collections.Generic;
using Coffee.UIEffects;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterCardSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _characterCardButton;

        [Header("BG Layer")]
        [SerializeField] private GameObject _lockBGLayerObject;
        [SerializeField] private GameObject _normalBGLayerObject;
        [SerializeField] private GameObject _SSRBGLayerObject;

        [Header("Character Info")]
        [SerializeField] private GameObject _characterImageParentObject;
        [SerializeField] private TextMeshProUGUI _chracterLevelText;
        [SerializeField] private Image _gradeImage;
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private SynergyUI _positionSynergyUI;

        [Space(10)]
        [SerializeField] private GameObject _shadowObject;
        [SerializeField] private GameObject _outlineObject;
        [SerializeField] private GameObject _outlineActiveObject;
        [SerializeField] private GameObject _outlineInactiveObject;

        [Space(10)]
        [SerializeField] private List<GameObject> _starObjectList;

        [Header("Character Info - Slider")]
        [SerializeField] private Image _characterSliderImage;
        [SerializeField] private TextMeshProUGUI _characterSliderText;


        private SpecCharacter _specCharacterData;
        private UserCharacter _userCharacterData;

        private CharacterCollectionPopup _parentCollectionPopup;

        public SpecCharacter SpecCharacterData => _specCharacterData;

        private void Awake()
        {
            _characterCardButton.onClick.AddListener(OnClickCardSlot);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _characterCardButton.onClick.RemoveListener(OnClickCardSlot);
        }

        public void SetCharcacterSlot(SpecCharacter characterData, CharacterCollectionPopup _parentPopup)
        {
            if (characterData == null) return;

            ClearCardSlot();

            _parentCollectionPopup = _parentPopup;

            _specCharacterData = characterData;
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(_specCharacterData.character_id);

            bool haveCharacter = UserDataManager.Instance.IsHaveCharacter(_specCharacterData.character_id);

            // 기본 데이터 관련 세팅
            string characterPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            var newObject = AddressablesUtil.Instantiate(characterPrefabName, _characterImageParentObject.transform);

            _gradeImage.sprite = ImageManager.Instance.GetGradeTypeSprite(_specCharacterData.grade_type, haveCharacter);

            _synergyUI.SetSynergyUI(_specCharacterData.element_type, haveCharacter);
            _positionSynergyUI.SetPositionSynergyUI(_specCharacterData.character_position_type, haveCharacter);

            _chracterLevelText.gameObject.SetActive(haveCharacter);
            if (haveCharacter)
            {
                _chracterLevelText.text = _userCharacterData.Level.ToString();
            }

            if (newObject != null)
            {
                // newObject.GetComponent<UIEffectController>()?.SetUIEffectMode(haveCharacter ? EffectMode.None : EffectMode.Grayscale);
                newObject.GetComponentInChildren<UIEffect>().effectMode = haveCharacter ? EffectMode.None : EffectMode.Grayscale;

                if (!haveCharacter)
                {
                    var newColor = BMUtil.ChangeColorAlpha(newObject.GetComponentInChildren<Image>().color, 0.46f);
                    newObject.GetComponentInChildren<Image>().color = newColor;
                }
            }

            SetStarObject(_specCharacterData.grade_type, haveCharacter);

            // 캐릭터 조각 슬라이더 관련 처리
            _characterSliderText.text = $"{_userCharacterData.CharacterPiece}/{_specCharacterData.need_piece}";
            _characterSliderImage.fillAmount = (float)_userCharacterData.CharacterPiece / _specCharacterData.need_piece;

            // 캐릭터 보유 여부 관련 처리
            _shadowObject.SetActive(haveCharacter);

            _outlineObject.SetActive(haveCharacter);
            _outlineActiveObject.SetActive(haveCharacter);
            _outlineInactiveObject.SetActive(!haveCharacter);

            // BG Layer 세팅
            _lockBGLayerObject.SetActive(!haveCharacter);
            _normalBGLayerObject.SetActive(haveCharacter && _specCharacterData.grade_type != GradeType.LEGEND);
            _SSRBGLayerObject.SetActive(haveCharacter && _specCharacterData.grade_type == GradeType.LEGEND);
        }

        private void SetStarObject(GradeType gradeType, bool isHaveCharacter)
        {
            for (int i = 0; i < _starObjectList.Count; i++)
            {
                _starObjectList[i].SetActive(i <= (int)gradeType);
                _starObjectList[i].GetComponent<CharacterGradeStar>().SetStar(isHaveCharacter);
            }
        }

        private void OnClickCardSlot()
        {
            if (_parentCollectionPopup == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            // 캐릭터 조각 20개 이상 보유 하여 최초 획득 시 처리
            if (UserDataManager.Instance.IsHaveCharacter(_userCharacterData.CharacterId) == false)
            {
                if (_userCharacterData.CharacterPiece >= _specCharacterData.need_piece)
                {
                    RewardItem newCharacter = new RewardItem(ItemType.CHARACTER, _userCharacterData.CharacterId, 1);
                    List<RewardItem> rewardList = new List<RewardItem> { newCharacter };

                    SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(rewardList).Forget();

                    UserDataManager.Instance.AddNewCharacter(_userCharacterData.CharacterId);
                    UserDataManager.Instance.DecreaseKnightPieceCount(_userCharacterData.CharacterId, _specCharacterData.need_piece);

                    _parentCollectionPopup.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN);

                    return;
                }
            }

            // 상세정보창 진입
            _parentCollectionPopup.SelectCharacterCard(_specCharacterData.character_id);
        }

        private void ClearCardSlot()
        {
            //_starObjectList?.ForEach(star => star.SetActive(false));

            BMUtil.RemoveChildObjects(_characterImageParentObject.transform);
        }
    }
}
