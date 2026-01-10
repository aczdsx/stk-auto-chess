using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

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
            CommanderListSkillResponse resp = await ExecuteAsync(
                ServiceClient.ListSkillAsync,
                new CommanderListSkillRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 장착
        /// </summary>
        public async UniTask<CommanderEquipSkillResponse> EquipSkillAsync(uint slotIndex, uint commanderSkillId, CancellationToken cancellationToken = default)
        {
            CommanderEquipSkillResponse resp = await ExecuteAsync(
                ServiceClient.EquipSkillAsync,
                new CommanderEquipSkillRequest
                {
                    SlotIndex = slotIndex,
                    CommanderSkillId = commanderSkillId
                },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 장착 해제
        /// </summary>
        public async UniTask<CommanderUnEquipSkillResponse> UnEquipSkillAsync(uint slotIndex, CancellationToken cancellationToken = default)
        {
            CommanderUnEquipSkillResponse resp = await ExecuteAsync(
                ServiceClient.UnEquipSkillAsync,
                new CommanderUnEquipSkillRequest { SlotIndex = slotIndex },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 레벨업
        /// </summary>
        public async UniTask<CommanderLevelUpSkillResponse> LevelUpSkillAsync(uint slotIndex, CancellationToken cancellationToken = default)
        {
            CommanderLevelUpSkillResponse resp = await ExecuteAsync(
                ServiceClient.LevelUpSkillAsync,
                new CommanderLevelUpSkillRequest { SlotIndex = slotIndex },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 지휘자 스킬 승급
        /// </summary>
        public async UniTask<CommanderPromoteSkillResponse> PromoteSkillAsync(uint commanderSkillId, uint promotionSlot, uint promotionOptionId, CancellationToken cancellationToken = default)
        {
            CommanderPromoteSkillResponse resp = await ExecuteAsync(
                ServiceClient.PromoteSkillAsync,
                new CommanderPromoteSkillRequest
                {
                    CommanderSkillId = commanderSkillId,
                    PromotionSlot = promotionSlot,
                    PromotionOptionId = promotionOptionId
                },
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
