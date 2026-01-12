using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 지휘자 스킬 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class CommanderSkillDataBridge : DataBridgeBase
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
        public int GetEquippedCommanderSkillId(int slotIndex)
        {
            return Model?.GetEquippedCommanderSkillId(slotIndex) ?? 0;
        }

        /// <summary>
        /// 스킬이 장착되어 있는지 확인
        /// </summary>
        public bool IsEquippedCommanderSkill(int skillId)
        {
            return Model?.IsEquippedCommanderSkill(skillId) ?? false;
        }

        /// <summary>
        /// 모든 슬롯에 스킬이 장착되어 있는지 확인
        /// </summary>
        public bool IsAllCommanderSkillsEquipped(int slotCount)
        {
            return Model?.IsAllCommanderSkillsEquipped(slotCount) ?? false;
        }

        /// <summary>
        /// 장착된 모든 스킬 ID 목록 가져오기
        /// </summary>
        public List<int> GetAllEquippedCommanderSkillIdList()
        {
            return Model?.GetAllEquippedCommanderSkillIdList() ?? new List<int>();
        }

        #endregion

        #region 보유 스킬 관련

        /// <summary>
        /// 스킬 보유 여부 확인
        /// </summary>
        public bool IsOpenedCommanderSkill(int skillId)
        {
            return Model?.IsOpenedCommanderSkill(skillId) ?? false;
        }

        /// <summary>
        /// 스킬 레벨 가져오기
        /// </summary>
        public int GetUserCommanderSkillLevel(int skillId)
        {
            return Model?.GetUserCommanderSkillLevel(skillId) ?? 0;
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

        #region 서버 API 호출

        /// <summary>
        /// 지휘자 스킬 장착 (Fire and Forget)
        /// </summary>
        public void SetEquippedCommanderSkill(int slotIndex, int skillId)
        {
            SetEquippedCommanderSkillAsync(slotIndex, skillId).Forget();
        }

        /// <summary>
        /// 지휘자 스킬 장착 (서버 API 호출)
        /// </summary>
        public async UniTask<bool> SetEquippedCommanderSkillAsync(int slotIndex, int skillId)
        {
            try
            {
                var response = await NetManager.Instance.Commander.EquipSkillAsync((uint)slotIndex, (uint)skillId);
                // 서버 응답으로 모델 업데이트는 서버에서 푸시하거나 응답에서 처리
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to equip commander skill: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지휘자 스킬 장착 해제 (서버 API 호출)
        /// </summary>
        public async UniTask<bool> UnEquipCommanderSkillAsync(int slotIndex)
        {
            try
            {
                var response = await NetManager.Instance.Commander.UnEquipSkillAsync((uint)slotIndex);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to unequip commander skill: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지휘자 스킬 레벨업 (서버 API 호출)
        /// </summary>
        public async UniTask<bool> LevelUpCommanderSkillAsync(int slotIndex)
        {
            try
            {
                var response = await NetManager.Instance.Commander.LevelUpSkillAsync((uint)slotIndex);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to level up commander skill: {e.Message}");
                return false;
            }
        }

        #endregion
    }
}
