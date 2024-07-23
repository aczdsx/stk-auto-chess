using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class PVPManager : Singleton<PVPManager>
    {
        /// <summary>
        /// pvp Init Info
        /// </summary>
        public async UniTask GetPvpInitInfo()
        {
            var response = await GrpcGame.GameGrpcManager.Instance.GetPvpInfoAsync();
            if (response.IsError)
                return;

            UnityEngine.Debug.Log("pvp info ===> " + response.CurrentSeasonId);
      
        }
    }
}