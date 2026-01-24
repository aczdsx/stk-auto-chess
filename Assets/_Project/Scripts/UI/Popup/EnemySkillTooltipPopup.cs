using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class EnemySkillTooltipPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimButton;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _skillNameText;

        [Space(10)]
        [SerializeField] private List<SpriteLoader> _rangeIconSpriteLoaders;
        [SerializeField] private List<TextMeshProUGUI> _rangeTexts;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickClose()).AddTo(this);
            _dimButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickClose()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            if (param is MonsterInfo monsterInfo)
            {
                SetSkillTooltip(monsterInfo);
            }
        }

        private void SetSkillTooltip(MonsterInfo monsterInfo)
        {
            if (monsterInfo == null) return;
            if (monsterInfo.skill_ids == null || monsterInfo.skill_ids.Length == 0) return;

            var skillList = SpecDataManager.Instance.GetSkillDataList(monsterInfo.skill_ids[0]);
            if (skillList == null || skillList.Count == 0) return;

            var skillData = skillList[0];
            _skillNameText.text = LanguageManager.Instance.GetDefaultText(skillData.skill_name_token);

            _rangeIconSpriteLoaders[0].SetSprite(SpriteNameParser.GetCharacterSkillSprite(skillData.skill_group_id)).Forget();
            _rangeTexts[0].text = LanguageManager.Instance.GetDefaultText(skillData.skill_desc_token);
        }

        private void OnClickClose()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
