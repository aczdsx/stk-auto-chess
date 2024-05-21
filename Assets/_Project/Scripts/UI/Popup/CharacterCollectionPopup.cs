using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.Obfuscator;
using CookApps.SpecData;
using UnityEngine;
using UnityEngine.UI;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    public enum CharacterCollectionTabType
    {
        All,
        EARTH = 1,
        WIND = 2,
        WATER = 3,
        FIRE = 4,
        DARK = 5,
        LIGHT = 6,
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
            _currentTabType = (CharacterCollectionTabType)tabIndex;

            FilterCharacterList(_currentTabType);
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

            _characterScrollRect.verticalNormalizedPosition = 1;
        }

        private void FilterCharacterList(CharacterCollectionTabType targetType)
        {
            _characterCardSlotList.ForEach(slot =>
            {
                if (targetType == CharacterCollectionTabType.All)
                {
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive((int)slot.CharacterData.type == (int)targetType);
                }
            });

            _characterScrollRect.verticalNormalizedPosition = 1;
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
