using System;
using System.Collections;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using PrimeTween;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace CookApps.BattleSystem
{
    // 웨이브 관리, 유저의 캐릭터 정보, 업그레이드 정보, 보유하고있는 캐릭터들
    public class InGameManager : SingletonMonoBehaviour<InGameManager>
    {
        #region GameInfo
        public SpecStage SpecStage { get; private set; }
        public SpecDungeonTrial SpecDungeonTrial { get; private set; }
        public UserPVPBattleDeckList UserPvpBattleDeckList { get; private set; }
        protected ObfuscatorInt randomGeneratorSeed;
        public int RandomGeneratorSeed => randomGeneratorSeed;

        private EffectCodeContainer ecc;
        public EffectCodeContainer EffectCodeContainer => ecc;

        public void ResetRandomGeneratorSeed()
        {
            randomGeneratorSeed = InGameRandomManager.GetUniversalRandomValue();
        }

        private bool isGameInfoLoaded;
        public bool IsInGamePlaying { get; private set; }

        public void Clear()
        {
            IsInGamePlaying = false;
            isGameInfoLoaded = false;
        }

        // TODO: Add Game Info
        #endregion

        #region InGame Cycle
        public void StartInGame<T>(SpecStage specStage) where T : StateBase, new()
        {
            SpecStage = specStage;
            IsInGamePlaying = true;
            ecc = new EffectCodeContainer(this);
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(specStage);
            InitializeInGameComponents(specStage);
        }

        public void StartInGame<T>(SpecDungeonTrial specDungeonTrial) where T : StateBase, new()
        {
            SpecDungeonTrial = specDungeonTrial;
            IsInGamePlaying = true;
            ecc = new EffectCodeContainer(this);
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(specDungeonTrial);
            InitializeInGameComponents(specDungeonTrial);
        }
        
        public void StartInGame<T>(UserPVPBattleDeckList pvpBattleDeck) where T : StateBase, new()
        {
            UserPvpBattleDeckList = pvpBattleDeck;
            IsInGamePlaying = true;
            ecc = new EffectCodeContainer(this);
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
            ecc.Clear();
            ecc = null;
        }
        #endregion

        public void AddSynergyEffectCode(AllianceType type)
        {
            ElementType elementType = ElementType.DARK;
            Span<double> stats = stackalloc double[2];

            int synergyCount = InGameObjectManager.Instance.GetCharacterSynergyCount(type, elementType);
            if (synergyCount > 0)
            {
                var list = SpecDataManager.Instance.GetSpecSynergyList(elementType);
                var data = list.Find(l => l.min_count <= synergyCount && l.max_count >= synergyCount);
                if (data.grade > 0)
                {
                    stats[0] = data.stat_value;
                    stats[1] = (double) type;

                    var effectCodeInfo = new EffectCodeInfo(list[0].id, 0, stats);
                    ecc.AddOrMergeEffectCode(effectCodeInfo, null);
                }
            }
        }
        public void RegenerateGlobalRandomSeeds()
        {
            InGameRandomManager.Instance.ResetRandomSeedGenerator(randomGeneratorSeed);
            Random random = InGameRandomManager.Instance.GetRandom();
            var globalRandomSeeds = new int[(int) GlobalRandomType.MAX];
            for (var i = 0; i < globalRandomSeeds.Length; i++)
            {
                globalRandomSeeds[i] = random.Next();
            }

            InGameRandomManager.Instance.ResetGlobalRandomSeed(globalRandomSeeds);
        }

        public void UpdateSynergyAndAttr()
        {
            InGameObjectManager.Instance.ClearSynergyFx();
            InGameMain.GetInGameMain().RefreshInGameTopUI(false);
        }
    }
}
