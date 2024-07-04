#define ENABLE_BATTLE_LOG
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using Cysharp.Text;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public enum ActionType
    {
        Damaged,
        Healed,
    }

    public readonly struct ActionLog
    {
        public readonly float elapsedTime;
        public readonly bool isPlayerCharacter;
        public readonly int srcCharacterUId;
        public readonly int srcCharacterId;
        public readonly int destCharacterUId;
        public readonly int destCharacterId;
        public readonly ActionType actionType;
        public readonly double value;

        public ActionLog(float elapsedTime, bool srcIsPlayerCharacter, int srcCharacterUId, int srcCharacterId, int destCharacterUId, int destCharacterId, ActionType actionType, double value)
        {
            this.elapsedTime = elapsedTime;
            this.isPlayerCharacter = srcIsPlayerCharacter;
            this.srcCharacterUId = srcCharacterUId;
            this.srcCharacterId = srcCharacterId;
            this.destCharacterUId = destCharacterUId;
            this.destCharacterId = destCharacterId;
            this.actionType = actionType;
            this.value = value;
        }
    }

    [Serializable]
    public class StatisticsData
    {
        public List<ActionLog> combatLogs = new ();

        public ObfuscatorFloat totalCombatTime = 0f;
        public ObfuscatorInt deathCount = 0;
    }

    public class InGameStatistics : SingletonMonoBehaviour<InGameStatistics>
    {
        // combat data
        StatisticsData statisticsData = new ();

        public void Clear()
        {
            ClearStatisticsData();
    #if UNITY_EDITOR && ENABLE_BATTLE_LOG
            CloseBattleLog();
    #endif
        }

        public void AddCombatDamage(CharacterController attacker, CharacterController receiver, double damageAmount, double currHp, long source)
        {
            // var realDamageAmount = damageAmount + (currHp > 0 ? 0 : currHp);
            if (attacker != null)
            {
                if(attacker.AllianceType == AllianceType.Enemy)
                    return;
            }

            bool isPlayerCharacter = attacker?.AllianceType == AllianceType.Player;

            //make actionlog
            var actionLog = new ActionLog(
                statisticsData.totalCombatTime,
                isPlayerCharacter,
                attacker?.CharacterUId ?? 0,
                attacker?.CharacterId ?? 0,
                receiver?.CharacterUId ?? 0,
                receiver?.CharacterId ?? 0,
                ActionType.Damaged,
                damageAmount);

            statisticsData.combatLogs.Add(actionLog);
    #if UNITY_EDITOR && ENABLE_BATTLE_LOG
            WriteBattleDamageLog(attacker, receiver, damageAmount, currHp, source);
    #endif
        }

        public void AddCombatHeal(CharacterController giver, CharacterController receiver, double healAmount, double currHp, double maxHp, long source)
        {
            // var nextHp = currHp + healAmount;
            // if (nextHp > maxHp)
            //     nextHp = maxHp;
            // var realHealAmount = nextHp - currHp;
            //make actionlog
            bool isPlayerCharacter = giver != null && giver.AllianceType == AllianceType.Player;
            var actionLog = new ActionLog(
                statisticsData.totalCombatTime,
                isPlayerCharacter,
                giver?.CharacterUId ?? 0,
                giver?.CharacterId ?? 0,
                receiver?.CharacterUId ?? 0,
                receiver?.CharacterId ?? 0,
                ActionType.Healed,
                healAmount);

            statisticsData.combatLogs.Add(actionLog);
    #if UNITY_EDITOR && ENABLE_BATTLE_LOG
            WriteBattleHealLog(giver, receiver, healAmount, currHp, maxHp, source);
    #endif
        }

        public double GetAttackDamageAmount(int id)
        {
            var damageAmount = 0d;
            foreach (var log in statisticsData.combatLogs)
            {
                if (log.srcCharacterId == id && log.actionType == ActionType.Damaged)
                {
                    damageAmount += log.value;
                }
            }

            return damageAmount;
        }

        public double GetTotalAttackDamageAmount()
        {
            var damageAmount = 0d;
            foreach (var log in statisticsData.combatLogs)
            {
                if (log.isPlayerCharacter && log.actionType == ActionType.Damaged)
                    damageAmount += log.value;
            }

            return damageAmount;
        }

        public double GetTakenDamageAmount(int id)
        {
            var damageAmount = 0d;
            foreach (var log in statisticsData.combatLogs)
            {
                if (log.destCharacterUId == id && log.actionType == ActionType.Damaged)
                {
                    damageAmount += log.value;
                }
            }
            return damageAmount;
        }

        public double GetGivenHealAmount(int id)
        {
            var healAmount = 0d;
            foreach (var log in statisticsData.combatLogs)
            {
                if (log.srcCharacterUId == id && log.actionType == ActionType.Healed)
                {
                    healAmount += log.value;
                }
            }
            return healAmount;
        }

        public double GetTakenHealAmount(int id)
        {
            var healAmount = 0d;
            foreach (var log in statisticsData.combatLogs)
            {
                if (log.destCharacterUId == id && log.actionType == ActionType.Healed)
                {
                    healAmount += log.value;
                }
            }
            return healAmount;
        }

        public int GetMvpID()
        {
            int mvpID = 0;
            float attackDamageWeight = 1.0f;
            float takenDamageWeight = 0.0f;
            float givenHealWeight = 0.0f;
            double maxScore = double.MinValue;

            foreach (var character in InGameObjectManager.Instance.StartingPlayerCharacters)
            {
                double attackDamageAmount = GetAttackDamageAmount(character.CharacterId);
                double takenDamageAmount = GetTakenDamageAmount(character.CharacterId);
                double givenHealAmount = GetGivenHealAmount(character.CharacterId);

                double score = attackDamageAmount * attackDamageWeight + takenDamageAmount * takenDamageWeight + givenHealAmount * givenHealWeight;

                if (score > maxScore)
                {
                    maxScore = score;
                    mvpID = character.CharacterId;
                }
            }

            return mvpID;
        }

        private void ClearStatisticsData()
        {
            statisticsData.combatLogs.Clear();
            statisticsData.deathCount = 0;
            statisticsData.totalCombatTime = 0;
        }

        public float GetTotalPlaySecond()
        {
            return statisticsData.totalCombatTime;
        }

        public int GetDeathCount()
        {
            return statisticsData.deathCount;
        }

        private StatisticsData GetStatisticsData()
        {
            return statisticsData;
        }

        public void AddDeathCount(int deathCount)
        {
            statisticsData.deathCount += deathCount;
        }

        // private void Update()
        // {
        //     if (!InGameManager.Instance.IsInGamePlaying)
        //         return;
        //
        //     if (InGameMainFlowManager.Instance.IsPaused)
        //         return;
        //
        //     if (InGameMainFlowManager.Instance.IsCombatRunning)
        //     {
        //         statisticsData.totalCombatTime += Time.unscaledDeltaTime;
        //     }
        // }

    #if UNITY_EDITOR && ENABLE_BATTLE_LOG

        private StreamWriter logFileWriter;
        private StreamWriter CreateBattleLogWriter()
        {
#if UNITY_EDITOR
            // var dataPath = Application.dataPath;
            // var index = dataPath.LastIndexOf(Path.DirectorySeparatorChar);
            // var projectRootPath = dataPath[..index];
            // string folderPath = Path.Combine(projectRootPath, "Logs");
            // if (!Directory.Exists(folderPath))
            // {
            //     Directory.CreateDirectory(folderPath);
            // }
    #else
            // string folderPath = Path.Combine(Application.persistentDataPath, "Logs");
            // if (!Directory.Exists(folderPath))
            // {
            //     Directory.CreateDirectory(folderPath);
            // }
    #endif

            // var path = Path.Combine(folderPath, ZString.Format("battlelog_{0}_{1}.log", DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss")));
            // return File.CreateText(path);
            return null;
        }

        private StringBuilder builder = new ();
        private void WriteBattleDamageLog(CharacterController attackerCtrl, CharacterController receiverCtrl, double damageAmount, double currHp, long source)
        {
            logFileWriter ??= CreateBattleLogWriter();

            builder.Append("공격자:");

            if (attackerCtrl != null)
            {
                builder.Append(attackerCtrl.CharacterId);
                builder.Append("(");
                builder.Append(attackerCtrl.CharacterUId);
                builder.Append("),");
            }
            else
            {
                builder.Append("X,");
            }
            builder.Append("피격자:");
            if (receiverCtrl != null)
            {
                builder.Append(receiverCtrl.CharacterId);
                builder.Append("(");
                builder.Append(receiverCtrl.CharacterUId);
                builder.Append("),");
            }
            else
            {
                builder.Append("X,");
            }
            builder.Append("입힌 대미지:");
            builder.Append(damageAmount);
            builder.Append(",남은 체력:");
            builder.Append(currHp);
            builder.Append(",원인:");
            if (source == 0)
            {
                builder.Append("기본공격");
            }
            else
            {
                builder.Append(source);
            }

            logFileWriter?.WriteLine(builder.ToString());
            builder.Clear();
            logFileWriter?.Flush();
        }

        private void WriteBattleHealLog(CharacterController giverCtrl, CharacterController receiverCtrl, double healAmount, double currHp, double maxHp, long source)
        {
            logFileWriter ??= CreateBattleLogWriter();

            builder.Append("치유자:");
            if (giverCtrl != null)
            {
                builder.Append(giverCtrl.CharacterId);
                builder.Append("(");
                builder.Append(giverCtrl.CharacterUId);
                builder.Append("),");
            }
            else
            {
                builder.Append("X,");
            }
            builder.Append("회복자:");
            if (receiverCtrl != null)
            {
                builder.Append(receiverCtrl.CharacterId);
                builder.Append("(");
                builder.Append(receiverCtrl.CharacterUId);
                builder.Append("),");
            }
            else
            {
                builder.Append("X,");
            }

            builder.Append("치유량:");
            builder.Append(healAmount);
            builder.Append(",치유전체력:");
            builder.Append(currHp);
            builder.Append(",최대체력:");
            builder.Append(maxHp);
            builder.Append(",원인:");
            if (source == 0)
            {
                builder.Append("기본치유");
            }
            else
            {
                builder.Append(source);
            }

            logFileWriter?.WriteLine(builder.ToString());
            builder.Clear();
            logFileWriter?.Flush();
        }

        private void CloseBattleLog()
        {
            logFileWriter?.Close();
            logFileWriter = null;
        }
    #endif
    }
}
