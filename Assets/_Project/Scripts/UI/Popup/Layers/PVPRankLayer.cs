using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPRankLayer : CachedMonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private GameObject _emptyLayerObject;  // 랭킹리스트에 아무도 없을 경우 노출
        
        [Header("My Ranking Info")] 
        [SerializeField] private Image _myTierImage;
        [SerializeField] private TextMeshProUGUI _myRankingText;
        [SerializeField] private TextMeshProUGUI _myLevelText;
        [SerializeField] private TextMeshProUGUI _myNicknameText;
        [SerializeField] private TextMeshProUGUI _myRankPointText;
        
        [Header("Ranking List")]
        [SerializeField] private ScrollRect _rankScrollRect;
        [SerializeField] private GameObject _rankSlotObject;
        
        private ArenaMainPopup _parentPopup;
        
        private UserPVP _currentUserPVPData;
        private List<PlayerRankingData> _currentServerRankingDataList;
        
        private SpecPVPTier _specPVPTierData;
        
        public void InitLayer(ArenaMainPopup parent)
        {
            _parentPopup = parent;

            _currentUserPVPData = UserDataManager.Instance.UserPVP;

            _specPVPTierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, _currentUserPVPData.RankPoint);

            _myLevelText.text = $"Lv. {UserDataManager.Instance.UserBasicData.Level}";
            _myNicknameText.text = UserDataManager.Instance.UserBasicData.Nickname;
            _myTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specPVPTierData.pvp_tier_type);
            _myRankingText.text = _currentUserPVPData.Ranking.ToString();
            _myRankPointText.text = _currentUserPVPData.RankPoint.ToString();
            
            CreateRankScrollList();
            
            _emptyLayerObject?.SetActive(_currentServerRankingDataList == null || _currentServerRankingDataList.Count == 0);
        }
        
        public void RefreshLayer()
        {
            
        }

        private void CreateRankScrollList()
        {
            ClearLayer();

            var rankInfo = PVPManager.Instance.CurrentPVPRankListData;
            if (rankInfo != null)
            {
                _currentServerRankingDataList = rankInfo.PvpRankers.ToList();

                foreach (var rankData in _currentServerRankingDataList)
                {
                    GameObject newSlotObject = Instantiate(_rankSlotObject, _rankScrollRect.content);
                    var rankSlot = newSlotObject.GetComponent<ArenaRankSlot>();
                    
                    rankSlot?.SetSlot();
                }
            }
            
        }
        
        public void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_rankScrollRect.content);
        }
    }
}