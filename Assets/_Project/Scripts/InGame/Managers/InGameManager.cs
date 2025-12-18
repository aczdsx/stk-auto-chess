using System;
using System.Collections;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using PrimeTween;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;
using UnityEditor.Localization.Plugins.XLIFF.V20;

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

        public void ResetRandomGeneratorSeed()
        {
            randomGeneratorSeed = InGameRandomManager.GetUniversalRandomValue();
        }

        private bool isGameInfoLoaded;
        public bool IsInGamePlaying { get; private set; }
        public bool IsInGameCombat { get; set; }
        public bool IsBlockAmbush { get; set; }

        public void Clear()
        {
            IsInGamePlaying = false;
            isGameInfoLoaded = false;
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

        private void InitializeInGameComponents(object stateData)
        {
            // 순서 중요!
            InGameVfxManager.Instance.Initialize();
            InGameHpBarViewPool.Instance.Initialize(InGameResourceHolder.HpBarView.CachedGo);
            InGameTextViewPool.Instance.InitializePool(InGameResourceHolder.InGameText.CachedGo);
            InGameObjectManager.Instance.Initialize();
            InGameCommanderManager.Instance.Initialize();
            InGameSynergyManager.Instance.Initialize();
        }

        public void EndInGame()
        {
            IsInGamePlaying = false;
            InGameMainFlowManager.Instance.StopInGameMainLoop();
            InGameCommanderManager.Instance.Clear();
            InGameObjectManager.Instance.Clear();
            InGameTextViewPool.Instance.ReleasePool();
            InGameHpBarViewPool.Instance.Clear();
            InGameVfxManager.Instance.Clear();
            InGameStatistics.Instance.Clear();
            InGameSynergyManager.Instance.Clear();
            _teamEcc.Clear();
            _teamEcc = null;
        }
        #endregion

        public void AddSynergyTeamOnce(AllianceType allianceType, long effectCodeID, ISpecSynergyData synergyData)
        {
            Span<double> stats = stackalloc double[4];
            stats[0] = synergyData.effect_stat_value_1;
            stats[1] = synergyData.effect_stat_value_2;
            stats[2] = synergyData.effect_stat_value_3;
            stats[3] = synergyData.grade;
        
            var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, stats);
            _teamEcc.AddOrMergeEffectCode(effectCodeInfo, null, allianceType);
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
            InGameMain.GetInGameMain().RefreshInGameTopUI(false);
        }
    }
}
