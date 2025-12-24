using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private SpriteLoader _characterSpriteLoader;
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _asterismSynergyUI;

        private CharacterStatData _statData;

        public void SetMonsterInfoSlot(CharacterStatData data)
        {
            if (data == null) return;

            _statData = data;

            _monsterBattlePointText.text = _statData.GetAttrValueCP().ToString("n0");
            _monsterNameText.text = $"Lv.{_statData.Level} " + LanguageManager.Instance.GetLanguageText(_statData.Spec.name_token);

            _characterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(_statData.Spec.prefab_id)).Forget();
            _elementSynergyUI.SetSynergyUI(_statData.Spec.character_element_type);
            _asterismSynergyUI.SetSynergyUI(_statData.Spec.character_stella_type);
        }
    }
}
