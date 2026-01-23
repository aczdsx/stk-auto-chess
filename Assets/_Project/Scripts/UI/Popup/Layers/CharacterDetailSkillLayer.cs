using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace CookApps.AutoBattler
{
    public class CharacterDetailSkillLayer : CachedMonoBehaviour
    {
        [SerializeField] private SkillTooltipPopup _skillTooltipPopup;
        [SerializeField] private NonSkillTooltipPopup _passiveSkillTooltipPopup;
        [SerializeField] private NonSkillTooltipPopup _jobSkillTooltipPopup;

        [Space(10)]
        [SerializeField] private CAButton _skillInfoButton;
        [SerializeField] private CAButton _passiveSkillInfoButton;
        [SerializeField] private CAButton _jobSkillInfoButton;

        [Space(10)]
        [SerializeField] private Image _normalSkillIconImage;
        [SerializeField] private SpriteLoader _normalSkillIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _normalSkillNameText;

        [Space(10)]
        [SerializeField] private Image _passiveSkillIconImage;
        [SerializeField] private SpriteLoader _passiveSkillIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _passiveSkillNameText;

        [Space(10)]
        [SerializeField] private Image _jobSkillIconImage;
        [SerializeField] private SpriteLoader _jobSkillIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _jobSkillNameText;

        private CharacterInfo _specCharacterData;
        private CharacterData _userCharacterData;// 레벨을 위해 필요한듯
        private List<SkillActive> _specActiveSkillBaseData = new List<SkillActive>();
        private List<SkillPassive> _specPassiveSkillBaseData = new List<SkillPassive>();
        private SkillJob _specJobSkillBaseData;

        private void Awake()
        {
            _skillInfoButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickSkillInfoButton()).AddTo(this);
            _passiveSkillInfoButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickPassiveSkillInfoButton()).AddTo(this);
            _jobSkillInfoButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickJobSkillInfoButton()).AddTo(this);
        }

        public void InitLayer(int characterID)
        {
            _skillTooltipPopup.gameObject.SetActive(false);

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = ServerDataManager.Instance.Character.GetCharacter(characterID);

            SetSkillLayer();

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        public void RefreshLayer()
        {

        }

        private void SetSkillLayer()
        {
            if (_specCharacterData == null) return;
            if (_userCharacterData == null) return;

            var specActiveSkillList = SpecDataManager.Instance.GetSkillDataListByPrefabID(_specCharacterData.prefab_id);
            if (specActiveSkillList != null && specActiveSkillList.Count > 0)
            {
                _specActiveSkillBaseData = specActiveSkillList;

                _normalSkillIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSkillSprite(_specActiveSkillBaseData[0].skill_group_id)).Forget();
                _normalSkillNameText.text = LanguageManager.Instance.GetDefaultText(_specActiveSkillBaseData[0].skill_name_token);
            }

            var specPassiveSkillList = SpecDataManager.Instance.GetSkillPassiveDataListByPrefabID(_specCharacterData.prefab_id);
            if (specPassiveSkillList != null && specPassiveSkillList.Count > 0)
            {
                _specPassiveSkillBaseData = specPassiveSkillList;
                _passiveSkillIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPassiveSkillSprite(specPassiveSkillList[0].passive_group_id)).Forget();
                _passiveSkillNameText.text = LanguageManager.Instance.GetDefaultText(specPassiveSkillList[0].passive_name_token);
            }

            var specJobSkillList = SpecDataManager.Instance.GetJobPassiveList(_specCharacterData.character_position_type);
            if (specJobSkillList != null && specJobSkillList.Count > 0)
            {
                _specJobSkillBaseData = specJobSkillList[0][0];
                _jobSkillIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterJobSkillSprite(_specCharacterData.character_position_type)).Forget();
                _jobSkillNameText.text = LanguageManager.Instance.GetDefaultText(specJobSkillList[0][0].jobs_name_token);
            }


        }

        private void OnClickSkillInfoButton()
        {
            if (_skillTooltipPopup == null) return;
            if (_specActiveSkillBaseData == null) return;

            _skillTooltipPopup.SetSkillToolTipPopup(_specActiveSkillBaseData);

            _skillTooltipPopup.gameObject.SetActive(true);
        }

        private void OnClickPassiveSkillInfoButton()
        {
            if (_passiveSkillTooltipPopup == null) return;
            if (_specPassiveSkillBaseData == null) return;

            _passiveSkillTooltipPopup.SetPassiveSkillToolTipPopup(_specPassiveSkillBaseData);
            _passiveSkillTooltipPopup.gameObject.SetActive(true);
        }

        private void OnClickJobSkillInfoButton()
        {
            if (_jobSkillTooltipPopup == null) return;
            if (_specJobSkillBaseData == null) return;

            _jobSkillTooltipPopup.SetJobSkillToolTipPopup(_specJobSkillBaseData, _specCharacterData.character_position_type);
            _jobSkillTooltipPopup.gameObject.SetActive(true);
        }
    }
}
