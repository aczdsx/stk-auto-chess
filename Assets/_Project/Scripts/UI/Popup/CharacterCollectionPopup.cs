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
    public enum CharacterCollectionPopupTabType
    {
        MAIN,
        GROW,
        SKILL,
    }

    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/CharacterCollectionPopup/CharacterCollectionPopup.prefab")]
    public class CharacterCollectionPopup : UILayer
    {
        [Header("BG Layer")]
        [SerializeField] private GameObject _detailBGLayerObject;
        [SerializeField] private CharacterDetailMainLayer _detailMainBGLayer;

        [Header("Layer")]
        [SerializeField] private CharacterCollectionMainLayer _collectionMainLayer;
        [SerializeField] private CharacterDetailGrowLayer _detailGrowLayer;
        [SerializeField] private CharacterDetailSkillLayer _detailSkillLayer;

        private CharacterCollectionPopupTabType _currentTabType = CharacterCollectionPopupTabType.MAIN;
        private int _currentCharacterID;


        public CharacterCollectionPopupTabType CurrentTabType => _currentTabType;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.Char_User_Exp_Item);

            _currentTabType = CharacterCollectionPopupTabType.MAIN;
            _currentCharacterID = 0;

            ChangeTabType(_currentTabType, true);
        }

        public void SelectCharacterCard(int characterID)
        {
            _currentCharacterID = characterID;

            ChangeTabType(CharacterCollectionPopupTabType.GROW);
        }

        public void ChangeTabType(CharacterCollectionPopupTabType tabType, bool isFirstInit = false)
        {
            if (_currentTabType == tabType && isFirstInit == false) return;

            ClearLayer();

            _currentTabType = tabType;

            switch (_currentTabType)
            {
                case CharacterCollectionPopupTabType.MAIN:
                    _collectionMainLayer.gameObject.SetActive(true);
                    _collectionMainLayer.InitLayer(this);

                    //baseAnimator.SetBool("_onCollectionMain", true);
                    if (isFirstInit == false)
                    {
                        baseAnimator.SetTrigger("OnCollectionMain");
                    }

                    break;
                case CharacterCollectionPopupTabType.GROW:
                    _detailBGLayerObject.SetActive(true);
                    _detailMainBGLayer.gameObject.SetActive(true);
                    _detailMainBGLayer.InitLayer(_currentCharacterID, this);

                    _detailGrowLayer.gameObject.SetActive(true);
                    _detailGrowLayer.InitLayer(_currentCharacterID);

                    //baseAnimator.SetBool("_onCollectionDetailGrow", true);
                    baseAnimator.SetTrigger("OnCollectionDetailGrow");

                    break;
                case CharacterCollectionPopupTabType.SKILL:
                    _detailBGLayerObject.SetActive(true);
                    _detailMainBGLayer.gameObject.SetActive(true);
                    _detailMainBGLayer.InitLayer(_currentCharacterID, this);

                    _detailSkillLayer.gameObject.SetActive(true);
                    _detailSkillLayer.InitLayer(_currentCharacterID);
                    break;
            }
        }

        private void ClearLayer()
        {
            _detailBGLayerObject.SetActive(false);
            _detailMainBGLayer.gameObject.SetActive(false);

            _collectionMainLayer.gameObject.SetActive(false);
            _detailGrowLayer.gameObject.SetActive(false);
            _detailSkillLayer.gameObject.SetActive(false);
        }
    }
}
