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
        // 데이터 모델
        public CharacterModel Character { get; private set; } = new ();
        public InventoryModel Inventory { get; private set; } = new ();
        public ElpisModel Elpis { get; private set; } = new ();
        public BattleModel Battle { get; private set; } = new ();
        public PlayerDataModel PlayerData { get; private set; } = new ();
        public CommanderSkillModel CommanderSkill { get; private set; } = new ();
        public DeckModel Deck { get; private set; } = new ();
        public GuideMissionModel GuideMission { get; private set; } = new ();

        /// <summary>
        /// 모든 데이터 초기화
        /// </summary>
        public void ClearAll()
        {
            Character.Reset();
            Inventory.Reset();
            Elpis.Reset();
            Battle.Reset();
            PlayerData.Reset();
            CommanderSkill.Reset();
            Deck.Reset();
            GuideMission.Reset();
        }
    }
}