using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using UnityEngine;

namespace CookApps.AutoBattler
{
    // PVP 시간 갱신 타입
    public enum PVPTimeRefreshType
    {
        MATCHING_LIST,              // 매칭 리스트 갱신
        MATCHING_REFRESH_COUNT,     // 매칭 리스트 갱신 카운트
        RANKING_LIST,               // 랭킹 리스트 갱신
        AUTO_PROFILE,               // 자동 프로필 갱신
    }
    
    public partial class UserDataManager
    {
        private UserPVP userPVP;

        public UserPVP UserPVP => userPVP;

        [Initialize(DataCategory.UserPvp, 12)]
        private void Initialize_PVPData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userPVP = new UserPVP();
                userPVP.RecentSeasonId = 1;
                userPVP.Ranking = 0;

                UpdateUserRankData(0, false);   // 랭크 데이터 업데이트
                
                return;
            }

            userPVP = MessageUtility.FromBase64String<UserPVP>(data);
        }

        [Clear]
        private void Clear_PVPData()
        {
            userPVP = null;
        }

        public void SaveUserPVPData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserPvp.ToCategoryString(), userPVP);
        }

        // PVP 전투 결과 데이터를 유저데이터에 반영
        public void SetPVPBattleResultData(MatchPvpResponse data, bool needSave)
        {
            UserPVP.Ranking = data.MyCurrentRank;
            UserPVP.RankPoint = data.MyCurrentScore;

            if (needSave)
            {
                SaveUserPVPData();
            }
        }

        // 유저 공격덱 데이터 반환
        public List<UserCharacterBattleDeck> GetPVPAttackDeckDataList()
        {
            return GetUserCharacterBattleDeckList(InGameType.PVP);
        }
        
        // 유저 방어덱 데이터 반환 (캐릭터)
        public List<UserPVPCharacterBattleDeck> GetPVPDefenseCharacterDeckDataList()
        {
            return UserPVP?.MyPvpDefenseDeckList?.PvpCharacterDecks.ToList();
        }
        
        // 유저 방어덱 데이터 반환 (장애물)
        public List<UserPVPObstacleBattleDeck> GetPVPDefenseObstacleDeckDataList()
        {
            return UserPVP?.MyPvpDefenseDeckList?.PvpObstacleDecks.ToList();
        }

        // 현재 정보를 기준으로 유저 데이터를 서버에 보낼 수 있는 SimpleData로 변환
        public UserPVPBattleSimpleData GetCurrentPVPSimpleProfileData(bool isDefenseDeck)
        {
            UserPVPBattleSimpleData simpleData = new UserPVPBattleSimpleData();
            simpleData.PlayerId = UserBasicData.PlayerId;
            simpleData.ServerId = UserBasicData.ServerId;
            simpleData.RankId = UserPVP.RankId;
            simpleData.RankPoint = UserPVP.RankPoint;
            simpleData.Ranking = UserPVP.Ranking;
            simpleData.Nickname = UserBasicData.Nickname;
            simpleData.PlayerLv = UserBasicData.Level;
            simpleData.BattlePoint = GetPVPDeckBattlePower(isDefenseDeck);

            if (isDefenseDeck)
            {
                var getPVPDefenseDeckList = GetPVPDefenseCharacterDeckDataList();
                if (getPVPDefenseDeckList != null && getPVPDefenseDeckList.Count > 0)
                {
                    foreach (var deckData in getPVPDefenseDeckList)
                    {
                        var characterData = GetUserCharacter(deckData.Id);
                        if (characterData == null) continue;
                
                        UserPVPCharacterSimpleDeck newSimpleData = new UserPVPCharacterSimpleDeck();
                        newSimpleData.Id = characterData.CharacterId;
                        newSimpleData.Lv = characterData.Level;
                
                        simpleData.SimpleDeckList.Add(newSimpleData);
                    }
                }
            }
            else
            {
                var getPVPAttackDeckList = GetPVPAttackDeckDataList();
                if (getPVPAttackDeckList != null && getPVPAttackDeckList.Count > 0)
                {
                    foreach (var deckData in getPVPAttackDeckList)
                    {
                        var characterData = GetUserCharacter(deckData.CharacterId);
                        if (characterData == null) continue;
                
                        UserPVPCharacterSimpleDeck newSimpleData = new UserPVPCharacterSimpleDeck();
                        newSimpleData.Id = characterData.CharacterId;
                        newSimpleData.Lv = characterData.Level;
                
                        simpleData.SimpleDeckList.Add(newSimpleData);
                    }
                }
            }
            
            return simpleData;
        }
        
        // 현재 정보를 기준으로 유저 데이터를 서버에 보낼 수 있는 DetailData로 변환
        public UserPVPBattleDetailData GetCurrentPVPDetailProfileData(bool isDefenseDeck)
        {
            UserPVPBattleDetailData detailData = new UserPVPBattleDetailData();
            detailData.PlayerId = UserBasicData.PlayerId;
            detailData.ServerId = UserBasicData.ServerId;
            detailData.RankId = UserPVP.RankId;
            detailData.RankPoint = UserPVP.RankPoint;
            detailData.Ranking = UserPVP.Ranking;
            detailData.Nickname = UserBasicData.Nickname;
            detailData.PlayerLv = UserBasicData.Level;
            detailData.BattlePoint = GetPVPDeckBattlePower(isDefenseDeck);

            detailData.PvpDeckList = new UserPVPBattleDeckList();
            
            // 유저 캐릭터 덱 데이터 세팅
            if (isDefenseDeck)
            {
                var getPVPDefenseDeckList = GetPVPDefenseCharacterDeckDataList();
                if (getPVPDefenseDeckList != null && getPVPDefenseDeckList.Count > 0)
                {
                    foreach (var deckData in getPVPDefenseDeckList)
                    {
                        detailData.PvpDeckList.PvpCharacterDecks.Add(deckData);
                    }
                }
                
                
                var getPVPDefenseDeckObstacleList = GetPVPDefenseObstacleDeckDataList();
                if (getPVPDefenseDeckObstacleList != null && getPVPDefenseDeckObstacleList.Count > 0)
                {
                    foreach (var deckData in getPVPDefenseDeckObstacleList)
                    {
                        detailData.PvpDeckList.PvpObstacleDecks.Add(deckData);
                    }
                }
            }
            else
            {
                var getPVPAttackDeckList = GetPVPAttackDeckDataList();
                if (getPVPAttackDeckList != null && getPVPAttackDeckList.Count > 0)
                {
                    foreach (var deckData in getPVPAttackDeckList)
                    {
                        var characterData = GetUserCharacter(deckData.CharacterId);
                        if (characterData == null) continue;
                
                        UserPVPCharacterBattleDeck characterDeckData = new UserPVPCharacterBattleDeck();
                        characterDeckData.Id = characterData.CharacterId;
                        characterDeckData.Lv = characterData.Level;
                        characterDeckData.PosX = deckData.PositionTileX;
                        characterDeckData.PosY = deckData.PositionTileY;
                
                        detailData.PvpDeckList.PvpCharacterDecks.Add(characterDeckData);
                    }
                }
            }
            
            return detailData;
        }

        public void SetPVPDefenseDeck(List<CookApps.BattleSystem.CharacterController> characterList, IEnumerable<UserPVPObstacleBattleDeck> obstacleDeck)
        {
            if (characterList == null || characterList.Count <= 0) return;

            if (UserPVP.MyPvpDefenseDeckList == null)
            {
                UserPVP.MyPvpDefenseDeckList = new UserPVPBattleDeckList();
            }
            
            // 초기화
            UserPVP.MyPvpDefenseDeckList.PvpCharacterDecks.Clear();
            UserPVP.MyPvpDefenseDeckList.PvpObstacleDecks.Clear();
            UserPVP.MyPvpDefenseDeckList.EffectCodeInfos.Clear();
            
            // 캐릭터 데이터 세팅
            foreach (var character in characterList)
            {
                UserPVPCharacterBattleDeck newUserBattleDeck = new UserPVPCharacterBattleDeck();

                newUserBattleDeck.Id = character.CharacterId;
                newUserBattleDeck.Lv = character.GetCharacterStat().Level;
                newUserBattleDeck.PosX = character.CurrentTile.X;
                newUserBattleDeck.PosY = character.CurrentTile.Y;

                UserPVP.MyPvpDefenseDeckList.PvpCharacterDecks.Add(newUserBattleDeck);
            }
            
            // 캐릭터 데이터 세팅
            UserPVP.MyPvpDefenseDeckList.PvpObstacleDecks.AddRange(obstacleDeck);
            
            SaveUserPVPData();
        }
        
        // PVP와 관련있는 갱신 시간 데이터를 업데이트
        public void UpdateNextRefreshTimeStamp(PVPTimeRefreshType timeType, bool needSave)
        {
            switch (timeType)
            {
                case PVPTimeRefreshType.MATCHING_LIST:
                    // var nextMatchingRefreshTime = SpecDataManager.Instance.GetGameConfig<int>("PVP_RANKING_LIST_REFRESH_TIME");
                    // UserPVP.RefreshRankingTimestamp = TimeManager.Instance.AddSecondsTimeStamp(nextMatchingRefreshTime);
                    break;
                case PVPTimeRefreshType.MATCHING_REFRESH_COUNT:
                    UserPVP.RefreshMatchingCntTimestamp = TimeManager.Instance.TommorrowTimeStamp();
                    break;
                case PVPTimeRefreshType.RANKING_LIST:
                    var nextRankingRefreshTime = SpecDataManager.Instance.GetGameConfig<int>("PVP_RANKING_LIST_REFRESH_TIME");
                    UserPVP.RefreshRankingTimestamp = TimeManager.Instance.AddSecondsTimeStamp(nextRankingRefreshTime);
                    break;
                case PVPTimeRefreshType.AUTO_PROFILE:
                    var autoRefreshTime = SpecDataManager.Instance.GetGameConfig<int>("PVP_PROFILE_AUTO_REFRESH_TIME");
                    UserPVP.AutoRefreshProfileTimestamp = TimeManager.Instance.AddSecondsTimeStamp(autoRefreshTime);
                    break;
            }

            if (needSave)
            {
                SaveUserQuestData();
            }
        }
        
        public void UpdateUserRankData(int rankPoint, bool needSave)
        {
            var specTierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, rankPoint);
            if (specTierData == null) return;

            UserPVP.RankPoint = rankPoint;
            UserPVP.RankId = specTierData.ranking_id;
            
            if (needSave)
            {
                SaveUserPVPData();
            }
        }
        
        // 유저 PVP 매칭 데이터를 타겟 매치 데이터를 기준으로 새로 업데이트
        public void UpdatePVPMatchingListData(GetPvpMatchListResponse targetMatchData, bool needSave)
        {
            if (targetMatchData == null) return;
            
            var matchDataList = targetMatchData.MatchInfo.ToList();
            
            UserPVP.CurrentPvpMatchingDic.Clear();
            foreach (var matchData in matchDataList)
            {
                if (UserPVP.CurrentPvpMatchingDic.ContainsKey(matchData.PlayerId)) continue;
                
                UserPVPBattleSimpleData newPVPSimpleData = new UserPVPBattleSimpleData();

                newPVPSimpleData = BMUtil.DecompressGzipToDataClass<UserPVPBattleSimpleData>(matchData.SimpleInfo);
                
                UserPVP.CurrentPvpMatchingDic.Add(matchData.PlayerId, newPVPSimpleData);
            }
            
            if (needSave)
            {
                SaveUserPVPData();
            }
        }
        
        // PVP 매칭 리스트 결과데이터 업데이트
        public void SetPVPMatchingResultData(string targetPlayerID, PvpMatchResult resultType, bool needSave)
        {
            if (UserPVP.CurrentPvpMatchingDic == null || UserPVP.CurrentPvpMatchingDic.Count <= 0) return;
            if (UserPVP.CurrentPvpMatchingDic.ContainsKey(targetPlayerID) == false) return;
            
            UserPVP.CurrentPvpMatchingDic[targetPlayerID].MatchResult = (int)resultType;

            if (needSave)
            {
                SaveUserPVPData();
            }
        }
        
        // 매칭 리스트에서 데이터 반환 (단일)
        public UserPVPBattleSimpleData GetPVPMatchingData(string playerID)
        {
            if (string.IsNullOrWhiteSpace(playerID)) return null;
            if (UserPVP.CurrentPvpMatchingDic.ContainsKey(playerID) == false) return null;

            return UserPVP.CurrentPvpMatchingDic[playerID];
        }
        
        // 매칭 리스트에서 데이터 반환 (전체)
        public List<UserPVPBattleSimpleData> GetPVPMatchingDataList()
        {
            return UserPVP.CurrentPvpMatchingDic.Values.ToList();
        }
    }
}
