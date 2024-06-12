using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/DialogueShowPopup.prefab")]
    public class DialogueShowPopup : UILayer
    {
        [Header("Chracter Layer")]
        [SerializeField] private GameObject _characeterIllustParentObject;
        [SerializeField] private TextMeshProUGUI _characterNameText;

        [Header("Dialogue Layer")]
        [SerializeField] private CAButton _blockLayerButton;
        [SerializeField] private TextMeshProUGUI _dialogueText;

        private List<SpecDialogue> _dialogueList = new List<SpecDialogue>();

        private int currentDialogueSeq = 0;

        protected override void Awake()
        {
            base.Awake();

            _blockLayerButton.onClick.AddListener(OnClickNextDialogue);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _blockLayerButton.onClick.RemoveListener(OnClickNextDialogue);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            int dialogueGroupID = (int)param;
            _dialogueList = SpecDataManager.Instance.GetDialogueListByGroupID(dialogueGroupID);

            ClearPopup();

            SetDialogueData(currentDialogueSeq);
        }

        private void SetDialogueData(int seq)
        {
            if (_dialogueList == null || _dialogueList.Count == 0) return;

            var currentDialougeData = _dialogueList[seq];

            string characterPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, currentDialougeData.prefab_id);
            AddressablesUtil.Instantiate(characterPrefabName, _characeterIllustParentObject.transform);
            _characterNameText.text = currentDialougeData.character_name_token;

            _dialogueText.text = currentDialougeData.text_desc_token;
        }

        // 다음 대화로 넘어가기
        private void OnClickNextDialogue()
        {
            currentDialogueSeq++;

            if (currentDialogueSeq >= _dialogueList.Count)
            {
                SceneUILayerManager.Instance.PopUILayer(this);
                return;
            }

            SetDialogueData(currentDialogueSeq);
        }

        private void ClearPopup()
        {
            currentDialogueSeq = 0;

            BMUtil.RemoveChildObjects(_characeterIllustParentObject.transform);
        }
    }
}
