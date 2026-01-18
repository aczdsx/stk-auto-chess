using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterItemSlot : CachedMonoBehaviour
    {
        [SerializeField] private Image _characterImage;
        [SerializeField] private SpriteLoader _characterSpriteLoader;
        [SerializeField] private Image _SynergyImage;
        [SerializeField] private SpriteLoader _SynergySpriteLoader;
        [SerializeField] private Image _SynergyClassImage;
        [SerializeField] private SpriteLoader _SynergyClassSpriteLoader;
        [SerializeField] private TextMeshProUGUI _lvText;

        private CharacterData _userCharacterData;
        private CharacterInfo _specCharacterData;

        // 자기 자신의 덱 기반 정보로 세팅
        public void SetSlot(int characterID)
        {
            if (characterID <= 0) return;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(characterID);

            _characterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(_specCharacterData.prefab_id)).Forget();
            _SynergySpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specCharacterData.character_element_type)).Forget();
            _SynergyClassSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specCharacterData.character_stella_type)).Forget();
            _lvText.text = $"{_userCharacterData.Level}";
        }

        // 타겟 데이터 기반 정보로 세팅
        public void SetSlot(int characterID, int targetLevel)
        {
            if (characterID <= 0) return;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(characterID);

            _characterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(_specCharacterData.prefab_id)).Forget();
            _SynergySpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specCharacterData.character_element_type)).Forget();
            _SynergyClassSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specCharacterData.character_stella_type)).Forget();
            _lvText.text = $"{targetLevel}";
        }
    }
}