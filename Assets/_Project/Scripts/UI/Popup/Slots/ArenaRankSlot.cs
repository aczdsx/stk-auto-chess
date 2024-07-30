using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ArenaRankSlot : CachedMonoBehaviour
    {
        [SerializeField] private Image _rankTierImage;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _rankPointText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _nicknameText;

        private PvpRankingData _currentRankingData;
        private UserPVPBattleSimpleData _userPVPBattleSimpleData;
        
        private SpecPVPTier _specPVPTierData;
        
        public void SetSlot(PvpRankingData data)
        {
            if (data == null) return;

            _currentRankingData = data;
            _userPVPBattleSimpleData = BMUtil.DecompressGzipToDataClass<UserPVPBattleSimpleData>(_currentRankingData.SimpleInfo);

            _specPVPTierData = SpecDataManager.Instance.GetPVPTierData(_userPVPBattleSimpleData.RankId);
            
            _rankTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specPVPTierData.pvp_tier_type);
            _rankText.text = _currentRankingData.Rank.ToString();
            _rankPointText.text = _currentRankingData.Score.ToString();
            _levelText.text = $"Lv. {_userPVPBattleSimpleData.PlayerLv}";
            _nicknameText.text = _userPVPBattleSimpleData.Nickname;
        }
    }
}