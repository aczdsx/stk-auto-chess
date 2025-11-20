using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterItemSlot : CachedMonoBehaviour
    {
        [SerializeField] private Image _characterImage;
        [SerializeField] private Image _SynergyImage;
        [SerializeField] private Image _SynergyClassImage;
        [SerializeField] private TextMeshProUGUI _lvText;

        private UserCharacter _userCharacterData;
        private SpecCharacter _specCharacterData;
        
        // 자기 자신의 덱 기반 정보로 세팅
        public void SetSlot(int characterID)
        {
            if (characterID <= 0) return;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);
            
            _characterImage.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(_specCharacterData.prefab_id);
            _SynergyImage.sprite = ImageManager.Instance.GetSynergySprite(_specCharacterData.element_type);
            _SynergyClassImage.sprite = ImageManager.Instance.GetSynergySprite(_specCharacterData.character_position_type);
            _lvText.text = $"{_userCharacterData.Level}";
        }
        
        // 타겟 데이터 기반 정보로 세팅
        public void SetSlot(int characterID, int targetLevel)
        {
            if (characterID <= 0) return;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);
            
            _characterImage.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(_specCharacterData.prefab_id);
            _SynergyImage.sprite = ImageManager.Instance.GetSynergySprite(_specCharacterData.element_type);
            _SynergyClassImage.sprite = ImageManager.Instance.GetSynergySprite(_specCharacterData.character_position_type);
            _lvText.text = $"{targetLevel}";
        }
    }
}