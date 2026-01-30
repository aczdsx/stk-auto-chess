using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.CommanderService.CommanderServiceClient))]
    public partial class CommanderService
    {
        /// <summary>
        /// 지휘자 스킬 목록 조회
        /// </summary>
        public async UniTask<CommanderListSkillResponse> ListSkillAsync(CancellationToken cancellationToken = default)
        {
            CommanderListSkillResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListSkillAsync,
                new CommanderListSkillRequest(),
                cancellationToken: cancellationToken
            );

            // CommanderSkillModel 갱신
            if (resp is { IsSuccess: true, SkillList: not null })
            {
                ServerDataManager.Instance.CommanderSkill.SetCommanderSkillList(resp.SkillList);
            }

            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 장착
        /// </summary>
        public async UniTask<CommanderEquipSkillResponse> EquipSkillAsync(uint slotIndex, uint commanderSkillId, CancellationToken cancellationToken = default)
        {
            CommanderEquipSkillResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.EquipSkillAsync,
                new CommanderEquipSkillRequest
                {
                    SlotIndex = slotIndex,
                    CommanderSkillId = commanderSkillId
                },
                cancellationToken: cancellationToken
            );

            // CommanderSkillModel 갱신
            if (resp is { IsSuccess: true })
            {
                ServerDataManager.Instance.CommanderSkill.SetEquippedCommanderSkill((int)slotIndex, (int)commanderSkillId);
            }

            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 장착 해제
        /// </summary>
        public async UniTask<CommanderUnEquipSkillResponse> UnEquipSkillAsync(uint slotIndex, CancellationToken cancellationToken = default)
        {
            CommanderUnEquipSkillResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.UnEquipSkillAsync,
                new CommanderUnEquipSkillRequest { SlotIndex = slotIndex },
                cancellationToken: cancellationToken
            );

            // CommanderSkillModel 갱신
            if (resp is { IsSuccess: true })
            {
                ServerDataManager.Instance.CommanderSkill.SetEquippedCommanderSkill((int)slotIndex, 0);
            }

            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 레벨업
        /// </summary>
        public async UniTask<CommanderLevelUpSkillResponse> LevelUpSkillAsync(uint slotIndex, CancellationToken cancellationToken = default)
        {
            CommanderLevelUpSkillResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.LevelUpSkillAsync,
                new CommanderLevelUpSkillRequest { SlotIndex = slotIndex },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // CommanderSkillModel 갱신
                if (resp.Skill is not null)
                {
                    ServerDataManager.Instance.CommanderSkill.SetSkillLevel((int)resp.Skill.CommanderSkillId, (int)resp.Skill.Level);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas is { Count: > 0 })
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 승급
        /// </summary>
        public async UniTask<CommanderPromoteSkillResponse> PromoteSkillAsync(uint commanderSkillId, uint promotionSlot, uint promotionOptionId, CancellationToken cancellationToken = default)
        {
            CommanderPromoteSkillResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.PromoteSkillAsync,
                new CommanderPromoteSkillRequest
                {
                    CommanderSkillId = commanderSkillId,
                    PromotionSlot = promotionSlot,
                    PromotionOptionId = promotionOptionId
                },
                cancellationToken: cancellationToken
            );

            // CommanderSkillModel 갱신
            if (resp is { IsSuccess: true, Skill: not null })
            {
                ServerDataManager.Instance.CommanderSkill.UpdateSkill(resp.Skill);
            }

            return resp;
        }
    }
}
