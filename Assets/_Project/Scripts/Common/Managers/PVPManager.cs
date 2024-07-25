using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Cookapps.Stkauto.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class PVPManager : Singleton<PVPManager>
    {
        public GetPvpInfoResponse CurrentPVPInfo { get; private set; }      // 현재 PVP INFO
        
        // PVP 정보를 서버로 부터 최신화
        public async UniTask UpdatePVPInfo()
        {
            var response = await GrpcGame.GameGrpcManager.Instance.GetPvpInfoAsync();
            if (response.IsError)
                return;

            CurrentPVPInfo = response;
        }
        
        // PVP 정보를 서버로 부터 최신화
        public async UniTask UpdatePVPMatchList()
        {
            var response = await GrpcGame.GameGrpcManager.Instance.GetPvpMatchListAsync();
            if (response.IsError)
                return;

            
        }
    }
}