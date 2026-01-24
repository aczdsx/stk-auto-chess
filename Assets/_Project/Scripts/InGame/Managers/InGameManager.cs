using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using Random = System.Random;

namespace CookApps.BattleSystem
{
    // 웨이브 관리, 유저의 캐릭터 정보, 업그레이드 정보, 보유하고있는 캐릭터들
    public class InGameManager : SingletonMonoBehaviour<InGameManager>
    {
        #region GameInfo
        public StageInfo SpecStage { get; private set; }
        public DungeonBabelInfo SpecDungeonTrial { get; private set; }
        public UserPVPBattleDetailData UserPvpBattleDeckList { get; private set; }
        protected ObfuscatorInt randomGeneratorSeed;
        public int RandomGeneratorSeed => randomGeneratorSeed;

        private EffectCodeContainerTeam _teamEcc;
        public EffectCodeContainerTeam TeamEcc => _teamEcc;


        public string AppEventResult = string.Empty;
        public string AppEventReason = string.Empty;
        public string BattleSessionId { get; private set; }

        public InGameTestConfig TestConfig { get; set; }

        public void SetSessionIdAndRandomSeed(string sessionId, int seed)
        {
            BattleSessionId = sessionId;
            randomGeneratorSeed = seed;
            RegenerateGlobalRandomSeeds();
        }

        public void SetFixedRandomSeed(int seed)
        {
            randomGeneratorSeed = seed;
        }

        /// <summary>
        /// 테스트 모드에서 SpecStage를 설정 (다른 시스템에서 참조할 수 있도록)
        /// </summary>
        public void SetSpecStageForTest(StageInfo specStage)
        {
            SpecStage = specStage;
        }

        private bool isGameInfoLoaded;
        public bool IsInGamePlaying { get; private set; }
        public bool IsInGameCombat { get; set; }
        public bool IsBlockAmbush { get; set; }

        // 테스트용 무적 플래그
        public bool IsPlayerInvincible { get; set; }
        public bool IsEnemyInvincible { get; set; }

        public void Clear()
        {
            IsInGamePlaying = false;
            isGameInfoLoaded = false;
            BattleSessionId = string.Empty;
        }

        // TODO: Add Game Info
        #endregion

        #region InGame Cycle
        public void StartInGame<T>(StageInfo specStage) where T : StateBase, new()
        {
            SpecStage = specStage;
            IsInGamePlaying = true;
            IsInGameCombat = true;
            IsBlockAmbush = false;
            AppEventResult = string.Empty;
            AppEventReason = string.Empty;
            _teamEcc = new EffectCodeContainerTeam(this);
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(specStage);
            InitializeInGameComponents(specStage);
        }

        public void StartInGame<T>(DungeonBabelInfo specDungeonTrial) where T : StateBase, new()
        {
            SpecDungeonTrial = specDungeonTrial;
            IsInGamePlaying = true;
            IsInGameCombat = true;
            IsBlockAmbush = specDungeonTrial.dungeon_map_id == 1 ? true : false;
            AppEventResult = string.Empty;
            AppEventReason = string.Empty;
            _teamEcc = new EffectCodeContainerTeam(this);
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(specDungeonTrial);
            InitializeInGameComponents(specDungeonTrial);
        }

        public void StartInGame<T>(UserPVPBattleDetailData pvpBattleDeck) where T : StateBase, new()
        {
            UserPvpBattleDeckList = pvpBattleDeck;
            IsInGamePlaying = true;
            IsInGameCombat = true;
            IsBlockAmbush = false;
            AppEventResult = string.Empty;
            AppEventReason = string.Empty;
            _teamEcc = new EffectCodeContainerTeam(this);
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(pvpBattleDeck);
            InitializeInGameComponents(pvpBattleDeck);
        }

        // 테스트용 StartInGame
        public void StartInGame<T>(InGameTestConfig testConfig) where T : StateBase, new()
        {
            IsInGamePlaying = true;
            IsInGameCombat = true;
            IsBlockAmbush = false;
            AppEventResult = string.Empty;
            AppEventReason = string.Empty;

            // 테스트 무적 플래그 설정
            TestConfig = testConfig;
            IsPlayerInvincible = TestConfig?.PlayerInvincible ?? false;
            IsEnemyInvincible = TestConfig?.EnemyInvincible ?? false;

            _teamEcc = new EffectCodeContainerTeam(this);
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(TestConfig);
            InitializeInGameComponents(TestConfig);
        }

        private void InitializeInGameComponents(object stateData)
        {
            // 순서 중요!
            InGameVfxManager.Instance.Initialize();
            InGameHpBarViewPool.Instance.Initialize(InGameResourceHolder.HpBarView.CachedGo);
            InGameTextViewPool.Instance.InitializePool(InGameResourceHolder.InGameText.CachedGo);
            InGameBuffDebuffPool.Instance.Initialize(InGameResourceHolder.InGameBuffDebuff.CachedGo);
            InGameObjectManager.Instance.Initialize();
            InGameCommanderManager.Instance.Initialize();
            InGameSynergyManager.Instance.Initialize();

            // 시너지 UI 이전 상태 초기화 (인게임 시작 시)
            InGameSynergyUI.ClearPreviousSynergyStates();
        }

        public void EndInGame()
        {
            BattleSessionId = string.Empty;
            randomGeneratorSeed = 0;
            IsInGamePlaying = false;
            InGameMainFlowManager.Instance.StopInGameMainLoop();
            InGameCommanderManager.Instance.Clear();
            InGameObjectManager.Instance.Clear();
            InGameTextViewPool.Instance.ReleasePool();
            InGameHpBarViewPool.Instance.Clear();
            InGameBuffDebuffPool.Instance.Clear();
            InGameVfxManager.Instance.Clear();
            InGameStatistics.Instance.Clear();
            InGameSynergyManager.Instance.Clear();
            _teamEcc.Clear();
            _teamEcc = null;
        }
        #endregion

        public void AddSynergyTeamOnce(AllianceType allianceType, long effectCodeID, ISpecSynergyData synergyData, int grade)
        {
            Span<double> stats = stackalloc double[4];
            stats[0] = synergyData.effect_stat_value_1;
            stats[1] = synergyData.effect_stat_value_2;
            stats[2] = synergyData.effect_stat_value_3;
            stats[3] = grade;
        
            var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, stats);
            _teamEcc.AddOrMergeEffectCode(effectCodeInfo, null, allianceType);
        }
        public void OnFlowStateStageReadyStart()
        {
            var codes = _teamEcc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnFlowStateStageReadyStart);
            EffectCodeForLoopHelper.Call(codes, EffectCodeCharacterLambda.CallOnFlowStateStageReadyStartLambda);
        }

        public void RemoveSynergyTeamOnce(AllianceType allianceType, SynergyType synergyType)
        {
            var synergyList = SpecDataManager.Instance.GetSpecSynergyList(synergyType);
            if (synergyList == null || synergyList.Count == 0)
                return;

            _teamEcc.RemoveEffectCode(synergyList[0].synergy_group_id, allianceType);
        }

        public void RegenerateGlobalRandomSeeds()
        {
            InGameRandomManager.Instance.ResetRandomSeedGenerator(randomGeneratorSeed);
            Random random = InGameRandomManager.Instance.GetRandom();
            var globalRandomSeeds = new int[(int)GlobalRandomType.MAX];
            for (var i = 0; i < globalRandomSeeds.Length; i++)
            {
                globalRandomSeeds[i] = random.Next();
            }

            InGameRandomManager.Instance.ResetGlobalRandomSeed(globalRandomSeeds);
        }

        public void UpdateSynergyAndAttr()
        {
            // InGameSynergyManager.Instance.ClearSynergyFx();

            // 상승한 시너지 타입 수집 시작
            InGameSynergyUI.BeginCollectUpgradedSynergies();

            InGameMain.GetInGameMain().RefreshInGameTopUI(false);

            // 상승한 시너지 타입 수집 종료 및 캐릭터 HpBarView 효과 재생
            var upgradedSynergies = InGameSynergyUI.EndCollectUpgradedSynergies();
            if (upgradedSynergies.Count > 0)
            {
                PlayCharacterSynergyEffects(upgradedSynergies);
            }
        }

        /// <summary>
        /// 상승한 시너지 타입에 해당하는 캐릭터들의 HpBarView 효과 재생
        /// </summary>
        private void PlayCharacterSynergyEffects(System.Collections.Generic.HashSet<SynergyType> upgradedSynergies)
        {
            var characters = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
            foreach (var character in characters)
            {
                if (character?.SpecCharacter == null) continue;

                var hpBarView = character.GetHpBarView();
                if (hpBarView == null) continue;

                // 속성 시너지 체크
                if (upgradedSynergies.Contains(character.SpecCharacter.character_element_type))
                {
                    hpBarView.PlayElementSynergyEffect();
                }

                // 성좌 시너지 체크
                if (upgradedSynergies.Contains(character.SpecCharacter.character_stella_type))
                {
                    hpBarView.PlayPositionSynergyEffect();
                }
            }
        }
    }
}
