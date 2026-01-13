using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class QuestSlot : CachedMonoBehaviour
    {
        [SerializeField] private QuestRewardSlot _questRewardSlot;
        [SerializeField] private TextMeshProUGUI _questTitleText;
        [SerializeField] private TextMeshProUGUI _questDescText;

        [Space]
        [SerializeField] private Slider _questProgressSlider;
        [SerializeField] private TextMeshProUGUI _questSliderText;

        [Space]
        [SerializeField] private GameObject _claimBGObject;
        [SerializeField] private GameObject _claimButtonObject;
        [SerializeField] private CAButton _claimButton;
        [SerializeField] private GameObject _completeLayerObject;
        [SerializeField] private GameObject _completeButtonObject;

        private QuestInfo _specQuestData;
        private UserQuestData _userQuestData;

        private List<RewardItem> _questRewardItemList = new List<RewardItem>();

        private QuestPopup _parentPopup;

        private void Awake()
        {
            _claimButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickGetRewardButton()).AddTo(this);
        }

        public void SetQuestSlot(QuestPopup parent, QuestInfo data)
        {
            if (data == null) return;

            _parentPopup = parent;

            _specQuestData = data;
            _userQuestData = UserDataManager.Instance.GetUserQuestData(_specQuestData.quest_id);

            _questTitleText.text = LanguageManager.Instance.GetLanguageText(_specQuestData.name_token);
            _questDescText.text = LanguageManager.Instance.GetLanguageText(_specQuestData.desc_token);

            // 리워드 데이터 세팅
            // ItemType의 삭제로 인해 변경.(new RewardItem(_specQuestData.item_type, _specQuestData.item_key, _specQuestData.item_count))
            var rewardItem = new RewardItem(_specQuestData.item_id, _specQuestData.item_count);
            _questRewardSlot.SetRewardSlot(rewardItem);

            _questRewardItemList.Add(rewardItem);

            RefreshQuestSlot(false);
        }

        public void RefreshQuestSlot(bool needRefreshData)
        {
            if (_specQuestData == null) return;
            if (_userQuestData == null) return;

            if (needRefreshData)
            {
                _userQuestData = UserDataManager.Instance.GetUserQuestData(_specQuestData.quest_id);
            }

            // 슬라이더 세팅
            _questSliderText.text = $"{_userQuestData.ActionCount}/{_specQuestData.need_count}";
            _questProgressSlider.maxValue = _specQuestData.need_count;
            _questProgressSlider.value = _userQuestData.ActionCount;

            // 버튼 상태 세팅
            _claimBGObject.SetActive(_userQuestData.QuestStateType == (int)QuestStateType.REWARD);
            _claimButtonObject.SetActive(_userQuestData.QuestStateType == (int)QuestStateType.REWARD);

            _completeLayerObject.SetActive(_userQuestData.QuestStateType == (int)QuestStateType.CLEAR);
            _completeButtonObject.SetActive(_userQuestData.QuestStateType == (int)QuestStateType.CLEAR);
        }

        private void OnClickGetRewardButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 퀘스트 상태 데이터 저장
            UserDataManager.Instance.SetUserQuestState(_userQuestData.QuestId, QuestStateType.CLEAR, true);

            // 보상 데이터 저장
            UserDataManager.Instance.IncreaseRewardItemList(_questRewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", _questRewardItemList)).Forget();

            _parentPopup?.RefreshPopup();
        }
    }
}
