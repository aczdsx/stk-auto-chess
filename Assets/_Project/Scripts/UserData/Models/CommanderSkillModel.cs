using System.Collections.Generic;
using R3;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 지휘자 스킬 데이터 모델
    /// 장착된 스킬과 보유한 스킬 목록 관리
    /// </summary>
    public class CommanderSkillModel
    {
        // 슬롯별 장착된 스킬 ID (key: slotIndex, value: skillId)
        private readonly Dictionary<int, int> _equippedSkills = new(4);

        // 보유한 스킬 목록 (key: skillId, value: level)
        private readonly Dictionary<int, int> _ownedSkills = new(16);

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<(int slotIndex, int skillId)> OnSkillEquipped = new();
        public readonly Subject<(int skillId, int level)> OnSkillAdded = new();
        public readonly Subject<(int skillId, int level)> OnSkillLevelChanged = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _equippedSkills.Clear();
            _ownedSkills.Clear();

            // 기본 슬롯 2개 초기화
            _equippedSkills[0] = 0;
            _equippedSkills[1] = 0;

            OnChanged.OnNext(Unit.Default);
        }

        #region 장착 스킬 관련

        /// <summary>
        /// 슬롯에 장착된 스킬 ID 가져오기
        /// </summary>
        public int GetEquippedSkillId(int slotIndex)
        {
            return _equippedSkills.TryGetValue(slotIndex, out var skillId) ? skillId : 0;
        }

        /// <summary>
        /// 슬롯에 스킬 장착
        /// </summary>
        internal void SetEquippedSkill(int slotIndex, int skillId)
        {
            _equippedSkills[slotIndex] = skillId;
            OnSkillEquipped.OnNext((slotIndex, skillId));
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 스킬이 장착되어 있는지 확인
        /// </summary>
        public bool IsEquipped(int skillId)
        {
            foreach (var kvp in _equippedSkills)
            {
                if (kvp.Value == skillId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 모든 슬롯에 스킬이 장착되어 있는지 확인
        /// </summary>
        public bool IsAllSlotsEquipped(int slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (!_equippedSkills.TryGetValue(i, out var skillId) || skillId == 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 장착된 모든 스킬 ID 목록 가져오기
        /// </summary>
        public void GetAllEquippedSkillIds(List<int> output)
        {
            if (output == null) return;

            output.Clear();
            foreach (var kvp in _equippedSkills)
            {
                if (kvp.Value > 0)
                    output.Add(kvp.Value);
            }
        }

        /// <summary>
        /// 장착 스킬 데이터 설정 (내부용)
        /// </summary>
        internal void SetEquippedSkills(Dictionary<int, int> equippedSkills)
        {
            _equippedSkills.Clear();
            foreach (var kvp in equippedSkills)
            {
                _equippedSkills[kvp.Key] = kvp.Value;
            }
            OnChanged.OnNext(Unit.Default);
        }

        #endregion

        #region 보유 스킬 관련

        /// <summary>
        /// 스킬 보유 여부 확인
        /// </summary>
        public bool HasSkill(int skillId)
        {
            return _ownedSkills.ContainsKey(skillId);
        }

        /// <summary>
        /// 스킬 레벨 가져오기
        /// </summary>
        public int GetSkillLevel(int skillId)
        {
            return _ownedSkills.TryGetValue(skillId, out var level) ? level : 0;
        }

        /// <summary>
        /// 스킬 추가 (내부용)
        /// </summary>
        internal void AddSkill(int skillId, int level = 1)
        {
            if (_ownedSkills.ContainsKey(skillId)) return;

            _ownedSkills[skillId] = level;
            OnSkillAdded.OnNext((skillId, level));
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 스킬 레벨 변경 (내부용)
        /// </summary>
        internal void SetSkillLevel(int skillId, int level)
        {
            if (!_ownedSkills.ContainsKey(skillId)) return;

            _ownedSkills[skillId] = level;
            OnSkillLevelChanged.OnNext((skillId, level));
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 보유 스킬 데이터 설정 (내부용)
        /// </summary>
        internal void SetOwnedSkills(Dictionary<int, int> ownedSkills)
        {
            _ownedSkills.Clear();
            foreach (var kvp in ownedSkills)
            {
                _ownedSkills[kvp.Key] = kvp.Value;
            }
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 모든 보유 스킬 목록 가져오기
        /// </summary>
        public void GetAllOwnedSkills(List<(int skillId, int level)> output)
        {
            if (output == null) return;

            output.Clear();
            foreach (var kvp in _ownedSkills)
            {
                output.Add((kvp.Key, kvp.Value));
            }
        }

        /// <summary>
        /// 보유 스킬 개수
        /// </summary>
        public int OwnedSkillCount => _ownedSkills.Count;

        #endregion

        #region UserDataManager 호환 메서드 (마이그레이션용)

        /// <summary>
        /// 장착된 지휘자 스킬 ID 가져오기 (UserDataManager 호환)
        /// </summary>
        public int GetEquippedCommanderSkillID(int targetSlot)
        {
            return GetEquippedSkillId(targetSlot);
        }

        /// <summary>
        /// 지휘자 스킬 레벨 가져오기 (UserDataManager 호환)
        /// </summary>
        public int GetUserCommanderSkillLevel(int commanderSkillID)
        {
            return GetSkillLevel(commanderSkillID);
        }

        /// <summary>
        /// 모든 슬롯에 스킬이 장착되어 있는지 확인 (UserDataManager 호환)
        /// </summary>
        public bool IsAllCommanderSkillsEquipped(int slotCount)
        {
            return IsAllSlotsEquipped(slotCount);
        }

        /// <summary>
        /// 스킬이 장착되어 있는지 확인 (UserDataManager 호환)
        /// </summary>
        public bool IsEquippedCommanderSkill(int skillID)
        {
            return IsEquipped(skillID);
        }

        /// <summary>
        /// 장착된 모든 지휘자 스킬 ID 목록 가져오기 (UserDataManager 호환)
        /// </summary>
        public List<int> GetAllEquippedCommanderSkillIDList()
        {
            var result = new List<int>();
            GetAllEquippedSkillIds(result);
            return result;
        }

        /// <summary>
        /// 지휘자 스킬 획득 여부 확인 (UserDataManager 호환)
        /// </summary>
        public bool IsOpenedCommanderSkill(int commanderSkillID)
        {
            return HasSkill(commanderSkillID);
        }

        /// <summary>
        /// 슬롯에 지휘자 스킬 장착 (UserDataManager 호환)
        /// </summary>
        public void SetEquippedCommanderSkill(int slotIndex, int skillId)
        {
            SetEquippedSkill(slotIndex, skillId);
        }

        #endregion
    }
}
