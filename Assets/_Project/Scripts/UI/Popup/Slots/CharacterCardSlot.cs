using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterCardSlot : CachedMonoBehaviour
    {
        [SerializeField] private GuideAlert _characterGuideAlert;

        [Space(10)]
        [SerializeField] private CAButton _characterCardButton;

        [Header("BG Layer")]
        [SerializeField] private GameObject _lockBGLayerObject;
        [SerializeField] private GameObject _normalBGLayerObject;
        [SerializeField] private GameObject _SSRBGLayerObject;

        [Header("Character Info")]
        [SerializeField] private GameObject _characterImageParentObject;
        [SerializeField] private TextMeshProUGUI _chracterLevelText;
        [SerializeField] private Image _gradeImage;
        [SerializeField] private SpriteLoader _gradeSpriteLoader;
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

        [Header("Fade Setting")]
        [SerializeField] private Color _fadeCharacterColor;
        
        [Header("Badge")]
        [SerializeField] private Badge _transcendenceBadge;


        private CharacterInfo _specCharacterData;
        private CharacterData _userCharacterData;
        private List<CharacterCardSlot> _slotList;

        public CharacterInfo SpecCharacterData => _specCharacterData;

        private void Awake()
        {
            _characterCardButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCardSlot()).AddTo(this);
            
        }

        public void SetCharcacterSlot(CharacterInfo characterData, List<CharacterCardSlot> slotList)
        {
            if (characterData == null) return;

            _slotList = slotList;

            ClearCardSlot();

            _specCharacterData = characterData;
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(_specCharacterData.id);

            bool haveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);

            // 기본 데이터 관련 세팅
            string characterPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            var newObject = AddressablesUtil.Instantiate(characterPrefabName, _characterImageParentObject.transform);

            _gradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specCharacterData.grade_type, haveCharacter)).Forget();

            _synergyUI.SetSynergyUI(_specCharacterData.character_element_type, haveCharacter);
            _positionSynergyUI.SetSynergyUI(_specCharacterData.character_stella_type, haveCharacter);

            _chracterLevelText.gameObject.SetActive(haveCharacter);
            if (haveCharacter)
            {
                _chracterLevelText.text = _userCharacterData.Level.ToString();
            }

            if (newObject != null)
            {
                UICharacter uiCharacter = newObject.GetComponent<UICharacter>();
                if (uiCharacter != null)
                {
                    uiCharacter.SetGrayCharacter(!haveCharacter);
                    Color targetColor = haveCharacter ? Color.white : _fadeCharacterColor;
                    uiCharacter.SetCharacterImageColor(targetColor);

                    if (!haveCharacter)
                    {
                        // var newColor = BMUtil.ChangeColorAlpha(newObject.GetComponentInChildren<Image>().color, 0.46f);
                        // newObject.GetComponentInChildren<Image>().color = newColor;
                    }
                    // newObject.GetComponent<UIEffectController>()?.SetUIEffectMode(haveCharacter ? EffectMode.None : EffectMode.Grayscale);
                }
            }

            SetStarObject((int)(_userCharacterData?.TranscendLevel ?? 0), haveCharacter);

            // 캐릭터 조각 슬라이더 관련 처리
            var specCharacterTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(_specCharacterData.grade_type, (int)(_userCharacterData?.TranscendLevel ?? 0));

            if (specCharacterTranscendenceData != null)
            {
                ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);

                int characterPiece =  (int)ServerDataManager.Instance.Inventory.GetCurrency(pieceItemId);
                _characterSliderText.text = $"{characterPiece}/{specCharacterTranscendenceData.piece}";
                _characterSliderImage.fillAmount = (float)characterPiece / specCharacterTranscendenceData.piece;
            }

            // 초월 가능 뱃지 (BadgeManager 경로 구독)
            if (_transcendenceBadge != null)
            {
                _transcendenceBadge.Clear();
                _transcendenceBadge.AddBadgePath(BadgeType.RedDot, PlayerDataModel.GetTranscendenceBadgePath(_specCharacterData.id));
            }

            // 캐릭터 보유 여부 관련 처리
            _shadowObject.SetActive(haveCharacter);

            _outlineObject.SetActive(haveCharacter);
            _outlineActiveObject.SetActive(haveCharacter);
            _outlineInactiveObject.SetActive(!haveCharacter);

            // BG Layer 세팅
            _lockBGLayerObject.SetActive(!haveCharacter);
            _normalBGLayerObject.SetActive(haveCharacter && _specCharacterData.grade_type != GradeType.LEGENDARY);
            _SSRBGLayerObject.SetActive(haveCharacter && _specCharacterData.grade_type == GradeType.LEGENDARY);

            // 가이드 알림 세팅
            SetGuideAlert();
            
        }

        private const int MaxVisibleStars = 5;

        private void SetStarObject(int transcendLevel, bool isHaveCharacter)
        {
            int startIndex = Mathf.Max(0, transcendLevel - MaxVisibleStars);
            for (int i = 0; i < _starObjectList.Count; i++)
            {
                bool isVisible = i >= startIndex && i < transcendLevel;
                _starObjectList[i].SetActive(isVisible);
                _starObjectList[i].GetComponent<CharacterGradeStar>().SetStar(isHaveCharacter);
            }
        }

        private void SetGuideAlert()
        {
            if (_characterGuideAlert == null) return;

            _characterGuideAlert.InitAlertWithSubKey(_specCharacterData.id);
        }

        private void OnClickCardSlot()
        {
            if (_specCharacterData == null) return;

            // 현재 활성화된(필터링 통과한) 슬롯들의 캐릭터 ID 리스트 생성
            List<int> visibleCharacterIds = new List<int>();
            if (_slotList != null)
            {
                foreach (var slot in _slotList)
                {
                    if (slot.gameObject.activeSelf)
                    {
                        visibleCharacterIds.Add(slot.SpecCharacterData.id);
                    }
                }
            }

            // 상세정보창 진입
            var param = new CharacterDetailPopupParam(_specCharacterData.id, visibleCharacterIds);
            SceneUILayerManager.Instance.PushUILayerAsync<CharacterDetailMainLayerPopup>(param).Forget();
        }

        private void ClearCardSlot()
        {
            //_starObjectList?.ForEach(star => star.SetActive(false));

            BMUtil.RemoveChildObjects(_characterImageParentObject.transform);
        }
    }
}
