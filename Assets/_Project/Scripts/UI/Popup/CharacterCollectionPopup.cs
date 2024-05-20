using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.Obfuscator;
using CookApps.SpecData;
using UnityEngine;
using UnityEngine.UI;
using CookApps.TeamBattle.UIManagements;
using Google.Protobuf.Collections;
using JetBrains.Annotations;

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
        [SerializeField] private GameObject _characterCardSlotObject;

        private CharacterCollectionTabType _currentTabType = CharacterCollectionTabType.All;

        private ISpecData<ObfuscatorInt, Character> _totalCharacterList;      // 전체 캐릭터 리스트
        private List<CharacterCardSlot> _characterCardSlotList = new List<CharacterCardSlot>();


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
            ClearList();

            _totalCharacterList = SpecDataManager.Instance.Character;

            foreach (var characterData in _totalCharacterList.All)
            {
                GameObject newCardObject = Instantiate(_characterCardSlotObject, _characterScrollRect.content);
                CharacterCardSlot slot = newCardObject.GetComponent<CharacterCardSlot>();
                slot.SetCharcacterSlot(characterData);

                _characterCardSlotList.Add(slot);
            }
        }

        private void RefreshUI()
        {

        }

        private void ClearList()
        {
            _characterCardSlotList.Clear();

            BMUtil.RemoveChildObjects(_characterScrollRect.content);
        }
    }
}
