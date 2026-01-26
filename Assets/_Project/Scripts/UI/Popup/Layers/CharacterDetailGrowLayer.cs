using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using NUnit.Framework.Constraints;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

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
        [SerializeField] private LocalizeStringEvent[] levelUpButtonText;
        [SerializeField] private CAButton activeLevelUpButton;
        [SerializeField] private CAButton inactiveLevelUpButton;
        [SerializeField] private CurrencyUIItem[] levelUpItems;
        [SerializeField] private List<UIParticleSystem> levelUpEffects;

        [Header("Transcendence Layer")]
        [SerializeField] private GameObject _transcendenceLayerObject;
        [SerializeField] private CAButton _activeTranscendenceButton;
        [SerializeField] private CAButton _inactiveTranscendenceButton;

        [Space(10)]
        [SerializeField] private List<TranscendStar> _starList;

        [Space(10)]
        [SerializeField] private CurrencyUIItem _transcendenceItemCurrencyUIItem;

        private int characterId;
        private CharacterData _userCharacterData;
        private CharacterInfo _specCharacterData;
        private CharacterLevelExp _specCharacterLevelExpData;
        private CharacterTranscendence _specCharacterTranscendenceData;

        private CharacterStatData _userStatData;

        private CharacterCollectionPopup _parentCollectionPopup;

        private bool _isHaveCharacter = false;

        private InventoryDataBridge _inventoryBridge;

        private void Awake()
        {
            _inventoryBridge = new InventoryDataBridge();
            _detailStatButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickDetailStatButton()).AddTo(this);

            // 레벨업
            activeLevelUpButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickLevelupButtonAsync(), AwaitOperation.Drop).AddTo(this);
            inactiveLevelUpButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickLevelupButtonAsync(), AwaitOperation.Drop).AddTo(this);
            // 초월
            _activeTranscendenceButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickTranscendenceButtonAsync(), AwaitOperation.Drop).AddTo(this);
        }

        public void InitLayer(CharacterCollectionPopup _parentPopup, int characterId)
        {
            this.characterId = characterId;

            _parentCollectionPopup = _parentPopup;

            RefreshLayer();

            // 가이드 알림 처리
            SetGuideAlert();

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        public void RefreshLayer()
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterId);
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(characterId);
            _isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(characterId);
            SetUserStatLayer();
            SetLevelUpLayer();
            SetTranscendenceLayer();
            SetTranscendencePieceLayer();   // SetTranscendenceLayer 이후 호출되어야 함
        }

        private void SetUserStatLayer()
        {
            if (_specCharacterData == null)
                return;

            var exceedLevel = _userCharacterData?.ExceedLevel ?? 0;
            var level = _userCharacterData?.Level ?? 1;

            var nextExceedLevel = SpecDataManager.Instance.GetCharacterNextExceedLevelExpData(exceedLevel);

            int userLevel = Mathf.Max(1, (int)level);

            _userStatData = new CharacterStatData(_specCharacterData.id, userLevel, GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            _levelText.text = $"Lv.{userLevel}/{nextExceedLevel?.level ?? userLevel}";
            _battlePointText.text = _userStatData.GetAttrValueCP().ToString("N0");
            _attackValueText.text = _userStatData.AD.ToString("N0");
            _hpValueText.text = _userStatData.HP.ToString("N0");
            _apDefText.text = _userStatData.APReduce.ToString("N0");
            _adDefText.text = _userStatData.ADReduce.ToString("N0");
        }

        private void SetTranscendencePieceLayer()
        {
            if (_specCharacterData == null)
                return;

            if (_specCharacterTranscendenceData == null)
                return;

            _pieceIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(_specCharacterData.id)).Forget();
            ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);
            int characterPiece = (int)_inventoryBridge.GetCurrency(pieceItemId);
            _pieceAmountText.text = $"{characterPiece}<color=#C4CDE2>/{_specCharacterTranscendenceData.piece}</color>";

            _pieceSlider.maxValue = _specCharacterTranscendenceData.piece;
            _pieceSlider.value = characterPiece;
        }

        private void SetLevelUpLayer()
        {
            if (_specCharacterData == null)
                return;
            
            var exceedLevel = _userCharacterData?.ExceedLevel ?? 0;

            // 레벨업 가능 여부 체크
            int userLevel = Mathf.Max(1, (int)(_userCharacterData?.Level ?? 1));
            var nextExceedLevelExpData = SpecDataManager.Instance.GetCharacterNextExceedLevelExpData(exceedLevel);
            _specCharacterLevelExpData = SpecDataManager.Instance.GetCharacterLevelExpData(userLevel);

            if (_isHaveCharacter && userLevel >= (nextExceedLevelExpData?.level ?? int.MaxValue))
            {
                _specCharacterLevelExpData = nextExceedLevelExpData;
                foreach (var localizeStringEvent in levelUpButtonText)
                {
                    localizeStringEvent.StringReference.SetReference(LanguageManager.DefaultTableName, "UI_EXCEED");
                }
            }
            else
            {
                foreach (var localizeStringEvent in levelUpButtonText)
                {
                    localizeStringEvent.StringReference.SetReference(LanguageManager.DefaultTableName, "UI_LEVEL_UP");
                }
            }

            if (_specCharacterLevelExpData != null)
            {
                var uiItemDatas = new List<(ItemId, int, bool)>();
                if (_specCharacterLevelExpData.need_gold > 0)
                    uiItemDatas.Add((IdMap.Item.Gold, _specCharacterLevelExpData.need_gold, _specCharacterLevelExpData.need_gold <= (int)_inventoryBridge.GetCurrency(IdMap.Item.Gold)));
                if (_specCharacterLevelExpData.base_levelup_item_id != 0)
                    uiItemDatas.Add((_specCharacterLevelExpData.base_levelup_item_id, _specCharacterLevelExpData.base_levelup_item_count, _specCharacterLevelExpData.base_levelup_item_count <= (int)_inventoryBridge.GetCurrency(_specCharacterLevelExpData.base_levelup_item_id)));
                if (_specCharacterLevelExpData.sec_levelup_item_id != 0)
                    uiItemDatas.Add((_specCharacterLevelExpData.sec_levelup_item_id, _specCharacterLevelExpData.sec_levelup_item_count, _specCharacterLevelExpData.sec_levelup_item_count <= (int)_inventoryBridge.GetCurrency(_specCharacterLevelExpData.sec_levelup_item_id)));

                var isAvailLevelup = uiItemDatas.All(x => x.Item3);

                for (var i = 0; i < levelUpItems.Length; i++)
                {
                    if (uiItemDatas.Count <= i)
                    {
                        levelUpItems[i].gameObject.SetActive(false);
                        continue;
                    }

                    levelUpItems[i].gameObject.SetActive(true);
                    levelUpItems[i].SetUIItem(uiItemDatas[i]);
                }
                activeLevelUpButton.gameObject.SetActive(isAvailLevelup);
                inactiveLevelUpButton.gameObject.SetActive(!isAvailLevelup);
            }
            else
            {
                for (var i = 0; i < levelUpItems.Length; i++)
                {
                    levelUpItems[i].gameObject.SetActive(false);
                }
                activeLevelUpButton.gameObject.SetActive(false);
                inactiveLevelUpButton.gameObject.SetActive(true);

                foreach (var localizeStringEvent in levelUpButtonText)
                {
                    localizeStringEvent.StringReference.SetReference(LanguageManager.DefaultTableName, "MSG_ALERT_MAX_LEVELUP");
                }
            }

        }

        private void SetTranscendenceLayer()
        {
            if (_specCharacterData == null) 
                return;

            int transcendLevel = (int)(_userCharacterData?.TranscendLevel ?? 1);

            // 초월 가능 여부 체크
            // 초월에 필요한 자원 정보 세팅
            _specCharacterTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(_specCharacterData.grade_type, transcendLevel);

            bool isHasPiece = false;
            if (_specCharacterTranscendenceData != null)
            {
                ItemId pieceItemId = ItemIdExtensions.GetCharacterPieceId(_specCharacterData.id);
                int characterPiece = (int)_inventoryBridge.GetCurrency(pieceItemId);
                isHasPiece = characterPiece >= _specCharacterTranscendenceData.piece;
                _transcendenceItemCurrencyUIItem.SetUIItem(_specCharacterData.id, _specCharacterTranscendenceData.piece);
            }
            bool isAvailTranscendence = _isHaveCharacter && _specCharacterTranscendenceData?.piece != 0 && isHasPiece;

            _activeTranscendenceButton.gameObject.SetActive(isAvailTranscendence);
            _inactiveTranscendenceButton.gameObject.SetActive(!isAvailTranscendence);

            UpdateStarDisplay(transcendLevel);
        }

        private const int MaxVisibleStars = 5;

        private void UpdateStarDisplay(int transcendLevel)
        {
            int startIndex = Mathf.Max(0, transcendLevel - MaxVisibleStars);
            for (int i = 0; i < _starList.Count; i++)
            {
                bool isVisible = i >= startIndex && i < transcendLevel;
                _starList[i].SetActive(isVisible, false);
            }
        }

        private void PlayTranscendStarAnimation(int previousLevel, int newLevel)
        {
            for (int i = previousLevel; i < newLevel && i < _starList.Count; i++)
            {
                _starList[i].SetActive(true, true);
            }
        }

        private void PlayLevelUpEffect()
        {
            for (var i = 0; i < levelUpEffects.Count; i++)
            {
                levelUpEffects[i].StartParticleEmission();
            }
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
            if (_isHaveCharacter == false) return;

            await LevelUpCharacterAsync();
        }

        private async UniTask LevelUpCharacterAsync()
        {
            try
            {
                if (_specCharacterLevelExpData.IsExceed)
                {
                    var resp = await NetManager.Instance.Character.ExceedAsync(_userCharacterData.CharacterId);
                    if (resp?.IsSuccess == false)
                        return;

                    var gdb = new GuideMissionDataBridge();
                    // ! GUIDE_TODO
                    // ! 305	8	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_305	아트레시아 돌파 진행	20006	GUIDE_MISSION_DESC_305	0	1	GOLD	210001	200											
                    // ! CHARACTER_EXCEED
                    if (gdb.GuideMissionId == 305 && ServerDataManager.Instance.Character.GetCharacter(GuideMissionConstants.아트레시아ID).ExceedLevel > 0)
                    {
                        await gdb.AddActionAsync(GuideMissionType.CHARACTER_EXCEED, 1);
                    }
                }
                else
                {
                    var resp = await NetManager.Instance.Character.LevelUpAsync(_userCharacterData.CharacterId);
                    if (resp?.IsSuccess == false)
                        return;

                    var gdb = new GuideMissionDataBridge();
                    // ! GUIDE_TODO
                    // ! 202	3	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_202	아트레시아 레벨 2 만들기	20003	GUIDE_MISSION_DESC_202	0	1	GOLD	210001	200											
                    // ! CHARACTER_LEVELUP
                    if (gdb.GuideMissionId == 202 && ServerDataManager.Instance.Character.GetCharacter(GuideMissionConstants.아트레시아ID).Level > 1)
                    {
                        await gdb.AddActionAsync(GuideMissionType.LEVELUP_CHARACTER_TARGET, 1);
                    }
                }

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
            if (_specCharacterTranscendenceData.piece == 0)
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
                int previousLevel = (int)_userCharacterData.TranscendLevel;

                var response = await NetManager.Instance.Character.TranscendAsync(_userCharacterData.CharacterId);

                if (response?.IsSuccess == false)
                    return;

                var gdb = new GuideMissionDataBridge();
                // ! GUIDE_TODO
                // ! 402	15	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_402	기사 초월 가이드 미션	30001	GUIDE_MISSION_DESC_402	0	1	GOLD	210001	200											
                // ! CHARACTER_TRANSCENDENCE
                if (gdb.GuideMissionId == 402 && ServerDataManager.Instance.Character.GetCharacter(GuideMissionConstants.아트레시아ID).TranscendLevel > 3)
                {
                    await gdb.AddActionAsync(GuideMissionType.CLEAR_TUTORIAL, 1);
                }

                // 메인 레이어 갱신
                _parentCollectionPopup?.RefreshTabLayer(CharacterCollectionPopupTabType.MAIN_DETAIL);

                // 사운드 플레이
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_char_level_up);

                RefreshLayer();
                // 이펙트 실행
                // PlayLevelUpEffect();
                // 별 연출 재생
                PlayTranscendStarAnimation(previousLevel, (int)_userCharacterData.TranscendLevel);

                // var afterTranscenenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(_specCharacterData.grade_type,
                //     (int)(_userCharacterData.TranscendLevel + 1));
                // if (afterTranscenenceData != null)
                // {
                //     string msg =
                //         $"{LanguageManager.Instance.GetDefaultText("MSG_MAX_LV_UP")}\n{_specCharacterTranscendenceData.max_level} -> {afterTranscenenceData.max_level}";
                //     ToastManager.Instance.ShowToast(msg);
                // }
                // else
                {
                    ToastManager.Instance.ShowToastByTokenKey("MSG_MAX_LV_UP");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to transcend character: {e.Message}");
            }
        }
    }
}
