using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
        [SerializeField] private Image _extraBGImage;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private RectTransform _dialogueTextRect;
        private Vector2 _tweenVector = new Vector2(1550f, 192f);

        private List<SpecDialogue> _dialogueList = new List<SpecDialogue>();

        private int currentDialogueSeq = 0;

        protected override void Awake()
        {
            base.Awake();

            _blockLayerButton.onClick.AddListener(OnClickNextDialogue);
            _blockLayerButton.onClick.AddListener(OnClickRefreshTextTween);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _blockLayerButton.onClick.RemoveListener(OnClickNextDialogue);
            _blockLayerButton.onClick.RemoveListener(OnClickRefreshTextTween);
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

            BMUtil.RemoveChildObjects(_characeterIllustParentObject.transform);

            // 추가 배경 설정
            if (string.IsNullOrWhiteSpace(currentDialougeData.bg_image) == false)
            {
                // todo.. bg 스플라이트 로드 처리
            }

            if (currentDialougeData.prefab_id > 0)
            {
                string characterPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, currentDialougeData.prefab_id);
                AddressablesUtil.Instantiate(characterPrefabName, _characeterIllustParentObject.transform);
            }

            _characterNameText.text = LanguageManager.Instance.GetLanguageText(currentDialougeData.character_name_token);
            _dialogueText.text = LanguageManager.Instance.GetLanguageText(currentDialougeData.text_desc_token);
        }

        // 다음 대화로 넘어가기
        private void OnClickNextDialogue()
        {
            currentDialogueSeq++;

            // 다이얼로그 종료 체크
            if (currentDialogueSeq >= _dialogueList.Count)
            {
                // 가이드 미션 완료 체크
                GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.END_DIALOGUE, 1);

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

        public void OnClickRefreshTextTween()
        {
            _dialogueText.DOFade(0, 0.3f).SetEase(Ease.OutQuad).From();
            _dialogueTextRect.DOSizeDelta(_tweenVector,0.3f).SetEase(Ease.OutQuad).From();
        }


    }
}
