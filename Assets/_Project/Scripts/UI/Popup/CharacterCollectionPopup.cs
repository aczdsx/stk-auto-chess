using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using UnityEngine;
using UnityEngine.UI;
using CookApps.TeamBattle.UIManagements;
using Google.Protobuf.Collections;

namespace CookApps.AutoBattler
{
    public enum CharacterCollectionTabType
    {
        All,
        FIRE,
        WATER,
        WIND,
        GROUND,
        LIGHT,
        DARK
    }

    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/Pop_CharacterCollection.prefab")]
    public class CharacterCollectionPopup : UILayer
    {
        [SerializeField] private ScrollRect _characterScrollRect;

        CharacterCollectionTabType _currentTabType = CharacterCollectionTabType.All;

        RepeatedField<UserCharacter> _totalUserCharacterList = new RepeatedField<UserCharacter>();      // 전체 캐릭터 리스트
        RepeatedField<UserCharacter> _selectedUserCharacterList = new RepeatedField<UserCharacter>();   // 선택된 캐릭터 리스트

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _currentTabType = CharacterCollectionTabType.All;

            SetCharacterCollectionUI();
        }

        public void OnClickTabToggleButton(int tabIndex)
        {
            CharacterCollectionTabType tabType = (CharacterCollectionTabType)tabIndex;
        }

        private void SetCharacterCollectionUI()
        {
            _totalUserCharacterList = UserDataManager.Instance.GetAllUserCharacters();


        }

        private void RefreshUI()
        {

        }

        private void ClearList()
        {

        }
    }
}
