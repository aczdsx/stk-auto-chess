using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class DungeonMonsterInfoSlot : CachedMonoBehaviour
    {
        [Header("Monster Info")]
        [SerializeField] private TextMeshProUGUI _monsterNameText;
        [SerializeField] private TextMeshProUGUI _monsterBattlePointText;

        [Space] 
        [SerializeField] private Image _characterImage;
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _asterismSynergyUI;

        private CharacterStatData _statData;

        public void SetMonsterInfoSlot(CharacterStatData data)
        {
            if (data == null) return;

            _statData = data;

            _monsterBattlePointText.text = _statData.GetAttrValue().ToString("n0");
            _monsterNameText.text = $"Lv.{_statData.Level} " + LanguageManager.Instance.GetLanguageText(_statData.Spec.name_token);

            _characterImage.sprite = ImageManager.Instance.GetCharacterSmallItemSprite(_statData.Spec.prefab_id);
            _elementSynergyUI.SetSynergyUI(_statData.Spec.character_element_type);
            _asterismSynergyUI.SetSynergyUI(_statData.Spec.character_stella_type);
        }
    }
}
