using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ChapterClearWindowPopup : UILayer
    {
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private TextMeshProUGUI _chapterClearTitleText;

        [Header("Reward Item Layer")]
        [SerializeField] private GameObject _rewardItemListLayerObject;
        [SerializeField] private GameObject _rewardItemSlotObject;

        [Header("Commander Skill Layer")]
        [SerializeField] private Image _commanderSkillIconImage;
        [SerializeField] private SpriteLoader _commanderSkillIconSpriteLoader;

        protected override void Awake()
        {
            base.Awake();

            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _getRewardButton.onClick.RemoveListener(OnClickGetRewardButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetChapterClearPopup();
        }

        private void SetChapterClearPopup()
        {
            ClearPopup();

            int lastClearStageID = UserDataManager.Instance.GetLatestClearUserStageID();
            var lastClearStageData = SpecDataManager.Instance.GetStageData(lastClearStageID);

            string chpaterClearString = LanguageManager.Instance.GetLanguageText("CHAPTER_CLEAR_GUIDE");
            _chapterClearTitleText.text = string.Format(chpaterClearString, lastClearStageData.chapter_id);

            int nextChapterID = lastClearStageData.chapter_id + 1;

            // 보상 데이터 세팅
            List<RewardItem> newRewardItemList = new List<RewardItem>();
            var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(ContentType.CHAPTER, lastClearStageData.chapter_id, lastClearStageData.difficulty_type);

            foreach (var rewardItem in rewardInfoList)
            {
                GameObject newRewardItem = Instantiate(_rewardItemSlotObject, _rewardItemListLayerObject.transform);
                RewardItemSlot rewardItemSlot = newRewardItem.GetComponent<RewardItemSlot>();

                // ItemType의 삭제로 인해 변경.(new RewardItem(rewardItem.item_type, rewardItem.item_key, rewardItem.item_count))
                RewardItem newReward = new RewardItem(rewardItem.item_id, rewardItem.item_count);

                rewardItemSlot.SetRewardSlot(newReward);

                newRewardItemList.Add(newReward);
            }

            // 지휘자 스킬 데이터 세팅
            var commanderSkillDataList = SpecDataManager.Instance.GetCommanderSkillList(nextChapterID);
            bool isExistCommanderSkill = commanderSkillDataList.Count > 0;

            // 애니메이션 컨트롤
            if (isExistCommanderSkill)
            {
                _commanderSkillIconSpriteLoader.SetSprite(SpriteNameParser.GetCommanderSkillSprite(commanderSkillDataList[0].commander_skill_id)).Forget();

                baseAnimator.SetTrigger("SetCommanderSkill");
            }
            else
            {
                baseAnimator.SetTrigger("SetRewardOnly");
            }

            // 보상 수령 처리 (지휘자 스킬은 UserData에서 이미 획득 처리)
            UserDataManager.Instance.IncreaseRewardItemList(newRewardItemList, true);
        }

        private void OnClickGetRewardButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_rewardItemListLayerObject.transform);
        }
    }
}
