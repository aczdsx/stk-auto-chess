using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private SpriteLoader _pieceIconSpriteLoader;
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

        private CharacterData _userCharacterData;
        private CharacterInfo _specCharacterData;

        private CharacterLevelExp _specCharacterLevelExpData;
        private CharacterTranscendence _specCharacterTranscendenceData;

        private CharacterStatData _userStatData;

        private CharacterCollectionPopup _parentCollectionPopup;

        private bool _isHaveCharacter = false;
        private bool _isPlayingLevelupEffect = false;

        private int _maxTranscendenceLevel;

        private InventoryDataBridge _inventoryBridge;

        private void Awake()
        {
            _inventoryBridge = new InventoryDataBridge();
            _detailStatButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickDetailStatButton()).AddTo(this);

            // 레벨업
            _activeLevelUpButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickLevelupButtonAsync(), AwaitOperation.Drop).AddTo(this);
            _inactiveLevelUpButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickLevelupButtonAsync(), AwaitOperation.Drop).AddTo(this);
            _activeResetLevelUpButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickCharacterResetButtonAsync(), AwaitOperation.Drop).AddTo(this);
            _inactiveResetLevelUpButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickDimmedResetButton()).AddTo(this);

            // 초월
            _activeTranscendenceButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickTranscendenceButtonAsync(), AwaitOperation.Drop).AddTo(this);
        }

        public void InitLayer(CharacterCollectionPopup _parentPopup, int characterID)
        {
            ClearLayer();

            _parentCollectionPopup = _parentPopup;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(characterID);

            _isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(characterID);

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

            int maxLevel = SpecDataManager.Instance.GetCharacterMaxLevel((int)_userCharacterData.Level);

            int userLevel = Mathf.Max(1, (int)_userCharacterData.Level);

            _userStatData = new CharacterStatData((int)_userCharacterData.CharacterId, userLevel, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            _levelText.text = $"Lv.{userLevel}/{maxLevel}";
            _battlePointText.text = _userStatData.GetAttrValueCP().ToString("N0");
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

            _pieceIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(_specCharacterData.prefab_id)).Forget();
            ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);
            int characterPiece = (int)_inventoryBridge.GetCurrency(pieceItemId);
            _pieceAmountText.text = $"{characterPiece}<color=#C4CDE2>/{_specCharacterTranscendenceData.piece}</color>";

            _pieceSlider.maxValue = _specCharacterTranscendenceData.piece;
            _pieceSlider.value = characterPiece;
        }

        private void SetLevelupLayer()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            // 레벨업 가능 여부 체크
            int maxLevel = SpecDataManager.Instance.GetCharacterMaxLevel((int)_userCharacterData.Level);

            int userLevel = Mathf.Max(1, (int)_userCharacterData.Level);

            // 레벨업에 필요한 자원 정보 세팅
            _specCharacterLevelExpData = SpecDataManager.Instance.GetCharacterLevelExpData(userLevel);

            bool isAvailLevelup = _isHaveCharacter && _userCharacterData.Level < maxLevel;
            if (_specCharacterLevelExpData != null)
            {
                bool isEnoughGold = _specCharacterLevelExpData.need_gold <= (int)_inventoryBridge.GetCurrency(IdMap.Item.Gold);
                bool isEnoughExpItem = _specCharacterLevelExpData.base_levelup_item_count <= (int)_inventoryBridge.GetCurrency(_specCharacterLevelExpData.base_levelup_item_id);
                bool isEnoughExpItem2 = _specCharacterLevelExpData.sec_levelup_item_count <= (int)_inventoryBridge.GetCurrency(_specCharacterLevelExpData.sec_levelup_item_id);

                isAvailLevelup = isEnoughGold && isEnoughExpItem && isEnoughExpItem2 && isAvailLevelup;

                _goldCurrencyUIItem.SetUIItem(IdMap.Item.Gold, _specCharacterLevelExpData.need_gold, isEnoughGold);
                _baseExpItemCurrencyUIItem.SetUIItem(_specCharacterLevelExpData.base_levelup_item_id, _specCharacterLevelExpData.base_levelup_item_count, isEnoughExpItem);

                // TODO: 체크 필요
                bool isNeedSecondExpItem = _specCharacterLevelExpData.sec_levelup_item_count > 0;
                if (isNeedSecondExpItem)
                {
                    _secondExpItemCurrencyUIItem.SetUIItem(_specCharacterLevelExpData.sec_levelup_item_id, _specCharacterLevelExpData.sec_levelup_item_count, isEnoughExpItem2);
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
            _maxTranscendenceLevel = transcendenceDataList.Max(data => data.star);
            // 초월에 필요한 자원 정보 세팅
            _specCharacterTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(_specCharacterData.grade_type, (int)_userCharacterData.TranscendLevel);

            bool isHasPiece = false;
            if (_specCharacterTranscendenceData != null)
            {
                ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);
                int characterPiece = (int)_inventoryBridge.GetCurrency(pieceItemId);
                isHasPiece = characterPiece >= _specCharacterTranscendenceData.piece;
                _transcendenceItemCurrencyUIItem.SetUIItem(_specCharacterData.id, _specCharacterTranscendenceData.piece);
            }
            bool isAvailTranscendence = _isHaveCharacter && _userCharacterData.TranscendLevel < _maxTranscendenceLevel && isHasPiece;

            _activeTranscendenceButton.gameObject.SetActive(isAvailTranscendence);
            _inactiveTranscendenceButton.gameObject.SetActive(!isAvailTranscendence);
        }

        private void SetLevelResetLayer()
        {
            int maxResetCount = SpecDataManager.Instance.GetGameConfig<int>("character_level_reset_count_daily");
            int resetCount = ClientDataManager.Instance.GetData<ClientBasicData>(ClientBasicData.CategoryName).GetTodayResetCharacterCount();

            int resultCount = maxResetCount - resetCount;

            string levelResetString = LanguageManager.Instance.GetDefaultText("UI_LEVEL_RESET");
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

            _levelupButtonGuideAlert.InitAlertWithSubKey(_specCharacterData.id);
        }

        private void OnClickDetailStatButton()
        {
            if (_userStatData == null) return;

            SceneUILayerManager.Instance.PushUILayerAsync<InfoDetailTooltipPopup>(_userStatData).Forget();
        }

        private async UniTask OnClickLevelupButtonAsync()
        {
            if (_userCharacterData == null) return;
            if (_specCharacterLevelExpData == null) return;

            // 캐릭터 보유 상태 검사
            if (_isHaveCharacter == false)
            {
                return;
            }

            // 최대 레벨 검사
            int maxLevel = SpecDataManager.Instance.GetCharacterMaxLevel((int)_userCharacterData.Level);
            if (_userCharacterData.Level >= maxLevel)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_MAX_LV_NEED_TRANSCENDENCE");
                return;
            }

            await LevelUpCharacterAsync();
        }

        private async UniTask LevelUpCharacterAsync()
        {
            try
            {
                await NetManager.Instance.Character.LevelUpAsync(_userCharacterData.CharacterId);

                // 이펙트 실행
                PlayLevelUpEffect();

                // 메인 레이어 갱신
                _parentCollectionPopup?.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN_DETAIL);

                // 사운드 플레이
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_char_level_up);

                RefreshLayer();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to level up character: {e.Message}");
            }
        }

        private async UniTask OnClickCharacterResetButtonAsync()
        {
            // 리셋 가능 레벨 체크
            if (_userCharacterData.Level <= 1)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_RESET_IMPOSSIBLE_ALERT");
                return;
            }

            // 리셋 가능 횟수 체크
            int maxResetCount = SpecDataManager.Instance.GetGameConfig<int>("character_level_reset_count_daily");
            if (ClientDataManager.Instance.GetData<ClientBasicData>(ClientBasicData.CategoryName).GetTodayResetCharacterCount() >= maxResetCount)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_RESET_END_GUIDE");
                return;
            }

            // 레벨업 리셋에 소모된 아이템 반환 아이템 체크
            var resetRewardItemList = SpecDataManager.Instance.GetCharacterLevelupTotalNeedItemList((int)_userCharacterData.Level, (int)_userCharacterData.CharacterId);
            if (resetRewardItemList == null || resetRewardItemList.Count <= 0)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_ITEM_ISSUE");
                return;
            }

            string descText = LanguageManager.Instance.GetDefaultText("MSG_CHARACTER_LV_RESET_ALERT");
            SystemConfirmPopupData newPopupData = new SystemConfirmPopupData("시스템 알림", descText, "확인", "취소");
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData);
            var isConfirmed = await popup.WaitForExit();
            
            if (isConfirmed is true)
            {
                // TODO: 서버 API로 캐릭터 레벨 리셋 구현 필요
                // 현재 서버 API가 없음
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_IMPLEMENTED");
            }
        }

        private void OnClickDimmedResetButton()
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_CHARACTER_LV_RESET_END_GUIDE");
        }

        private async UniTask OnClickTranscendenceButtonAsync()
        {
            if (_userCharacterData == null) return;
            if (_specCharacterTranscendenceData == null) return;

            // 캐릭터 보유 상태 검사
            if (_isHaveCharacter == false)
            {
                return;
            }

            // 최대 초월 레벨 검사
            if (_maxTranscendenceLevel <= _userCharacterData.TranscendLevel)
            {
                return;
            }

            // 재료 검사
            ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);
            if (!_inventoryBridge.HasEnoughCurrency(pieceItemId, (ulong)_specCharacterTranscendenceData.piece))
            {
                return;
            }

            string characterName = LanguageManager.Instance.GetDefaultText(_specCharacterData.name_token);
            string contentText = string.Format(LanguageManager.Instance.GetDefaultText("MSG_TRANSCENDENCE_ASK"), characterName);

            SystemConfirmPopupData newPopupData = new SystemConfirmPopupData("시스템 알림", contentText, "확인", "취소");
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData);
            var isConfirmed = await popup.WaitForExit();
            if (isConfirmed is true)
            {
                // 초월 진행 (서버 API 호출)
                await TranscendCharacterAsync();
            }
        }

        private async UniTask TranscendCharacterAsync()
        {
            try
            {
                var response = await NetManager.Instance.Character.TranscendAsync(_userCharacterData.CharacterId);

                // 메인 레이어 갱신
                _parentCollectionPopup?.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN_DETAIL);

                // 사운드 플레이
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_char_level_up);

                RefreshLayer();
                // 이펙트 실행
                PlayLevelUpEffect();

                var afterTranscenenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(_specCharacterData.grade_type,
                    (int)(_userCharacterData.TranscendLevel + 1));
                if (afterTranscenenceData != null)
                {
                    // ! max_level은 사용 안한다!
                    // string msg =
                    //     $"{LanguageManager.Instance.GetDefaultText("MSG_MAX_LV_UP")}\n{_specCharacterTranscendenceData.max_level} -> {afterTranscenenceData.max_level}";
                    // ToastManager.Instance.ShowToast(msg);
                }
                else
                {
                    ToastManager.Instance.ShowToastByTokenKey("MSG_MAX_LV_UP");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to transcend character: {e.Message}");
            }
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

            string msg = LanguageManager.Instance.GetDefaultText("LV_RESET_REMAIN_TIME");
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
