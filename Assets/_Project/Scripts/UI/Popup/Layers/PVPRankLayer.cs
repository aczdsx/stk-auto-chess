using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPRankLayer : CachedMonoBehaviour
    {
        [Header("Common")] [SerializeField] private GameObject _emptyLayerObject; // 랭킹리스트에 아무도 없을 경우 노출

        [Header("My Ranking Info")] [SerializeField]
        private Image _myTierImage;

        [SerializeField] private TextMeshProUGUI _myRankingText;
        [SerializeField] private TextMeshProUGUI _myLevelText;
        [SerializeField] private TextMeshProUGUI _myNicknameText;
        [SerializeField] private TextMeshProUGUI _myRankPointText;

        [Space(10)] [SerializeField] private GameObject _bronzeRankerObject; // 3위 랭커 아이콘
        [SerializeField] private GameObject _silverRankerObject; // 2위 랭커 아이콘
        [SerializeField] private GameObject _goldRankerObject; // 1위 랭커 아이콘

        [Header("Ranking List")] [SerializeField]
        private ScrollRect _rankScrollRect;

        [SerializeField] private GameObject _rankSlotObject;

        private ArenaMainPopup _parentPopup;

        private UserPVP _currentUserPVPData;
        private List<PvpRankingData> _currentServerRankingDataList;

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

            // 랭커 아이콘 세팅
            _bronzeRankerObject.SetActive(_currentUserPVPData.Ranking == 3);
            _silverRankerObject.SetActive(_currentUserPVPData.Ranking == 2);
            _goldRankerObject.SetActive(_currentUserPVPData.Ranking == 1);

            CreateRankScrollList();

            _emptyLayerObject?.SetActive(_currentServerRankingDataList == null || _currentServerRankingDataList.Count == 0);

            _rankScrollRect.verticalNormalizedPosition = 1;
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
                    if (string.IsNullOrEmpty(rankData.PlayerId) || rankData.Rank <= 0) continue;
                    
                    var newSlotObject = Instantiate(_rankSlotObject, _rankScrollRect.content);
                    var rankSlot = newSlotObject.GetComponent<ArenaRankSlot>();

                    rankSlot?.SetSlot(rankData);
                }
            }
        }

        public void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_rankScrollRect.content);
        }
    }
}