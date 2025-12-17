using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterDetailGrowLayer : CachedMonoBehaviour
    {
        [SerializeField] private GuideAlert _levelupButtonGuideAlert;
        
        [Header("Stat Info")]
        [SerializeField] private CAButton _detailStatButton;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _battlePointText;
        [SerializeField] private TextMeshProUGUI _attackValueText;
        [SerializeField] private TextMeshProUGUI _hpValueText;
        [SerializeField] private TextMeshProUGUI _apDefText;
        [SerializeField] private TextMeshProUGUI _adDefText;

        [Header("Piece Layer")]
        [SerializeField] private GameObject _pieceLayerObject;
        [SerializeField] private Image _pieceIconImage;
        [SerializeField] private TextMeshProUGUI _pieceAmountText;
        [SerializeField] private Slider _pieceSlider;

        [Header("LevelUp Layer")]
        [SerializeField] private CAButton _activeLevelUpButton;
        [SerializeField] private CAButton _inactiveLevelUpButton;
        [SerializeField] private CAButton _activeResetLevelUpButton;
        [SerializeField] private CAButton _inactiveResetLevelUpButton;
        [SerializeField] private TextMeshProUGUI _resetCountText;
        [SerializeField] private TextMeshProUGUI _inactiveResetCountText;

        [Space(10)]
        [SerializeField] private CurrencyUIItem _baseExpItemCurrencyUIItem;
        [SerializeField] private CurrencyUIItem _secondExpItemCurrencyUIItem;
        [SerializeField] private CurrencyUIItem _goldCurrencyUIItem;

        [Space(10)]
        [SerializeField] private List<ParticleSystem> _levelupEffectObjectList_1;
        [SerializeField] private List<ParticleSystem> _levelupEffectObjectList_2;

        [Header("Transcendence Layer")]
        [SerializeField] private GameObject _transcendenceLayerObject;
        [SerializeField] private CAButton _activeTranscendenceButton;
        [SerializeField] private CAButton _inactiveTranscendenceButton;

        [Space(10)]
        [SerializeField] private CurrencyUIItem _transcendenceItemCurrencyUIItem;

        private UserCharacter _userCharacterData;
        private CharacterInfo _specCharacterData;

        private CharacterLevelExp _specCharacterLevelExpData;
        private CharacterTranscendence _specCharacterTranscendenceData;

        private CharacterStatData _userStatData;

        private CharacterCollectionPopup _parentCollectionPopup;

        private bool _isHaveCharacter = false;
        private bool _isPlayingLevelupEffect = false;

        private int _maxTranscendenceLevel;

        private void Awake()
        {
            _detailStatButton.onClick.AddListener(OnClickDetailStatButton);

            // 레벨업
            _activeLevelUpButton.onClick.AddListener(OnClickLevelupButton);
            _inactiveLevelUpButton.onClick.AddListener(OnClickLevelupButton);
            _activeResetLevelUpButton.onClick.AddListener(OnClickCharacterResetButton);
            _inactiveResetLevelUpButton.onClick.AddListener(OnClickDimmedResetButton);

            // 초월
            _activeTranscendenceButton.onClick.AddListener(OnClickTranscendenceButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _detailStatButton.onClick.RemoveListener(OnClickDetailStatButton);

            // 레벨업
            _activeLevelUpButton.onClick.RemoveListener(OnClickLevelupButton);
            _inactiveLevelUpButton.onClick.AddListener(OnClickLevelupButton);
            _activeResetLevelUpButton.onClick.RemoveListener(OnClickCharacterResetButton);
            _inactiveResetLevelUpButton.onClick.RemoveListener(OnClickDimmedResetButton);

            // 초월
            _activeTranscendenceButton.onClick.RemoveListener(OnClickTranscendenceButton);
        }

        public void InitLayer(CharacterCollectionPopup _parentPopup, int characterID)
        {
            ClearLayer();

            _parentCollectionPopup = _parentPopup;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);

            _isHaveCharacter = UserDataManager.Instance.IsHaveCharacter(characterID);

            // 스탯 표시 처리 
            SetUserStatLayer();

            // 레벨업 기능 관련 처리
            SetLevelupLayer();

            // 초월 기능 관련 처리
            SetTranscendenceLayer();
            SetTranscendencePieceLayer();
            
            // 리셋 기능 관련 처리
            SetLevelResetLayer();
            
            // 가이드 알림 처리
            SetGuideAlert();

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        public void RefreshLayer()
        {
            SetUserStatLayer();
            SetLevelupLayer();
            SetLevelResetLayer();
            
            SetTranscendenceLayer();
            SetTranscendencePieceLayer();   // SetTranscendenceLayer 이후 호출되어야 함
        }

        private void SetUserStatLayer()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            int maxLevel = UserDataManager.Instance.GetCharacterMaxLevel(_userCharacterData.CharacterId);

            int userLevel = Mathf.Max(1, _userCharacterData.Level);

            _userStatData = new CharacterStatData(_userCharacterData.CharacterId, userLevel, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            _levelText.text = $"Lv.{userLevel}/{maxLevel}";
            _battlePointText.text = _userStatData.GetAttrValue().ToString("N0");
            _attackValueText.text = _userStatData.AD.ToString("N0");
            _hpValueText.text = _userStatData.HP.ToString("N0");
            _apDefText.text = _userStatData.APReduce.ToString("N0");
            _adDefText.text = _userStatData.ADReduce.ToString("N0");
        }

        private void SetTranscendencePieceLayer()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;
            if (_specCharacterTranscendenceData == null) return;

            _pieceLayerObject.SetActive(_isHaveCharacter);
            
            if (_isHaveCharacter == false) return;

            _pieceIconImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(_specCharacterData.prefab_id);
            _pieceAmountText.text = $"{_userCharacterData.CharacterPiece}<color=#C4CDE2>/{_specCharacterTranscendenceData.char_transcendence_count}</color>";

            _pieceSlider.maxValue = _specCharacterTranscendenceData.char_transcendence_count;
            _pieceSlider.value = _userCharacterData.CharacterPiece;
        }

        private void SetLevelupLayer()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            // 레벨업 가능 여부 체크
            int maxLevel = UserDataManager.Instance.GetCharacterMaxLevel(_userCharacterData.CharacterId);

            int userLevel = Mathf.Max(1, _userCharacterData.Level);

            // 레벨업에 필요한 자원 정보 세팅
            _specCharacterLevelExpData = SpecDataManager.Instance.GetCharacterLevelExpData(userLevel);
            
            bool isAvailLevelup = _isHaveCharacter && _userCharacterData.Level < maxLevel;
            if (_specCharacterLevelExpData != null)
            {
                bool isEnoughGold =_specCharacterLevelExpData.need_gold <= UserDataManager.Instance.UserWallet.Gold;
                bool isEnoughExpItem = _specCharacterLevelExpData.base_levelup_item_count <= UserDataManager.Instance.UserWallet.CharUserExpItem;
                bool isEnoughExpItem2 = _specCharacterLevelExpData.sec_levelup_item_count <= UserDataManager.Instance.UserWallet.CharUserExpItem2;
                
                isAvailLevelup = isEnoughGold && isEnoughExpItem && isEnoughExpItem2 && isAvailLevelup;
                
                _goldCurrencyUIItem.SetUIItem(ItemType.GOLD, 0, _specCharacterLevelExpData.need_gold, isEnoughGold);
                _baseExpItemCurrencyUIItem.SetUIItem(_specCharacterLevelExpData.base_levelup_item_type, 0, _specCharacterLevelExpData.base_levelup_item_count, isEnoughExpItem);

                bool isNeedSecondExpItem = _specCharacterLevelExpData.sec_levelup_item_count > 0;
                if (isNeedSecondExpItem)
                {
                    _secondExpItemCurrencyUIItem.SetUIItem(_specCharacterLevelExpData.sec_levelup_item_type, _specCharacterData.character_id, _specCharacterLevelExpData.sec_levelup_item_count, isEnoughExpItem2);
                }
                _secondExpItemCurrencyUIItem.gameObject.SetActive(isNeedSecondExpItem);
            }

            _activeLevelUpButton.gameObject.SetActive(isAvailLevelup);
            _inactiveLevelUpButton.gameObject.SetActive(!isAvailLevelup);
        }

        private void SetTranscendenceLayer()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            _transcendenceLayerObject.SetActive(_isHaveCharacter);
            
            if (_isHaveCharacter == false) return;
            
            // 초월 가능 여부 체크
            var transcendenceDataList = SpecDataManager.Instance.GetCharacterTranscendenceDataList(_specCharacterData.character_element_type, _specCharacterData.grade_type);
            _maxTranscendenceLevel = transcendenceDataList.Max(data => data.transcendence_lv);
            // 초월에 필요한 자원 정보 세팅
            _specCharacterTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(_specCharacterData.character_element_type,
            _specCharacterData.grade_type, _userCharacterData.TranscendenceLevel);

            bool isHasPiece = false;
            if (_specCharacterTranscendenceData != null)
            {
                isHasPiece = _userCharacterData.CharacterPiece >= _specCharacterTranscendenceData.char_transcendence_count;
                _transcendenceItemCurrencyUIItem.SetUIItem(_specCharacterTranscendenceData.item_type, _specCharacterData.character_id, _specCharacterTranscendenceData.char_transcendence_count);
            }
            bool isAvailTranscendence = _isHaveCharacter && _userCharacterData.TranscendenceLevel < _maxTranscendenceLevel && isHasPiece;

            _activeTranscendenceButton.gameObject.SetActive(isAvailTranscendence);
            _inactiveTranscendenceButton.gameObject.SetActive(!isAvailTranscendence);
        }

        private void SetLevelResetLayer()
        {
            int maxResetCount = SpecDataManager.Instance.GetGameConfig<int>("character_level_reset_count_daily");
            int resetCount = UserDataManager.Instance.UserBasicData.ResetCharacterCount;

            int resultCount = maxResetCount - resetCount;

            string levelResetString = LanguageManager.Instance.GetLanguageText("UI_LEVEL_RESET");
            _resetCountText.text = $"{levelResetString} <color=#C35B79><b>({resultCount})</b></color>";

            bool isAvailReset = resultCount > 0;
            
            _activeResetLevelUpButton.gameObject.SetActive(isAvailReset);
            _inactiveResetLevelUpButton.gameObject.SetActive(!isAvailReset);

            if (!isAvailReset)
            {
                _inactiveResetCountText.ToString();
                StartCountdown().Forget();
            }
        }

        private void PlayLevelUpEffect()
        {
            _isPlayingLevelupEffect = true;

            _levelupEffectObjectList_1.ForEach(effect =>
            {
                effect.gameObject.SetActive(true);

                effect.Stop();
                effect.Play();
            });

            _levelupEffectObjectList_2.ForEach(effect =>
            {
                effect.gameObject.SetActive(true);

                effect.Stop();
                effect.Play();
            });

            _isPlayingLevelupEffect = false;
        }

        private void SetGuideAlert()
        {
            if (_levelupButtonGuideAlert == null) return;
            
            _levelupButtonGuideAlert.InitAlertWithSubKey(_specCharacterData.character_id);
        }

        private void OnClickDetailStatButton()
        {
            if (_userStatData == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<InfoDetailTooltipPopup>(_userStatData).Forget();
        }

        private void OnClickLevelupButton()
        {
            if (_userCharacterData == null) return;
            if (_specCharacterLevelExpData == null) return;

            // 캐릭터 보유 상태 검사
            if (_isHaveCharacter == false)
            {
                return;
            }

            // 최대 레벨 검사
            int maxLevel = UserDataManager.Instance.GetCharacterMaxLevel(_userCharacterData.CharacterId);
            if (_userCharacterData.Level >= maxLevel)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_MAX_LV_NEED_TRANSCENDENCE");
                return;
            }

            // 재료 검사
            if (!UserDataManager.Instance.CheckEnoughItem(_specCharacterLevelExpData.base_levelup_item_type, 0, _specCharacterLevelExpData.base_levelup_item_count, true)
                || !UserDataManager.Instance.CheckEnoughItem(ItemType.GOLD, 0, _specCharacterLevelExpData.need_gold, true)
                || !UserDataManager.Instance.CheckEnoughItem(_specCharacterLevelExpData.sec_levelup_item_type, _specCharacterData.character_id, _specCharacterLevelExpData.sec_levelup_item_count, true))
            {
                return;
            }

            // 재료 아이템 소진
            List<RewardItem> recipeItemList = new List<RewardItem>();
            recipeItemList.Add(new RewardItem(_specCharacterLevelExpData.base_levelup_item_type, 0, _specCharacterLevelExpData.base_levelup_item_count));
            recipeItemList.Add(new RewardItem(ItemType.GOLD, 0, _specCharacterLevelExpData.need_gold));
            if (_specCharacterLevelExpData.sec_levelup_item_count > 0)
            {
                recipeItemList.Add(new RewardItem(_specCharacterLevelExpData.sec_levelup_item_type, _specCharacterData.character_id, _specCharacterLevelExpData.sec_levelup_item_count));
            }

            UserDataManager.Instance.DecreaseRewardItemList(recipeItemList, true);

            // 레벨업 진행
            UserDataManager.Instance.IncreaseCharacterLevel(_specCharacterData.character_id, 1);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.LEVELUP_CHARACTER, 0, 1);
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.LEVELUP_CHARACTER_TARGET, _specCharacterData.character_id, 1);
            GuideMissionManager.Instance.RefreshGuideMissionUI();
                
            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.LEVELUP_CHARACTER, 1, true, true);

            // 이펙트 실행
            PlayLevelUpEffect();

            // 메인 레이어 갱신
            _parentCollectionPopup?.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN_DETAIL);

            // 사운드 플레이
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_char_level_up);

            RefreshLayer();
        }

        private void OnClickCharacterResetButton()
        {
            // 리셋 가능 레벨 체크
            if (_userCharacterData.Level <= 1)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_RESET_IMPOSSIBLE_ALERT");
                return;
            }

            // 리셋 가능 횟수 체크
            int maxResetCount = SpecDataManager.Instance.GetGameConfig<int>("character_level_reset_count_daily");
            if (UserDataManager.Instance.UserBasicData.ResetCharacterCount >= maxResetCount)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_RESET_END_GUIDE");
                return;
            }

            // 레벨업 리셋에 소모된 아이템 반환 아이템 체크
            var resetRewardItemList = SpecDataManager.Instance.GetCharacterLevelupTotalNeedItemList(_userCharacterData.Level, _userCharacterData.CharacterId);
            if (resetRewardItemList == null || resetRewardItemList.Count <= 0)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_ITEM_ISSUE");
                return;
            }

            string descText = LanguageManager.Instance.GetLanguageText("MSG_CHARACTER_LV_RESET_ALERT");
            SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
            newPopupData.SetPopupData("시스템 알림", descText, "확인", "취소", () =>
            {
                SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();

                // 캐릭터 레벨 리셋
                UserDataManager.Instance.SetCharacterLevel(_userCharacterData.CharacterId, 1);

                // 리셋 횟수 증가
                UserDataManager.Instance.SetResetCharacterCount(1, true, true);

                // 레벨업에 소모된 아이템 반환 적용 처리
                UserDataManager.Instance.IncreaseRewardItemList(resetRewardItemList, true);
                SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", resetRewardItemList)).Forget();

                // 팝업 레이어 갱신
                _parentCollectionPopup?.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN_DETAIL);

                RefreshLayer();
            });

            SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();
        }

        private void OnClickDimmedResetButton()
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_RESET_END_GUIDE");
        }

        private void OnClickTranscendenceButton()
        {
            if (_userCharacterData == null) return;
            if (_specCharacterTranscendenceData == null) return;

            // 캐릭터 보유 상태 검사
            if (_isHaveCharacter == false)
            {
                return;
            }

            // 최대 초월 레벨 검사
            if (_maxTranscendenceLevel <= _userCharacterData.TranscendenceLevel)
            {
                return;
            }

            // 재료 검사
            if (!UserDataManager.Instance.CheckEnoughItem(_specCharacterTranscendenceData.item_type, _specCharacterData.character_id, _specCharacterTranscendenceData.char_transcendence_count, true))
            {
                return;
            }

            string characterName = LanguageManager.Instance.GetLanguageText(_specCharacterData.name_token);
            string contentText = string.Format(LanguageManager.Instance.GetLanguageText("MSG_TRANSCENDENCE_ASK"), characterName);

            SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
            newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", () =>
            {
                // 재료 아이템 소진
                List<RewardItem> recipeItemList = new List<RewardItem>();
                recipeItemList.Add(new RewardItem(_specCharacterTranscendenceData.item_type, _specCharacterData.character_id, _specCharacterTranscendenceData.char_transcendence_count));

                UserDataManager.Instance.DecreaseRewardItemList(recipeItemList, true);

                // 초월 진행
                UserDataManager.Instance.IncreaseTranscendenceLevel(_specCharacterData.character_id, 1);

                // 메인 레이어 갱신
                _parentCollectionPopup?.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN_DETAIL);

                // 사운드 플레이
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_char_level_up);

                RefreshLayer();
                // 이펙트 실행
                PlayLevelUpEffect();
                
                var afterTranscenenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                    _specCharacterData.character_element_type, _specCharacterData.grade_type,
                    _userCharacterData.TranscendenceLevel + 1);
                if (afterTranscenenceData != null)
                {
                    string msg =
                        $"{LanguageManager.Instance.GetLanguageText("MSG_MAX_LV_UP")}\n{_specCharacterTranscendenceData.max_lv} -> {afterTranscenenceData.max_lv}";
                    ToastManager.Instance.ShowToast(msg);
                }
                else
                {
                    ToastManager.Instance.ShowToastByTokenKey("MSG_MAX_LV_UP");
                }
            });
            
            SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();
        }

        private void ClearLayer()
        {
            _levelupEffectObjectList_1.ForEach(effect => effect.gameObject.SetActive(false));
            _levelupEffectObjectList_2.ForEach(effect => effect.gameObject.SetActive(false));
        }
        
        private async UniTaskVoid StartCountdown()
        {
            DateTime currentTime = TimeManager.Instance.UtcNow();
            DateTime nextDayTime = TimeManager.Instance.TommorrowToUtc();
            
            string msg = LanguageManager.Instance.GetLanguageText("LV_RESET_REMAIN_TIME");
            while (true)
            {
                TimeSpan timeRemaining = nextDayTime - currentTime;

                int days = timeRemaining.Days;
                int hours = timeRemaining.Hours;
                int minutes = timeRemaining.Minutes;

                string timeString = "";

                if (days > 0)
                {
                    timeString += $"{days}일 ";
                }
                if (hours > 0)
                {
                    timeString += $"{hours}시간 ";
                }
                if (minutes > 0 || timeString == "")
                {
                    timeString += $"{minutes}분";
                }
                
                _inactiveResetCountText.text = string.Format(msg, timeString);

                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
