using System.Collections.Generic;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameMainStateTest : IGameStateUICore, IReturnCharacterUI, IKillLogUI, IBottomScrollRectCheck
    {
        // 테스트 설정 주소 (Addressables)
        private const string TestConfigAddress = "Data/InGameTestConfig.asset";

        private InGameUI _inGameUI;
        private InGameTestConfig _testConfig;

        private float _updateTimer = 0f;
        private const float UpdateInterval = 0.3f;

        public async UniTask Initialize(Transform canvasTransform, int id)
        {
            // Addressables에서 테스트 설정 로드
            _testConfig = await Addressables.LoadAssetAsync<InGameTestConfig>(TestConfigAddress);

            // 모드에 따른 리소스 로드
            if (_testConfig.Mode == TestMode.Stage && _testConfig.StageId > 0)
            {
                var stageData = SpecDataManager.Instance.GetStageData(_testConfig.StageId);
                if (stageData != null)
                {
                    Debug.LogColor($"[Test] Stage 모드: {stageData.chapter_id}-{stageData.stage_number} (맵: {stageData.map_size})", "yellow");
                    await InGameResourceHolder.LoadResources(InGameType.TEST, this, stageData.chapter_id);
                }
                else
                {
                    Debug.LogError($"[Test] 스테이지 데이터를 찾을 수 없음: {_testConfig.StageId}");
                    await InGameResourceHolder.LoadResources(InGameType.TEST, this, _testConfig.StageChapterId);
                }
            }
            else
            {
                Debug.LogColor($"[Test] Custom 모드 - 챕터: {_testConfig.StageChapterId}, 그리드: {_testConfig.GridWidth}x{_testConfig.GridHeight}", "cyan");
                await InGameResourceHolder.LoadResources(InGameType.TEST, this, _testConfig.StageChapterId);
            }

            await InitializeInternal(canvasTransform);
        }


        private async UniTask InitializeInternal(Transform canvasTransform)
        {
            var stageUIObj = await Addressables.LoadAssetAsync<GameObject>("Prefabs/UI/InGame/StageUI.prefab").Task;
            _inGameUI = Object.Instantiate(stageUIObj, canvasTransform).GetComponent<InGameUI>();
            _inGameUI.transform.SetSiblingIndex(2);

            _inGameUI.TopUI.SetMyName("[TEST MODE]");
            _inGameUI.TopUI.SetStageName("Test Battle");

            // 테스트 게임 시작
            InGameManager.Instance.StartInGame<FlowStateInGameTestReady>((_testConfig));
        }

        public void RefreshInGameTopUI(bool isCombat)
        {
            _inGameUI.TopUI.UpdateSynergyUI(AllianceType.Player, isCombat);
            _inGameUI.TopUI.UpdateSynergyUI(AllianceType.Enemy, isCombat);

            _inGameUI.TopUI.UpdateAttrUI(AllianceType.Player, isCombat);
            _inGameUI.TopUI.UpdateAttrUI(AllianceType.Enemy, isCombat);
        }

        public void ReturnCharacterUI(CharacterController characterController)
        {
            _inGameUI.BottomUI.ReturnCharacter(characterController);
            InGameManager.Instance.UpdateSynergyAndAttr();
        }

        public void ManagedUpdate(float dt)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase)
            {
                _updateTimer += dt;
                if (InGameManager.Instance.IsInGameCombat)
                {
                    InGameMain.GetInGameMain().SetInGameTime(InGameMain.GetInGameMain().InGameTime - dt);
                    _inGameUI.BottomUI.UpdateCommanderSkillCoolTime();
                }

                if (_updateTimer >= UpdateInterval)
                {
                    _inGameUI.TopUI.UpdateTopHpUI(AllianceType.Player);
                    _inGameUI.TopUI.UpdateTopHpUI(AllianceType.Enemy);
                    _inGameUI.TopUI.UpdateTimeUI(InGameMain.GetInGameMain().InGameTime);

                    _updateTimer -= UpdateInterval;
                }
            }
        }

        public void InitReadyStateUI(List<Tech.Hive.V1.DeckCharacterPlacement> battleDeckList)
        {
            _inGameUI.PlayAnimation("SetEntry");
            _inGameUI.BottomUI.InitData();
            RefreshInGameTopUI(false);

            float battleTimeLimit = _testConfig?.BattleTimeLimit ?? 60f;
            InGameMain.GetInGameMain().SetInGameTime(battleTimeLimit);

            _inGameUI.TopUI.InitTopUI(typeof(FlowStateInGameTestCombat));
            _inGameUI.BottomUI.InitReadyStateUI(typeof(FlowStateInGameTestCombat), battleDeckList, _testConfig);
        }

        public void InitCombatStateUI()
        {
            _inGameUI.PlayAnimation("SetBattleEntry");
            _inGameUI.TopUI.InitCombatTopUI();
            _inGameUI.BottomUI.InitCommanderSkill();
            _inGameUI.BottomUI.InitSpeedUpSetting();
            InGameMain.GetInGameMain().RefreshInGameTopUI(true);
        }

        public bool IsCheckTouchTile(InGameTile tile)
        {
            return tile.IsOccupied() && (tile.OccupiedCharacter.AllianceType == AllianceType.Player);
        }

        public void AddKillLog(in KillSource source, CharacterController death, bool isPlayerKill)
        {
            _inGameUI.TopUI.AddKillLog(source, death, isPlayerKill);
        }

        public bool IsPointInBottomScrollRect(UnityEngine.Vector2 screenPosition)
        {
            return _inGameUI.BottomUI.IsPointInScrollRect(screenPosition);
        }

        public void SetDropHighlight(bool active)
        {
            _inGameUI.BottomUI.SetDropHighlight(active);
        }
    }
}
