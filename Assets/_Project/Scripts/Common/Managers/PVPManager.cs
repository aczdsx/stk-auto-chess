using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Cookapps.Stkauto.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class PVPManager : SingletonMonoBehaviour<PVPManager>
    {
        public GetPvpInfoResponse CurrentPVPInfo { get; private set; }      // 현재 PVP INFO
        public GetPvpMatchListResponse CurrentPVPMatchListData { get; private set; }      // 현재 PVP 매치 리스트
        public GetPvpRankListResponse CurrentPVPRankListData { get; private set; } // 현재 PVP 랭킹 리스트
        public GetPvpMatchHistoryResponse CurrentPVPHistoryListData { get; private set; } // 현재 PVP 전투 히스토리 리스트
        
        private void Awake()
        {
            
        }

        private void Update()
        {
            
        }

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

            CurrentPVPMatchListData = response;
            
            // 유저 데이터 갱신
            UserDataManager.Instance.UpdatePVPMatchingListData(response, true);
        }
        
        // PVP 랭킹 리스트를 로드
        public async UniTask UpdatePVPRankList()
        {
            int showRankCount = SpecDataManager.Instance.GetGameConfig<int>("PVP_RANKING_LIST_COUNT");
            
            var response = await GrpcGame.GameGrpcManager.Instance.GetPvpRankListAsync(showRankCount);
            if (response.IsError)
                return;

            CurrentPVPRankListData = response;
            
            // todo.. 내 랭킹 데이터 업데이트
        } 
        
        // PVP 전투 히스토리 정보를 서버로 부터 최신화
        public async UniTask UpdatePVPHistoryList()
        {
            int showCount = SpecDataManager.Instance.GetGameConfig<int>("PVP_SHOW_BATTLE_LOG_COUNT");
            
            var response = await GrpcGame.GameGrpcManager.Instance.GetPvpMatchHistory(showCount);
            if (response.IsError)
                return;

            CurrentPVPHistoryListData = response;
        }
        
        // 자신의 PVP 프로필 정보를 서버에 업데이트 (배틀덱 저장 - 자동, 수동)
        public async UniTask UpdatePVPProfileData(int battlePower)
        {
            // 심플 정보 세팅
            var userPVPSimpleData = UserDataManager.Instance.GetCurrentPVPSimpleProfileData();
            var serializedSimpleData = BMUtil.ConvertToJsonSerialize(userPVPSimpleData);
            
            // 디테일 정보 세팅
            var userPVPDetailData = UserDataManager.Instance.GetCurrentPVPDetailProfileData();
            var serializedDetailData = BMUtil.ConvertToJsonSerialize(userPVPDetailData);
            
            var response = await GrpcGame.GameGrpcManager.Instance.UpdatePvpProfile(battlePower, serializedSimpleData, serializedDetailData);
            if (response.IsError)
                return;
        }
    }
}