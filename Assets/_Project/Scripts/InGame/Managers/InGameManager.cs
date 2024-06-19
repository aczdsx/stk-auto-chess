using System;
using System.Collections;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
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
        public void StartInGame<T>(SpecStage specStage, object stateData) where T : StateBase, new()
        {
            SpecStage = specStage;
            IsInGamePlaying = true;
            ecc = new EffectCodeContainer(this);
            // 순서 중요!
            InGameVfxManager.Instance.Initialize();
            InGameHpBarViewPool.Instance.Initialize(InGameResourceHolder.HpBarView.CachedGo);
            InGameTextViewPool.Instance.InitializePool(InGameResourceHolder.InGameText.CachedGo);
            InGameObjectManager.Instance.Initialize();
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(stateData);
        }

        public void StartInGame<T>(object stateData) where T : StateBase, new()
        {
            IsInGamePlaying = true;
            ecc = new EffectCodeContainer(this);
            InGameVfxManager.Instance.Initialize();
            InGameHpBarViewPool.Instance.Initialize(InGameResourceHolder.HpBarView.CachedGo);
            InGameTextViewPool.Instance.InitializePool(InGameResourceHolder.InGameText.CachedGo);
            InGameObjectManager.Instance.Initialize();
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>(stateData);
        }

        public void EndInGame()
        {
            IsInGamePlaying = false;
            InGameMainFlowManager.Instance.StopInGameMainLoop();
            InGameObjectManager.Instance.Clear();
            InGameTextViewPool.Instance.ReleasePool();
            InGameHpBarViewPool.Instance.Clear();
            InGameVfxManager.Instance.Clear();
            ecc.Clear();
            ecc = null;
        }
        #endregion

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
    }
}
