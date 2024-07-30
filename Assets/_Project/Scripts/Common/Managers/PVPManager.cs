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
        
        // 자신의 PVP 방어덱 프로필 정보를 서버에 업데이트 (배틀덱 저장 - 자동 위주)
        public async UniTask UpdatePVPProfileData(int battlePower)
        {
            // 저장할 덱 데이터 유효성 체크
            // var pvpDefenseDeckList = UserDataManager.Instance.GetPVPDefenseCharacterDeckDataList();
            // if (pvpDefenseDeckList == null || pvpDefenseDeckList.Count <= 0)
            // {
            //     return;
            // }
            
            // 심플 정보 세팅
            var userPVPSimpleData = UserDataManager.Instance.GetCurrentPVPSimpleProfileData(true);
            var serializedSimpleData = BMUtil.ConvertToJsonSerialize(userPVPSimpleData);
            
            // 디테일 정보 세팅
            var userPVPDetailData = UserDataManager.Instance.GetCurrentPVPDetailProfileData(true);
            var serializedDetailData = BMUtil.ConvertToJsonSerialize(userPVPDetailData);
            
            var response = await GrpcGame.GameGrpcManager.Instance.UpdatePvpProfile(battlePower, serializedSimpleData, serializedDetailData);
            if (response.IsError)
                return;
            
            Debug.Log("UpdatePVPProfileData --- Success");
        }

        // 자신의 PVP 방어덱 프로필 정보를 서버 및 로컬 데이터에 업데이트
        public async UniTask SavePVPProfileData(int battlePower, List<CookApps.BattleSystem.CharacterController> characterList /*장애물 데이터 파라미터*/)
        {
            UserDataManager.Instance.SetPVPDefenseDeck(characterList);
            
            await UpdatePVPProfileData(battlePower);
        }
        
        // PVP 전투 결과를 전송
        public async UniTask SendMatchPVPResult(PvpMatchResult result, string opponentPlayerID, string opponentSimpleData)
        {
            var response = await GrpcGame.GameGrpcManager.Instance.MatchPvp(result, opponentPlayerID, opponentSimpleData);
            if (response.IsError)
                return;

            // 유저 데이터에 결과 저장
            UserDataManager.Instance.SetPVPBattleResultData(response, true);
        }
        
        // 상대방 PVP 매칭 덱이 업데이트 되었는지 체크
        public async UniTask<CheckPvpPowerUpdatedResponse> CheckPVPPowerUpdated(string opponentPlayerID, int opponentBattlePower)
        {
            var response = await GrpcGame.GameGrpcManager.Instance.CheckPvpPowerUpdated(opponentPlayerID, opponentBattlePower);
            if (response.IsError)
                return null;

            return response;
        }
    }
}