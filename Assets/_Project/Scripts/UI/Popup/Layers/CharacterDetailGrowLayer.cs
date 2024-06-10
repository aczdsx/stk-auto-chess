using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
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

        private SpecCharacter _specCharacterData;
        private UserCharacter _userCharacterData;

        private void Awake()
        {
            _detailStatButton.onClick.AddListener(OnClickDetailStatButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _detailStatButton.onClick.RemoveListener(OnClickDetailStatButton);
        }

        public void InitLayer(int prefabID)
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(prefabID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(prefabID);

            // test 임시 처리
            SetDefaultStatInfo();

            // // 캐릭터 보유 상태에 따른 분기처리
            // if (UserDataManager.Instance.IsHaveCharacter(prefabID))
            // {
            //     SetUserStatInfo();
            // }
            // else
            // {
            //     SetDefaultStatInfo();
            // }
        }

        private void SetUserStatInfo()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            // todo.. 추후 스탯 관련 계산식 및 데이터 구조 필요

        }

        private void SetDefaultStatInfo()
        {
            if (_specCharacterData == null || _userCharacterData == null) return;

            _levelText.text = "Lv.1";
            _battlePointText.text = "-";
            _attackValueText.text = _specCharacterData.stat_atk.ToString("N0");
            _hpValueText.text = _specCharacterData.stat_hp.ToString("N0");
            _apDefText.text = _specCharacterData.stat_res.ToString("N0");
            _adDefText.text = _specCharacterData.stat_def.ToString("N0");
        }

        private void OnClickDetailStatButton()
        {

        }
    }
}
