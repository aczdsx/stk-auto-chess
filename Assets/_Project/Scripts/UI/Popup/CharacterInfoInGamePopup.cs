using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class CharacterInfoInGamePopup : UILayerPopupBase
    {

        [Header("Character Info")]
        [SerializeField] private SpriteLoader _characterIllustSpriteLoader;
        [SerializeField] private TextMeshProUGUI _characterName;
        [SerializeField] private SpriteLoader _characterStigmaSpriteLoader;
        [SerializeField] private TextMeshProUGUI _characterInGameRole;

        [Header("Synergy")]
        [SerializeField] private SpriteLoader _elementIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _elementName;
        [SerializeField] private SpriteLoader _asterismIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _asterismName;

        [Header("Skill And Passive")]
        [SerializeField] private SpriteLoader _skillIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _skillName;
        [SerializeField] private TextMeshProUGUI _skillDesc;
        [SerializeField] private GameObject _skillRangeContainer;
        [SerializeField] private GameObject _skillRange0;
        [SerializeField] private GameObject _skillRange1;
        [SerializeField] private SpriteLoader _skillRangeLoader0;
        [SerializeField] private SpriteLoader _skillRangeLoader1;

        [Space(5)]
        [SerializeField] private SpriteLoader _passiveIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _passiveName;
        [SerializeField] private TextMeshProUGUI _passiveDesc;

        private ISpecCharacterInfo _specData;

        public readonly struct PopupParam
        {
            public readonly int ChampionSpecId;
            public readonly int StarLevel;

            public PopupParam(int champSpecId, int starLevel = 0)
            {
                ChampionSpecId = champSpecId;
                StarLevel = starLevel;
            }
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            if (param is PopupParam popupParam)
            {
                _specData = SpecDataManager.Instance.GetSpecCharacter(popupParam.ChampionSpecId);
            }

            if (_specData == null) return;

            SetCharacterInfo();
            SetSynergyInfo();
            SetSkillInfo();
            SetPassiveInfo();
        }

        public void Refresh(PopupParam popupParam)
        {
            _specData = SpecDataManager.Instance.GetSpecCharacter(popupParam.ChampionSpecId);
            if (_specData == null) return;

            SetCharacterInfo();
            SetSynergyInfo();
            SetSkillInfo();
            SetPassiveInfo();
        }

        private void SetCharacterInfo()
        {
            _characterName.text = LanguageManager.Instance.GetDefaultText(_specData.name_token);
            _characterIllustSpriteLoader.SetSprite(SpriteNameParser.GetCharacterIllustSprite(_specData.prefab_id)).Forget();
            _characterStigmaSpriteLoader.SetSprite(SpriteNameParser.GetCharacterStigmaSprite(_specData.prefab_id)).Forget();
            _characterInGameRole.text = _specData.character_position_type.ToString();
        }

        private void SetSynergyInfo()
        {
            // 속성 시너지
            var elementType = _specData.character_element_type;
            _elementIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(elementType, isActive: true)).Forget();
            var elementSynergyList = SpecDataManager.Instance.GetSpecSynergyList(elementType);
            if (elementSynergyList != null && elementSynergyList.Count > 0)
            {
                _elementName.text = LanguageManager.Instance.GetDefaultText(elementSynergyList[0].name_token);
            }

            // 성군 시너지
            var asterismType = _specData.character_stella_type;
            if (asterismType != SynergyType.NONE)
            {
                _asterismIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(asterismType, isActive: true)).Forget();
                var asterismSynergyList = SpecDataManager.Instance.GetSpecSynergyList(asterismType);
                if (asterismSynergyList != null && asterismSynergyList.Count > 0)
                {
                    _asterismName.text = LanguageManager.Instance.GetDefaultText(asterismSynergyList[0].name_token);
                }
            }
        }

        private void SetSkillInfo()
        {
            if (_specData.skill_ids == null || _specData.skill_ids.Length == 0) return;

            var skillList = SpecDataManager.Instance.GetSkillDataList(_specData.skill_ids[0]);
            if (skillList == null || skillList.Count == 0) return;

            var skill = skillList[0];
            _skillIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSkillSprite(skill.skill_group_id)).Forget();
            _skillName.text = LanguageManager.Instance.GetDefaultText(skill.skill_name_token);

            string descText = LanguageManager.Instance.GetDefaultText(skill.skill_desc_token);
            _skillDesc.text = FormatDescWithBaseRates(descText, skillList);

            SetRangeImages(_specData.skill_range, _skillRangeContainer, _skillRangeLoader0, _skillRangeLoader1, _skillRange0, _skillRange1);
        }

        private void SetPassiveInfo()
        {
            if (_specData.passive_skill_id <= 0) return;

            var passiveList = SpecDataManager.Instance.GetSkillPassiveDataList(_specData.passive_skill_id);
            if (passiveList == null || passiveList.Count == 0) return;

            var passive = passiveList[0];
            _passiveIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPassiveSkillSprite(passive.passive_group_id)).Forget();
            _passiveName.text = LanguageManager.Instance.GetDefaultText(passive.passive_name_token);

            string descText = LanguageManager.Instance.GetDefaultText(passive.passive_desc_token);
            _passiveDesc.text = FormatDescWithBaseRates(descText, passiveList);

        }

        private static void SetRangeImages(string[] rangeNames, GameObject container,
            SpriteLoader loader0, SpriteLoader loader1, GameObject go0, GameObject go1)
        {
            if (rangeNames == null || rangeNames.Length == 0
                || (rangeNames.Length == 1 && rangeNames[0] == "NONE"))
            {
                container.SetActive(false);
                return;
            }

            container.SetActive(true);

            loader0.SetSprite(rangeNames[0]).Forget();
            go0.SetActive(true);

            if (rangeNames.Length > 1)
            {
                loader1.SetSprite(rangeNames[1]).Forget();
                go1.SetActive(true);
            }
            else
            {
                go1.SetActive(false);
            }
        }

        private static string FormatDescWithBaseRates<T>(string text, List<T> dataList) where T : class
        {
            if (string.IsNullOrEmpty(text)) return text;

            int maxIndex = -1;
            int maxCheck = Mathf.Min(dataList.Count, CharacterController.CharacterActiveSkillStatCnt);
            for (int i = 0; i < maxCheck; i++)
            {
                if (text.Contains($"{{{i}}}"))
                    maxIndex = i;
            }

            if (maxIndex < 0) return text;

            int requiredCount = maxIndex + 1;
            if (requiredCount > dataList.Count) return text;

            var args = new object[requiredCount];
            for (int i = 0; i < requiredCount; i++)
            {
                args[i] = dataList[i] switch
                {
                    SkillActive active => active.base_rate,
                    SkillPassive passive => passive.base_rate,
                    _ => 0f
                };
            }
            return string.Format(text, args);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();

            _specData = null;
            _characterIllustSpriteLoader.UnloadSprite();
            _characterStigmaSpriteLoader.UnloadSprite();
            _elementIconSpriteLoader.UnloadSprite();
            _asterismIconSpriteLoader.UnloadSprite();
            _skillIconSpriteLoader.UnloadSprite();
            _skillRangeLoader0.UnloadSprite();
            _skillRangeLoader1.UnloadSprite();
            _passiveIconSpriteLoader.UnloadSprite();
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
