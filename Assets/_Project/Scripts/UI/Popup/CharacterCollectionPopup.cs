using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum CharacterCollectionPopupTabType
    {
        MAIN,
        MAIN_DETAIL,
        GROW,
        SKILL,
    }

    public class CharacterCollectionPopup : UILayerPopupBase
    {
        [SerializeField] private GameObject _dimmedBGLayerObject;

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
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.Char_User_Exp_Item, TopPanelType.Char_User_Exp_Item_2);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 캐릭터 리셋 데이터 업데이트
            UserDataManager.Instance.UpdateResetCharacterCount();

            _currentTabType = CharacterCollectionPopupTabType.MAIN;
            _currentCharacterID = 0;

            ChangeTabType(_currentTabType, true);
        }

        public void OnClickLeftButton()
        {
            if (_currentCharacterID == 0) return;

            var leftCharacterID = SpecDataManager.Instance.GetLeftOwnedCharacterId(_currentCharacterID);
            _currentCharacterID = leftCharacterID;
            ChangeTabType(_currentTabType, true);
        }

        public void OnClickRightButton()
        {
            if (_currentCharacterID == 0) return;

            var rightCharacterID = SpecDataManager.Instance.GetRightOwnedCharacterId(_currentCharacterID);
            _currentCharacterID = rightCharacterID;
            ChangeTabType(_currentTabType, true);
        }

        // 외부 애니메이션 연출용 함수
        public void SetMaterialGlobalAlpha(float duration)
        {
            _detailMainBGLayer?.IllustMaterial?.SetFloat("_GlobalAlpha", 0);
            _detailMainBGLayer?.IllustMaterial?.DOFloat(1, "_GlobalAlpha", duration).SetEase(Ease.InQuad).SetDelay(0.24f);
        }

        public void SelectCharacterCard(int characterID)
        {
            _currentCharacterID = characterID;

            ChangeTabType(CharacterCollectionPopupTabType.GROW, true);
        }

        public void ChangeTabType(CharacterCollectionPopupTabType tabType, bool isFirstEnter = false)
        {
            if (_currentTabType == tabType && isFirstEnter == false) return;

            ClearLayer();

            _currentTabType = tabType;

            switch (_currentTabType)
            {
                case CharacterCollectionPopupTabType.MAIN:
                    _collectionMainLayer.gameObject.SetActive(true);
                    _collectionMainLayer.InitLayer(this);

                    if (isFirstEnter == false)
                    {
                        baseAnimator.SetTrigger("OnCollectionMain");
                    }

                    break;
                case CharacterCollectionPopupTabType.GROW:
                    _detailBGLayerObject.SetActive(true);
                    _detailMainBGLayer.gameObject.SetActive(true);
                    _detailMainBGLayer.InitLayer(_currentCharacterID, this);

                    _detailGrowLayer.gameObject.SetActive(true);
                    _detailGrowLayer.InitLayer(this, _currentCharacterID);

                    RefreshDimmedLayer();

                    //baseAnimator.SetBool("_onCollectionDetailGrow", true);
                    if (isFirstEnter)
                    {
                        baseAnimator.SetTrigger("OnCollectionDetailEntry");
                    }
                    else
                    {
                        baseAnimator.SetTrigger("OnCollectionDetailGrow");
                    }

                    break;
                case CharacterCollectionPopupTabType.SKILL:
                    _detailBGLayerObject.SetActive(true);
                    _detailMainBGLayer.gameObject.SetActive(true);
                    _detailMainBGLayer.InitLayer(_currentCharacterID, this);

                    _detailSkillLayer.gameObject.SetActive(true);
                    _detailSkillLayer.InitLayer(_currentCharacterID);

                    RefreshDimmedLayer();

                    baseAnimator.SetTrigger("OnCollectionDetailSkill");

                    break;
            }
        }

        public void RefreshTabLayer(CharacterCollectionPopupTabType tabType)
        {
            switch (tabType)
            {
                case CharacterCollectionPopupTabType.MAIN:
                    _collectionMainLayer.RefreshLayer();
                    break;
                case CharacterCollectionPopupTabType.MAIN_DETAIL:
                    _detailMainBGLayer.RefreshLayer();
                    break;
                case CharacterCollectionPopupTabType.GROW:
                    _detailGrowLayer.RefreshLayer();

                    RefreshDimmedLayer();
                    break;
                case CharacterCollectionPopupTabType.SKILL:
                    _detailSkillLayer.RefreshLayer();

                    RefreshDimmedLayer();
                    break;
            }
        }

        private void RefreshDimmedLayer()
        {
            // 딤드 레이어 설정
            bool isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(_currentCharacterID);

            _dimmedBGLayerObject.SetActive(!isHaveCharacter);
        }

        private void ClearLayer()
        {
            _dimmedBGLayerObject.SetActive(false);

            _detailBGLayerObject.SetActive(false);
            _detailMainBGLayer.gameObject.SetActive(false);

            _collectionMainLayer.gameObject.SetActive(false);
            _detailGrowLayer.gameObject.SetActive(false);
            _detailSkillLayer.gameObject.SetActive(false);
        }
    }
}
