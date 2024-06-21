using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
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
        [Header("Stat Info")]
        [SerializeField] private CAButton _detailStatButton;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _battlePointText;
        [SerializeField] private TextMeshProUGUI _attackValueText;
        [SerializeField] private TextMeshProUGUI _hpValueText;
        [SerializeField] private TextMeshProUGUI _apDefText;
        [SerializeField] private TextMeshProUGUI _adDefText;

        [Header("LevelUp Layer")]
        [SerializeField] private CAButton _activeLevelUpButton;
        [SerializeField] private CAButton _inactiveLevelUpButton;

        [Space(10)]
        [SerializeField] private CurrencyUIItem _baseExpItemCurrencyUIItem;
        [SerializeField] private CurrencyUIItem _secondExpItemCurrencyUIItem;
        [SerializeField] private CurrencyUIItem _goldCurrencyUIItem;

        private UserCharacter _userCharacterData;
        private SpecCharacter _specCharacterData;
        private SpecCharacterLevelExp _specCharacterLevelExpData;
        private CharacterStatData _userStatData;

        private bool _isHaveCharacter = false;

        private void Awake()
        {
            _detailStatButton.onClick.AddListener(OnClickDetailStatButton);
            _activeLevelUpButton.onClick.AddListener(OnClickLevelupButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _detailStatButton.onClick.RemoveListener(OnClickDetailStatButton);
            _activeLevelUpButton.onClick.RemoveListener(OnClickLevelupButton);
        }

        public void InitLayer(int characterID)
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);

            _isHaveCharacter = UserDataManager.Instance.IsHaveCharacter(characterID);

            // 스탯 표시
            SetUserStatInfo();

            // 레벨업 기능 관련 처리
            SetLevelupLayer();
        }

        public void RefreshLayer()
        {
            SetUserStatInfo();
        }

        private void SetUserStatInfo()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            int userLevel = Mathf.Max(1, _userCharacterData.Level);

            _userStatData = new CharacterStatData(_userCharacterData.CharacterId, userLevel);

            _levelText.text = $"Lv.{userLevel}";
            _battlePointText.text = _userStatData.GetAttrValue().ToString("N0");
            _attackValueText.text = _userStatData.AD.ToString("N0");
            _hpValueText.text = _userStatData.HP.ToString("N0");
            _apDefText.text = _userStatData.RES.ToString("N0");
            _adDefText.text = _userStatData.DEF.ToString("N0");

        }

        private void SetLevelupLayer()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            // 레벨업 가능 여부 체크
            int maxLevel = SpecDataManager.Instance.GetCharacterMaxLevel();
            bool isAvailLevelup = _isHaveCharacter && _userCharacterData.Level < maxLevel;

            int userLevel = Mathf.Max(1, _userCharacterData.Level);

            // 레벨업에 필요한 자원 정보 세팅
            _specCharacterLevelExpData = SpecDataManager.Instance.GetCharacterLevelExpData(userLevel);
            if (_specCharacterLevelExpData != null)
            {
                _baseExpItemCurrencyUIItem.SetUIItem(_specCharacterLevelExpData.base_levelup_item_type, _specCharacterLevelExpData.base_levelup_item_count);
                _goldCurrencyUIItem.SetUIItem(ItemType.GOLD, _specCharacterLevelExpData.need_gold);

                bool isNeedSecondExpItem = _specCharacterLevelExpData.sec_levelup_item_count > 0;
                if (isNeedSecondExpItem)
                {
                    _secondExpItemCurrencyUIItem.SetUIItem(_specCharacterLevelExpData.sec_levelup_item_type, _specCharacterLevelExpData.sec_levelup_item_count);
                }
                _secondExpItemCurrencyUIItem.gameObject.SetActive(isNeedSecondExpItem);
            }

            _activeLevelUpButton.gameObject.SetActive(isAvailLevelup);
            _inactiveLevelUpButton.gameObject.SetActive(!isAvailLevelup);
        }


        private void OnClickDetailStatButton()
        {
            if (_userStatData == null) return;

            SceneUILayerManager.Instance.PushUILayerAsync<InfoDetailTooltipPopup>(_userStatData).Forget();
        }

        private void OnClickLevelupButton()
        {
            if (_userCharacterData == null) return;
            if (_specCharacterLevelExpData == null) return;

            // 레벨업 가능 여부 검사
            int maxLevel = SpecDataManager.Instance.GetCharacterMaxLevel();
            bool isAvailLevelup = _isHaveCharacter && _userCharacterData.Level < maxLevel;
            if (isAvailLevelup == false)
            {
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
                recipeItemList.Add(new RewardItem(_specCharacterLevelExpData.sec_levelup_item_type, 0, _specCharacterLevelExpData.sec_levelup_item_count));
            }

            UserDataManager.Instance.DecreaseRewardItemList(recipeItemList, true);

            // 레벨업 진행
            UserDataManager.Instance.IncreaseCharacterLevel(_specCharacterData.character_id, 1);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.LEVELUP_CHARACTER, 0, 1);

            RefreshLayer();
        }
    }
}
