using System;
using System.Collections;
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
        protected ObfuscatorInt randomGeneratorSeed;
        public int RandomGeneratorSeed => randomGeneratorSeed;

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
        public void StartInGame<T>() where T : StateBase, new()
        {
            IsInGamePlaying = true;
            InGameMainFlowManager.Instance.StartInGameMainLoop<T>();
            // IngameResourceManager.Instance.Initialize();
        }

        public void EndInGame()
        {
            IsInGamePlaying = false;
            InGameMainFlowManager.Instance.StopInGameMainLoop();
            // IngameEffectViewPool.Instance.Clear(false);
            InGameObjectManager.Instance.Clear();
            // IngameResourceManager.Instance.Clear();
            // DOTween.defaultUpdateType = UpdateType.Normal;
            // IngameGlobalBuffPanel.popupStoreBuffs.Clear();
            // IngameGlobalBuffPanel.itemBuffs.Clear();
            // IngameGlobalBuffPanel.questBuffInfo = null;
            // ClearStageData();
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
