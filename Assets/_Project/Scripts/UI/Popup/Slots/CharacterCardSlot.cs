using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Image = UnityEngine.UI.Image;

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
        [SerializeField] private GameObject _SRBGLayerObject;
        [SerializeField] private GameObject _SSRBGLayerObject;

        [Header("Character Info")]
        [SerializeField] private GameObject _characterImageParentObject;
        [SerializeField] private TextMeshProUGUI _chracterLevelText;
        [SerializeField] private Image _gradeImage;
        [SerializeField] private SpriteLoader _gradeSpriteLoader;
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private SynergyUI _positionSynergyUI;
        [SerializeField] private TextMeshProUGUI _characterNameText;

        [Space(10)]
        [SerializeField] private GameObject _lvBox;

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
        private List<CharacterInfo> _filteredList;
        private AsyncOperationHandle<GameObject> _loadHandle;
        private readonly List<int> _reusableIdList = new List<int>();

        public CharacterInfo SpecCharacterData => _specCharacterData;

        private void Awake()
        {
            _characterCardButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCardSlot()).AddTo(this);

        }

        protected override void OnDestroy()
        {
            ReleaseCharacterImage();
        }

        public void SetCharcacterSlot(CharacterInfo characterData, List<CharacterInfo> filteredList)
        {
            if (characterData == null) return;

            _filteredList = filteredList;

            ClearCardSlot();

            _specCharacterData = characterData;
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(_specCharacterData.id);

            bool haveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);

            // 공통 세팅
            LoadCharacterImage(_specCharacterData.prefab_id, haveCharacter).Forget();
            _gradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specCharacterData.grade_type, haveCharacter)).Forget();
            _synergyUI.SetSynergyUI(_specCharacterData.character_element_type, haveCharacter);
            _positionSynergyUI.SetSynergyUI(_specCharacterData.character_stella_type, haveCharacter);

            // 캐릭터 이름
            if (_characterNameText != null)
                _characterNameText.text = LanguageManager.Instance.GetDefaultText(_specCharacterData.name_token);

            // 캐릭터 조각 슬라이더
            SetPieceSlider();

            // BG Layer 세팅
            SetBGLayer(haveCharacter);

            if (haveCharacter)
                SetOwnedState();
            else
                SetUnownedState();

            // 가이드 알림 세팅
            SetGuideAlert();
        }

        private async UniTaskVoid LoadCharacterImage(int prefabId, bool haveCharacter)
        {
            ReleaseCharacterImage();

            string characterPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, prefabId);
            var handle = Addressables.InstantiateAsync(characterPrefabName, _characterImageParentObject.transform);
            _loadHandle = handle;
            await handle;

            // 재활용으로 인한 경쟁 조건 방지: await 사이에 새 로드가 시작되었으면 이전 결과 폐기
            if (!_loadHandle.Equals(handle))
            {
                if (handle.IsValid())
                    Addressables.ReleaseInstance(handle);
                return;
            }

            var newObject = handle.Result;
            if (newObject != null)
            {
                UICharacter uiCharacter = newObject.GetComponent<UICharacter>();
                if (uiCharacter != null)
                {
                    uiCharacter.SetGrayCharacter(!haveCharacter);
                    Color targetColor = haveCharacter ? Color.white : _fadeCharacterColor;
                    uiCharacter.SetCharacterImageColor(targetColor);
                }
            }
        }

        private void ReleaseCharacterImage()
        {
            if (_loadHandle.IsValid())
            {
                Addressables.ReleaseInstance(_loadHandle);
                _loadHandle = default;
            }
        }

        private void SetOwnedState()
        {
            _chracterLevelText.gameObject.SetActive(true);
            _chracterLevelText.text = _userCharacterData.Level.ToString();

            SetStarObject((int)(_userCharacterData?.TranscendLevel ?? 0), true);
            _lvBox.SetActive(true);

            // 초월 가능 뱃지
            if (_transcendenceBadge != null)
            {
                _transcendenceBadge.Clear();
                _transcendenceBadge.AddBadgePath(BadgeType.RedDot, PlayerDataModel.GetTranscendenceBadgePath(_specCharacterData.id));
            }
        }

        private void SetUnownedState()
        {
            _chracterLevelText.gameObject.SetActive(false);

            SetStarObject(0, false);
            _lvBox.SetActive(false);

            if (_transcendenceBadge != null)
                _transcendenceBadge.Clear();
        }

        private void SetBGLayer(bool haveCharacter)
        {
            _lockBGLayerObject.SetActive(!haveCharacter);

            var grade = _specCharacterData.grade_type;
            bool isNormal = haveCharacter && grade == GradeType.RARE;
            bool isSR     = haveCharacter && grade == GradeType.EPIC;
            bool isSSR    = haveCharacter && grade == GradeType.LEGENDARY;

            _normalBGLayerObject.SetActive(isNormal);
            if (_SRBGLayerObject != null) _SRBGLayerObject.SetActive(isSR);
            _SSRBGLayerObject.SetActive(isSSR);
        }

        private void SetPieceSlider()
        {
            var specCharacterTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                _specCharacterData.grade_type, (int)(_userCharacterData?.TranscendLevel ?? 0));

            if (specCharacterTranscendenceData != null)
            {
                ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);
                int characterPiece = (int)ServerDataManager.Instance.Inventory.GetCurrency(pieceItemId);
                _characterSliderText.text = $"{characterPiece}/{specCharacterTranscendenceData.piece}";
                _characterSliderImage.fillAmount = (float)characterPiece / specCharacterTranscendenceData.piece;
            }
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

            // 필터링된 데이터 리스트에서 직접 ID 수집
            _reusableIdList.Clear();
            if (_filteredList != null)
            {
                foreach (var c in _filteredList)
                    _reusableIdList.Add(c.id);
            }

            // 상세정보창 진입
            var param = new CharacterDetailPopupParam(_specCharacterData.id, _reusableIdList);
            SceneUILayerManager.Instance.PushUILayerAsync<CharacterDetailMainLayerPopup>(param).Forget();
        }

        public void ClearCardSlot()
        {
            ReleaseCharacterImage();
            BMUtil.RemoveChildObjects(_characterImageParentObject.transform);
        }
    }
}
