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
            //simpleData.BattlePoint =

            if (isDefenseDeck)
            {
                var getPVPDefenseDeckList = GetPVPDefenseCharacterDeckDataList();
                if (getPVPDefenseDeckList != null)
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
                if (getPVPAttackDeckList != null)
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
            //detailData.BattlePoint =

            detailData.PvpDeckList = new UserPVPBattleDeckList();
            
            // 유저 캐릭터 덱 데이터 세팅
            if (isDefenseDeck)
            {
                var getPVPDefenseDeckList = GetPVPDefenseCharacterDeckDataList();
                if (getPVPDefenseDeckList != null)
                {
                    foreach (var deckData in getPVPDefenseDeckList)
                    {
                        detailData.PvpDeckList.PvpCharacterDecks.Add(deckData);
                    }
                }
            }
            else
            {
                var getPVPAttackDeckList = GetPVPAttackDeckDataList();
                if (getPVPAttackDeckList != null)
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
            
            // 유저 장애물 덱 데이터 세팅
            //...
            
            return detailData;
        }

        public void SetPVPDefenseDeck(List<CookApps.BattleSystem.CharacterController> characterList /*장애물 데이터 파라미터*/)
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
            
            // todo.. 장애물 데이터 세팅
            
            SaveUserPVPData();
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
        
        // 매칭 리스트에 데이터 추가
        public void AddPVPMatchingList(UserPVPBattleSimpleData pvpData, bool needSave)
        {
            if (pvpData == null) return;
            
            UserPVP.CurrentPvpMatchingList.Add(pvpData);
            
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
            
            UserPVP.CurrentPvpMatchingList.Clear();
            foreach (var matchData in matchDataList)
            {
                UserPVPBattleSimpleData newPVPSimpleData = new UserPVPBattleSimpleData();

                var decompressStringData  = BMUtil.DecompressGzipToString(matchData.SimpleInfo);
                newPVPSimpleData = JsonConvert.DeserializeObject<UserPVPBattleSimpleData>(decompressStringData);
                
                UserPVP.CurrentPvpMatchingList.Add(newPVPSimpleData);
            }
            
            if (needSave)
            {
                SaveUserPVPData();
            }
        }
        
        // 매칭 리스트에서 데이터 반환 (단일)
        public UserPVPBattleSimpleData GetPVPMatchingData(string playerID)
        {
            if (string.IsNullOrWhiteSpace(playerID)) return null;
            
            return UserPVP.CurrentPvpMatchingList.ToList().Find(x => x.PlayerId == playerID);
        }
        
        // 매칭 리스트에서 데이터 반환 (전체)
        public List<UserPVPBattleSimpleData> GetPVPMatchingDataList()
        {
            return UserPVP.CurrentPvpMatchingList.ToList();
        }
    }
}
