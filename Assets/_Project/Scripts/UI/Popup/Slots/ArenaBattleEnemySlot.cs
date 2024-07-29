using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ArenaBattleEnemySlot : MonoBehaviour
    {
        [Header("Common")] 
        [SerializeField] private CAButton _battleButton;
        
        [Header("Enemy Info")]
        [SerializeField] private TextMeshProUGUI _enemyLevelText;
        [SerializeField] private TextMeshProUGUI _enemyNicknameText;
        [SerializeField] private TextMeshProUGUI _enemyBattlePowerText;
        [SerializeField] private Image _enemyRankTierImage;
        [SerializeField] private TextMeshProUGUI _enemyRankPointText;

        [Header("Character Layer")] 
        [SerializeField] private ScrollRect _characterDeckScrollRect;
        [SerializeField] private GameObject _characterDeckObject;

        private void Awake()
        {
            _battleButton.onClick.AddListener(OnClickBattleButton);
        }

        private void OnDestroy()
        {
            _battleButton.onClick.RemoveListener(OnClickBattleButton);
        }

        public void InitSlot()
        {

            CreateCharacterDeckList();
        }

        public void RefreshSlot()
        {
            
        }
        
        private void CreateCharacterDeckList()
        {
            ClearSlot();
        }
        
        private void OnClickBattleButton()
        {
            // 방어덱 설정 여부 체크
            if (UserDataManager.Instance.CheckUserCharacterBattleDeckList(InGameType.PVP_DEFENSE) == false)
            {
                ToastManager.Instance.ShowToast("TEST - 방어덱 설정이 필요합니다.");
                return;
            }
            
            // todo.. pvp 인게임 씬 진입
        }
        
        private void ClearSlot()
        {
            
        }
    }
}