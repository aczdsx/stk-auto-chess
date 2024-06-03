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

        public void InitLayer(int characterID)
        {
            _specCharacterData = SpecDataManager.Instance.SpecCharacter.Get(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);

            SetStatInfo();
        }

        private void SetStatInfo()
        {

        }

        private void OnClickDetailStatButton()
        {

        }
    }
}
