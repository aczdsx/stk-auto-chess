using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
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

        private UserPVPBattleSimpleData _userPVPBattleSimpleData;
        
        private void Awake()
        {
            _battleButton.onClick.AddListener(OnClickBattleButton);
        }

        private void OnDestroy()
        {
            _battleButton.onClick.RemoveListener(OnClickBattleButton);
        }

        public void InitSlot(UserPVPBattleSimpleData data)
        {
            if (data == null) return;
            
            _userPVPBattleSimpleData = data;       
            
            _enemyLevelText.text = $"Lv.{_userPVPBattleSimpleData.PlayerLv}";
            _enemyNicknameText.text = _userPVPBattleSimpleData.Nickname;
            _enemyBattlePowerText.text = _userPVPBattleSimpleData.BattlePoint.ToString();

            var specTierData = SpecDataManager.Instance.GetPVPTierData(_userPVPBattleSimpleData.RankId);
            _enemyRankTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _enemyRankPointText.text = _userPVPBattleSimpleData.RankPoint.ToString();
            
            CreateCharacterDeckList();
        }

        public void RefreshSlot()
        {
            
        }
        
        private void CreateCharacterDeckList()
        {
            ClearSlot();

            foreach (var simpleDeckData in _userPVPBattleSimpleData.SimpleDeckList)
            {
                GameObject newSlotObject = Instantiate(_characterDeckObject, _characterDeckScrollRect.content);
                var characterDeckSlot = newSlotObject.GetComponent<CharacterItemSlot>();
                
                characterDeckSlot.SetSlot(simpleDeckData.Id, simpleDeckData.Lv);
            }
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
            BMUtil.RemoveChildObjects(_characterDeckScrollRect.content);
        }
    }
}