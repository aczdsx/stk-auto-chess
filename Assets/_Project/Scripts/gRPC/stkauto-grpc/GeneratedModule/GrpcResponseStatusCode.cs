// GrpcGenerator에서 만들어진 파일입니다. 수정하지 마세요.
// Copyright (c) CookApps.
// ReSharper disable All
namespace CookApps.AutoBattler
{
    /// 모든 Response Status Code를 정의합니다.
    public static class GrpcResponseStatusCode
    {
        // AuthResponseStatus ----------------------------------------------------------
        /// 잘못된 인증 플랫폼
        public const uint Auth_InvalidAuthPlatform = 1000;
        /// 벤된 사용자
        public const uint Auth_Banned = 1001;
        /// 인증 삭제 불가
        public const uint Auth_CannotDeleteAuth = 1002;
        /// 사용자 없음
        public const uint Auth_NotFoundUser = 1004;
        /// 비활성화된 사용자
        public const uint Auth_InactiveUser = 1003;
        // PlayerDataResponseStatus ----------------------------------------------------
        /// 데이터 없음
        public const uint PlayerData_PlayerDataNotFound = 1600;
        // PlayerResponseStatus --------------------------------------------------------
        /// 사용자 uid가 없음
        public const uint Player_PlayerUidIsNull = 1500;
        /// 사용자를 찾을 수 없음
        public const uint Player_PlayerNotFound = 1502;
        /// 이미 삭제된 사용자
        public const uint Player_PlayerAlreadyDeleted = 1503;
        /// 서버 아이디가 일치하지 않음
        public const uint Player_PlayerServerIdNotMatched = 1504;
        /// 닉네임이 변경되지 않음
        public const uint Player_PlayerNicknameNotChanged = 1505;
        /// 이미 존재하는 닉네임
        public const uint Player_PlayerNicknameAlreadyExist = 1506;
        // PremiumCurrencyResponseStatus -----------------------------------------------
        public const uint PremiumCurrency_PremiumCurrencyInvalidError = 1801;
        public const uint PremiumCurrency_PremiumCurrencyNotFound = 1804;
        // ServerRankingResponseStatus -------------------------------------------------
        /// 랭크 자체가 없음.
        public const uint ServerRanking_ServerRankNotFound = 2000;
        /// 플레이어 데이터가 없음.
        public const uint ServerRanking_NotFoundPlayer = 2001;
        /// 읽기 전용 서버 랭킹
        public const uint ServerRanking_ServerRankReadonly = 2002;
        // ServerResponseStatus --------------------------------------------------------
        /// 서버 목록이 비었음.
        public const uint Server_ServerListEmpty = 1901;
        /// 서버에 입장할 수 없음.
        public const uint Server_ServerNotJoinable = 1902;
        /// 플레이어를 찾을 수 없음.
        public const uint Server_ServerPlayerNotFound = 1903;
        /// 플레이어가 이미 삭제 되어 있음.
        public const uint Server_ServerPlayerDeleted = 1904;
        // ShopResponseStatus ----------------------------------------------------------
        /// 상품을 찾을 수 없음
        public const uint Shop_ShopItemNotFound = 3000;
        /// 상품이 비활성화 상태
        public const uint Shop_ShopItemNotEnabled = 3001;
        /// 캐시 체크가 필요함
        public const uint Shop_RequiredCashCheck = 3002;
        /// IAP 인증 오류
        public const uint Shop_ShopIapInvalidError = 3003;
        // SpecResponseStatus ----------------------------------------------------------
        /// 스펙 데이터가 없음
        public const uint Spec_SpecNotFound = 2100;
        // StkautoPVPResponseStatus ----------------------------------------------------
        /// 복수전 상대방 매치 아이디가 없음
        public const uint StkautoPVP_PvpRevengeNotFoundMatchId = 16310;
        /// 프로필을 찾을 수 없음
        public const uint StkautoPVP_PvpProfileNotFound = 16301;
        /// Response Status Code에 대한 설명을 반환합니다.
        public static string GetDescription(uint code) => code switch
        {
            0 => "Undefined",
            1000 => "Auth_InvalidAuthPlatform",
            1001 => "Auth_Banned",
            1002 => "Auth_CannotDeleteAuth",
            1004 => "Auth_NotFoundUser",
            1003 => "Auth_InactiveUser",
            1600 => "PlayerData_PlayerDataNotFound",
            1500 => "Player_PlayerUidIsNull",
            1502 => "Player_PlayerNotFound",
            1503 => "Player_PlayerAlreadyDeleted",
            1504 => "Player_PlayerServerIdNotMatched",
            1505 => "Player_PlayerNicknameNotChanged",
            1506 => "Player_PlayerNicknameAlreadyExist",
            1801 => "PremiumCurrency_PremiumCurrencyInvalidError",
            1804 => "PremiumCurrency_PremiumCurrencyNotFound",
            2000 => "ServerRanking_ServerRankNotFound",
            2001 => "ServerRanking_NotFoundPlayer",
            2002 => "ServerRanking_ServerRankReadonly",
            1901 => "Server_ServerListEmpty",
            1902 => "Server_ServerNotJoinable",
            1903 => "Server_ServerPlayerNotFound",
            1904 => "Server_ServerPlayerDeleted",
            3000 => "Shop_ShopItemNotFound",
            3001 => "Shop_ShopItemNotEnabled",
            3002 => "Shop_RequiredCashCheck",
            3003 => "Shop_ShopIapInvalidError",
            2100 => "Spec_SpecNotFound",
            16310 => "StkautoPVP_PvpRevengeNotFoundMatchId",
            16301 => "StkautoPVP_PvpProfileNotFound",
            _ => "Unknown",
        };
        /// Response Status Code가 존재하는지 확인합니다.
        public static bool Contains(uint code) => code switch
        {
            0 => false,
            Auth_InvalidAuthPlatform => true,
            Auth_Banned => true,
            Auth_CannotDeleteAuth => true,
            Auth_NotFoundUser => true,
            Auth_InactiveUser => true,
            PlayerData_PlayerDataNotFound => true,
            Player_PlayerUidIsNull => true,
            Player_PlayerNotFound => true,
            Player_PlayerAlreadyDeleted => true,
            Player_PlayerServerIdNotMatched => true,
            Player_PlayerNicknameNotChanged => true,
            Player_PlayerNicknameAlreadyExist => true,
            PremiumCurrency_PremiumCurrencyInvalidError => true,
            PremiumCurrency_PremiumCurrencyNotFound => true,
            ServerRanking_ServerRankNotFound => true,
            ServerRanking_NotFoundPlayer => true,
            ServerRanking_ServerRankReadonly => true,
            Server_ServerListEmpty => true,
            Server_ServerNotJoinable => true,
            Server_ServerPlayerNotFound => true,
            Server_ServerPlayerDeleted => true,
            Shop_ShopItemNotFound => true,
            Shop_ShopItemNotEnabled => true,
            Shop_RequiredCashCheck => true,
            Shop_ShopIapInvalidError => true,
            Spec_SpecNotFound => true,
            StkautoPVP_PvpRevengeNotFoundMatchId => true,
            StkautoPVP_PvpProfileNotFound => true,
            _ => false,
        };
    }
}