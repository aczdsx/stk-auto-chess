using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 준비 페이즈 시너지 행동 추상 베이스.
    /// Asterism 시너지(Supernova/Troubleshooter 등)의 준비 페이즈 상호작용을 정의.
    /// 전투 시작 시 ExportState → SynergyBehaviorBase로 전달.
    /// </summary>
    public abstract class SynergyPrepBehaviorBase
    {
        public int TraitId;
        public byte Tier;
        public byte PlayerIndex;

        /// <summary>시너지 활성화 시 (유닛 배치로 임계치 충족)</summary>
        public virtual void OnActivate(GameWorld world) { }

        /// <summary>시너지 비활성화 시 (유닛 회수로 임계치 미달)</summary>
        public virtual void OnDeactivate(GameWorld world) { }

        /// <summary>티어 변경 시 (유닛 수 변동으로 단계 변경)</summary>
        public virtual void OnTierChanged(GameWorld world, byte oldTier, byte newTier) { }

        /// <summary>보드 구성 변경 시 (매 배치/회수/교환마다)</summary>
        public virtual void OnBoardChanged(GameWorld world) { }

        /// <summary>플레이어 커맨드 처리 (SetSynergyPrepTarget 등)</summary>
        public virtual void HandleCommand(GameWorld world, in GameCommand cmd) { }

        /// <summary>combat behavior에 전달할 데이터</summary>
        public int PrepTargetEntityId = -1;
        public int PrepParam0;
        public int PrepParam1;
    }

    /// <summary>
    /// 준비 페이즈 시너지 행동 팩토리.
    /// SynergyType → SynergyPrepBehaviorBase 인스턴스 생성.
    /// </summary>
    public static class SynergyPrepBehaviorFactory
    {
        public static SynergyPrepBehaviorBase Create(SynergyType type, byte tier, int traitId, byte playerIndex)
        {
            SynergyPrepBehaviorBase behavior = type switch
            {
                // === 구현체 추가 시 여기만 수정 ===
                // SynergyType.SUPERNOVA => new SynergyPrepSupernova(),
                // SynergyType.TROUBLESHOOTER => new SynergyPrepTroubleShooter(),
                _ => null,
            };

            if (behavior != null)
            {
                behavior.TraitId = traitId;
                behavior.Tier = tier;
                behavior.PlayerIndex = playerIndex;
            }

            return behavior;
        }

        /// <summary>해당 SynergyType이 준비 페이즈 행동을 필요로 하는지</summary>
        public static bool NeedsPrepBehavior(SynergyType type) => type switch
        {
            SynergyType.SUPERNOVA => true,
            SynergyType.TROUBLESHOOTER => true,
            _ => false,
        };
    }
}
