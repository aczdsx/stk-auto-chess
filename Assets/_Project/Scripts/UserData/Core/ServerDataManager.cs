using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 서버 데이터 관리 매니저
    /// 모든 데이터 모델을 직접 속성으로 노출하여 타입 안전성 보장
    /// </summary>
    public class ServerDataManager : Singleton<ServerDataManager>
    {
        // 데이터 모델들 (직접 접근)
        public CharacterModel Character { get; private set; } = new ();
        public WalletModel Wallet { get; private set; } = new ();
        public ElpisModel Elpis { get; private set; } = new ();
        public BattleModel Battle { get; private set; } = new ();

        /// <summary>
        /// 모든 데이터 초기화
        /// </summary>
        public void ClearAll()
        {
            Character.Reset();
            Wallet.Reset();
            Elpis.Reset();
            Battle.Reset();
        }

        /// <summary>
        /// 데이터 유효성 검증 (디버그용)
        /// </summary>
        public bool ValidateAll()
        {
            bool allValid = true;

            if (!Character.Validate())
            {
                Debug.LogError("[ServerDataManager] Character validation failed");
                allValid = false;
            }

            if (!Wallet.Validate())
            {
                Debug.LogError("[ServerDataManager] Wallet validation failed");
                allValid = false;
            }

            if (!Elpis.Validate())
            {
                Debug.LogError("[ServerDataManager] Elpis validation failed");
                allValid = false;
            }

            if (!Battle.Validate())
            {
                Debug.LogError("[ServerDataManager] Battle validation failed");
                allValid = false;
            }

            return allValid;
        }
    }
}