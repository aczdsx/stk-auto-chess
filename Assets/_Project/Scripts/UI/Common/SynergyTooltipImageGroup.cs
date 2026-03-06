using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 시너지 툴팁에서 캐릭터 아이콘 슬롯 리스트를 관리하는 그룹.
    /// GridLayoutGroup 하위에 슬롯을 풀링하며, 전투 참여 캐릭터를 등급순으로 앞에 정렬한다.
    /// </summary>
    public class SynergyTooltipImageGroup : CachedMonoBehaviour
    {
        [SerializeField] private SynergyTooltipImageSlot _slotPrefab;

        private readonly List<SynergyTooltipImageSlot> _slots = new();

        /// <summary>
        /// 슬롯에 바인딩할 캐릭터 데이터
        /// </summary>
        public struct CharacterSlotData
        {
            public int PrefabId;
            public GradeType Grade;
            public bool InBattle;
            public List<SynergyType> SynergyTypes;
        }

        /// <summary>
        /// 캐릭터 목록을 정렬 후 슬롯에 바인딩한다.
        /// 정렬: 전투 참여 캐릭터 먼저(등급 내림차순) → 미참여 캐릭터(등급 내림차순)
        /// </summary>
        public void SetCharacters(List<CharacterSlotData> characters)
        {
            // 1. 정렬: 전투 참여 먼저(등급 내림차순), 미참여 뒤(등급 내림차순)
            characters.Sort((a, b) =>
            {
                if (a.InBattle != b.InBattle)
                    return b.InBattle.CompareTo(a.InBattle);
                return b.Grade.CompareTo(a.Grade);
            });

            // 2. 슬롯 수 부족 → Instantiate 추가
            while (_slots.Count < characters.Count)
            {
                var slot = Instantiate(_slotPrefab, transform);
                _slots.Add(slot);
            }

            // 3. 데이터 바인딩 + 활성화
            for (int i = 0; i < characters.Count; i++)
            {
                var data = characters[i];
                _slots[i].SetCharacter(data.PrefabId, data.Grade, data.InBattle);
                _slots[i].SetSynergyIcons(data.SynergyTypes);
                _slots[i].SetActive(true);
                _slots[i].transform.SetSiblingIndex(i);
            }

            // 4. 남은 슬롯 비활성화 + 뒤로 밀기
            for (int i = characters.Count; i < _slots.Count; i++)
            {
                _slots[i].SetActive(false);
                _slots[i].transform.SetSiblingIndex(i);
            }
        }
    }
}
