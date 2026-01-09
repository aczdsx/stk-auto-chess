using System.Collections.Generic;
using R3;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 지휘자 스킬 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class CommanderSkillDataBridge
    {
        private CommanderSkillModel Model;

        // Public Observable 노출
        public Observable<(int slotIndex, int skillId)> OnSkillEquipped;
        public Observable<(int skillId, int level)> OnSkillAdded;
        public Observable<(int skillId, int level)> OnSkillLevelChanged;

        public CommanderSkillDataBridge()
        {
            Model = ServerDataManager.Instance.CommanderSkill;
            OnSkillEquipped = Model.OnSkillEquipped;
            OnSkillAdded = Model.OnSkillAdded;
            OnSkillLevelChanged = Model.OnSkillLevelChanged;
        }

        #region 장착 스킬 관련

        /// <summary>
        /// 슬롯에 장착된 스킬 ID 가져오기
        /// </summary>
        public int GetEquippedSkillId(int slotIndex)
        {
            return Model?.GetEquippedSkillId(slotIndex) ?? 0;
        }

        /// <summary>
        /// 스킬이 장착되어 있는지 확인
        /// </summary>
        public bool IsEquipped(int skillId)
        {
            return Model?.IsEquipped(skillId) ?? false;
        }

        /// <summary>
        /// 모든 슬롯에 스킬이 장착되어 있는지 확인
        /// </summary>
        public bool IsAllSlotsEquipped(int slotCount)
        {
            return Model?.IsAllSlotsEquipped(slotCount) ?? false;
        }

        /// <summary>
        /// 장착된 모든 스킬 ID 목록 가져오기
        /// </summary>
        public void GetAllEquippedSkillIds(List<int> output)
        {
            Model?.GetAllEquippedSkillIds(output);
        }

        #endregion

        #region 보유 스킬 관련

        /// <summary>
        /// 스킬 보유 여부 확인
        /// </summary>
        public bool HasSkill(int skillId)
        {
            return Model?.HasSkill(skillId) ?? false;
        }

        /// <summary>
        /// 스킬 레벨 가져오기
        /// </summary>
        public int GetSkillLevel(int skillId)
        {
            return Model?.GetSkillLevel(skillId) ?? 0;
        }

        /// <summary>
        /// 모든 보유 스킬 목록 가져오기
        /// </summary>
        public void GetAllOwnedSkills(List<(int skillId, int level)> output)
        {
            Model?.GetAllOwnedSkills(output);
        }

        /// <summary>
        /// 보유 스킬 개수
        /// </summary>
        public int OwnedSkillCount => Model?.OwnedSkillCount ?? 0;

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

        #endregion
    }
}
