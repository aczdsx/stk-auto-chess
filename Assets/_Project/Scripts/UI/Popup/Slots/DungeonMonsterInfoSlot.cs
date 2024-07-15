using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class DungeonMonsterInfoSlot : CachedMonoBehaviour
    {
        [Header("Monster Info")]
        [SerializeField] private TextMeshProUGUI _monsterNameText;
        [SerializeField] private TextMeshProUGUI _monsterBattlePointText;

        [Space]
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _classSynergyUI;

        private SpecDungeonMonster _specDungeonMonsterData;
        private SpecCharacter _specCharacterMonsterData;

        public void SetMonsterInfoSlot(SpecDungeonMonster data)
        {
            if (data == null) return;

            _specDungeonMonsterData = data;
            _specCharacterMonsterData = SpecDataManager.Instance.GetCharacterData(_specDungeonMonsterData.monster_id);

            _monsterNameText.text = LanguageManager.Instance.GetLanguageText(_specCharacterMonsterData.name_token);

            _elementSynergyUI.SetSynergyUI(_specCharacterMonsterData.element_type);
            _classSynergyUI.SetPositionSynergyUI(_specCharacterMonsterData.character_position_type);
        }
    }
}
