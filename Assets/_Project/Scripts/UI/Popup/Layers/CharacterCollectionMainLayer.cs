using System.Collections;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.SpecData;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum CharacterCollectionMainLayerTabType
    {
        All,
        EARTH = 1,
        WIND = 2,
        WATER = 3,
        FIRE = 4,
        DARK = 5,
        LIGHT = 6,
    }

    public class CharacterCollectionMainLayer : CachedMonoBehaviour
    {
        [SerializeField] private ScrollRect _characterScrollRect;
        [SerializeField] private GameObject _characterCardSlotObject;

        private CharacterCollectionMainLayerTabType _currentMainLayerTabType = CharacterCollectionMainLayerTabType.All;

        private List<SpecCharacter> _totalCharacterList;      // 전체 캐릭터 리스트
        private List<CharacterCardSlot> _characterCardSlotList = new List<CharacterCardSlot>();

        private CharacterCollectionPopup _parentCollectionPopup;

        public void InitLayer(CharacterCollectionPopup _parentPopup)
        {
            _parentCollectionPopup = _parentPopup;

            _currentMainLayerTabType = CharacterCollectionMainLayerTabType.All;

            SetCharacterCollectionUI();
        }

        public void RefreshLayer()
        {

        }

        public void OnClickTabToggleButton(int tabIndex)
        {
            _currentMainLayerTabType = (CharacterCollectionMainLayerTabType)tabIndex;

            FilterCharacterList(_currentMainLayerTabType);
        }

        private void SetCharacterCollectionUI()
        {
            ClearList();

            _totalCharacterList = SpecDataManager.Instance.GetCharacterListByCharacterType(CharacterType.CHARACTER);

            foreach (var characterData in _totalCharacterList)
            {
                GameObject newCardObject = Instantiate(_characterCardSlotObject, _characterScrollRect.content);
                CharacterCardSlot slot = newCardObject.GetComponent<CharacterCardSlot>();
                slot.SetCharcacterSlot(characterData, _parentCollectionPopup);

                _characterCardSlotList.Add(slot);
            }

            _characterScrollRect.verticalNormalizedPosition = 1;
        }

        private void FilterCharacterList(CharacterCollectionMainLayerTabType targetType)
        {
            _characterCardSlotList.ForEach(slot =>
            {
                if (targetType == CharacterCollectionMainLayerTabType.All)
                {
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive((int)slot.CharacterData.element_type == (int)targetType);
                }
            });

            _characterScrollRect.verticalNormalizedPosition = 1;
        }

        private void ClearList()
        {
            _characterCardSlotList.Clear();

            BMUtil.RemoveChildObjects(_characterScrollRect.content);
        }
    }
}
