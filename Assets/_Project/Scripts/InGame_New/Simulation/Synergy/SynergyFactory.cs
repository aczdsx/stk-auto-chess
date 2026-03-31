using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 준비 페이즈 시너지 행동 추상 베이스.
    /// Asterism 시너지(Supernova/Troubleshooter 등)의 준비 페이즈 상호작용을 정의.
    /// 전투 시작 시 prep 데이터 → 시너지 전투 행동 시스템로 전달.
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

        /// <summary>전투 행동(시너지 전투 행동 시스템)에 전달할 데이터</summary>
        public int PrepTargetEntityId = -1;
        public int PrepParam0;
        public int PrepParam1;

        /// <summary>재접속 복원용 상태 스냅샷</summary>
        public virtual PrepBehaviorSnapshot CaptureSnapshot()
        {
            return new PrepBehaviorSnapshot
            {
                TraitId = TraitId,
                Tier = Tier,
                PlayerIndex = PlayerIndex,
                PrepTargetEntityId = PrepTargetEntityId,
                PrepParam0 = PrepParam0,
                PrepParam1 = PrepParam1,
            };
        }

        /// <summary>스냅샷에서 상태 복원 (OnActivate 대신 호출)</summary>
        public virtual void RestoreFromSnapshot(PrepBehaviorSnapshot snapshot)
        {
            PrepTargetEntityId = snapshot.PrepTargetEntityId;
            PrepParam0 = snapshot.PrepParam0;
            PrepParam1 = snapshot.PrepParam1;
        }
    }

    /// <summary>PrepBehavior 재접속 복원용 스냅샷</summary>
    public struct PrepBehaviorSnapshot
    {
        public int TraitId;
        public byte Tier;
        public byte PlayerIndex;
        public int PrepTargetEntityId;
        public int PrepParam0;
        public int PrepParam1;

        // 슈퍼노바 전용 (다른 Prep 타입도 확장 가능)
        public sbyte ObjectCol;
        public sbyte ObjectRow;
    }

    /// <summary>
    /// 성군(Asterism) 시너지 통합 팩토리.
    /// 준비 페이즈 행동(SynergyPrepBehaviorBase) + 전투 행동(시너지 전투 행동 시스템) 모두 여기서 생성.
    /// 새 Asterism 시너지 추가 시 이 파일만 수정.
    /// </summary>
    public static class SynergyFactory
    {
        /// <summary>해당 SynergyType이 행동 클래스를 필요로 하는지 (속성=false, 성군=true)</summary>
        public static bool NeedsBehavior(SynergyType type) => type switch
        {
            SynergyType.NORMAL or
            SynergyType.FIRE or
            SynergyType.WIND or
            SynergyType.LIGHTNING or
            SynergyType.EARTH or
            SynergyType.WATER => false,
            _ => true,
        };

        /// <summary>준비 페이즈 행동 생성. 구현체 추가 시 스위치에 케이스 추가.</summary>
        public static SynergyPrepBehaviorBase CreatePrep(SynergyType type, byte tier, int traitId, byte playerIndex)
        {
            SynergyPrepBehaviorBase behavior = type switch
            {
                SynergyType.SUPERNOVA => new SynergyPrepSupernova(),
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

    }
}
