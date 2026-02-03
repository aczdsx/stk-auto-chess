using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ChapterClearWindowPopup : UILayerPopupBase
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

            _getRewardButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickGetRewardButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetChapterClearPopupAsync().Forget();
        }

        private async UniTask SetChapterClearPopupAsync()
        {
            ClearPopup();

            int lastClearStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
            var lastClearStageData = SpecDataManager.Instance.GetStageData(lastClearStageID);

            {
                
                // 지휘자 스킬 데이터 세팅
                int nextChapterID = lastClearStageData.chapter_id + 1;
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
            }

            string chpaterClearString = LanguageManager.Instance.GetDefaultText("CHAPTER_CLEAR_GUIDE");
            _chapterClearTitleText.text = string.Format(chpaterClearString, lastClearStageData.chapter_id);

            var rewardInfo = SpecDataManager.Instance.GetSpecRewardInfo(ContentType.CHAPTER, lastClearStageData.chapter_id, lastClearStageData.difficulty_type);
            // 서버에 보상 수령 요청
            var resp = await NetManager.Instance.CustomLobby.ClaimOtherRewardAsync((uint)rewardInfo.reward_id);
            if (resp != null && resp.IsSuccess && resp.Rewards != null && resp.Rewards.Count > 0)
            {
                // 보상 수령 처리
                ClientProgressData.Get().AddReceivedRewardId(rewardInfo.reward_id);

                for (int i = 0; i < resp.Rewards.Count; i++)
                {
                    var rewardItem = new RewardItem(resp.Rewards[i]);
                    GameObject newRewardItem = Instantiate(_rewardItemSlotObject, _rewardItemListLayerObject.transform);
                    RewardItemSlot rewardItemSlot = newRewardItem.GetComponent<RewardItemSlot>();
                    rewardItemSlot.SetRewardSlot(rewardItem);
                }
            }
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
