using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler.Prologue
{
    public static class PrologueID
    {
        public const int 프롤로그유니ID = 10;
        public const int 프롤로그필리아ID = 11;
        public const int 프롤로그아트레시아ID = 12;
        public const int 프롤로그클레이ID = 13;
        public const int 프롤로그마리에ID = 15;
        public const int 프롤로그라플라스마녀ID = 9002;
    }

    public static class PrologueDelays
    {
        public const int 라플라스SKL애니메이션타이밍 = 2000;
    }

    public static class PrologueUtility
    {
        public static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static GameObject PrologueStageUI = null;
    }


    public class FlowStatePrologueReady : StateReadyBase
    {
        public override void SetStateData(object data)
        {
            base.SetStateData(data);
            //[TODO] 프롤로그 사운드 추가 필요
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_dgboss_01);
            InGameMain.GetInGameMain().SetVignette(0);
        }

        public override async void StateInit(object target)
        {
            var addCharacterTasks = new List<UniTask<CharacterController>>();
            
            PrologueUtility.FindChildRecursive(PrologueUtility.PrologueStageUI.transform, "Body_Left").gameObject.SetActive(false);
            PrologueUtility.FindChildRecursive(PrologueUtility.PrologueStageUI.transform, "Body_Right").gameObject.SetActive(false);


            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPosition(new Vector3(-10, 2.5f, -10));
            await ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(5, new Vector3(-10, 2.5f, -10), 0);
            // ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(8.5f,new Vector3(-1f, 0f, -10), 1.0f).Forget();

            SpawnPrologueCharacters(addCharacterTasks);

            await UniTask.WhenAll(addCharacterTasks);

            InGameMainFlowManager.Instance.AddNextState<FlowStatePrologueCombat>();
        }

        public override void StateStart()
        {
        }

        public override void StateRunning(float dt)
        {
        }

        public override void StateEnd(bool isForced)
        {
        }

        // 프롤로그 시나리오 캐릭터 소환
        private void SpawnPrologueCharacters(List<UniTask<CharacterController>> addCharacterTasks)
        {
            // 프롤로그 시나리오 캐릭터 ID 및 위치 정의
            // 클레이 (130101), 유니 (130201), 필리아 (130301), 아트레시아 (130401)
            // 마리에 (130501)는 나중에 합류하므로 초기에는 소환하지 않음

            // 기본 레벨 설정 (필요시 조정)
            int prologueCharacterLevel = 1;

            // 프롤로그 플레이어 캐릭터 위치 (플레이어 진영 앞쪽)

            var prologueCharacterPositions = new Dictionary<int, int2>
        {
            { PrologueID.프롤로그유니ID, new int2(1, 2) }, // 유니
            { PrologueID.프롤로그필리아ID, new int2(3, 2) }, // 필리아
            { PrologueID.프롤로그아트레시아ID, new int2(2, 2) },  // 아트레시아 (중앙 앞)
            { PrologueID.프롤로그클레이ID, new int2(2, 1) }
        };

            // 플레이어 캐릭터 소환
            foreach (var kvp in prologueCharacterPositions)
            {
                int characterId = kvp.Key;
                int2 position = kvp.Value;

                Debug.LogColor($"프롤로그 캐릭터 추가 : {characterId} at ({position.x}, {position.y})");

                var characterStat = new CharacterStatData(characterId, prologueCharacterLevel,
                    GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

                // Debug.LogColor(Newtonsoft.Json.JsonConvert.SerializeObject(characterStat),"cyan");

                addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, position,
                    AllianceType.Player,
                    typeof(CharacterStateReady), true, HpBarType.Synergy));
            }

            //라플라스 마녀 소환 
            //[TODO] 라플라스 마녀의 실제 캐릭터 ID로 변경 필요
            int laplaceWitchId = PrologueID.프롤로그라플라스마녀ID; // 임시 ID (Trial 던전 보스)
            int2 witchPosition = new int2(2, 10);

            Debug.LogColor($"라플라스 마녀 추가 : {laplaceWitchId} at ({witchPosition.x}, {witchPosition.y})");

            var witchStat = new CharacterStatData(laplaceWitchId, 5, 0.0f, 12.0f);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(witchStat, witchPosition,
                AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }
    }

}