using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GuideMissionSlot : CachedMonoBehaviour
    {
        [SerializeField] private GameObject activateLayerObject;
        [SerializeField] private GameObject activateBackgroundObject;
        [SerializeField] private CAButton guideMissionButton;

        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI missionDescText;

        [SerializeField] private Image missionRewardItemImage;
        [SerializeField] private SpriteLoader missionRewardItemSpriteLoader;
        [SerializeField] private Image rewardCharacterBGImage;
        [SerializeField] private Image missionRewardCharacterImage;
        [SerializeField] private SpriteLoader missionRewardCharacterSpriteLoader;
        [SerializeField] private TextMeshProUGUI missionRewardAmountText;

        private GuideMissionDataBridge guideMissionDataBridge;
        private GuideMissionInfo specGuideMissionData;
        private ElpisFacilityType? pendingFacilityType;
        private Action pendingPopupAction;

        private bool IsRewardClaimable =>
            ServerDataManager.Instance.GuideMission.IsCompleted || ServerDataManager.Instance.GuideMission.IsGoalReached;

        private void Awake()
        {
            guideMissionDataBridge = new GuideMissionDataBridge();

            guideMissionButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickMissionSlotButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);

            // 가이드 미션 데이터 변경 시 UI 갱신
            guideMissionDataBridge.OnChanged
                .Subscribe(this, (_, self) => self.RefreshGuideMissionSlot())
                .AddTo(this);
        }

        public async void InitGuideMissionSlot()
        {
            await guideMissionDataBridge.GetAsync();
            RefreshGuideMissionSlot();
        }

        #region Set UI

        public void RefreshGuideMissionSlot()
        {
            // 모든 가이드 미션 완료 또는 최대 오더 초과 시 off 처리
            if (guideMissionDataBridge.IsAllCompleted || guideMissionDataBridge.Order > SpecDataManager.Instance.GetGuideMissionMaxOrder())
            {
                gameObject.SetActive(false);
                return;
            }

            // 가이드 미션 슬롯 데이터 세팅
            var guideMissionId = (int)guideMissionDataBridge.GuideMissionId;
            specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(guideMissionId);

            if (specGuideMissionData == null)
            {
                gameObject.SetActive(false);
                return;
            }

            SetGuideMissionSlotUI();

            // 다이얼로그 팝업 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, specGuideMissionData.id.ToString(),
                () =>
                {
                    if (specGuideMissionData.id == 1)
                        SceneUILayerManager.Instance.PushUILayerAsync<NicknamePopup>(true).Forget();
                });

        }

        private async void SetGuideMissionSlotUI()
        {
            missionTitleText.text = LanguageManager.Instance.GetDefaultText(specGuideMissionData.name_token);
            missionDescText.text = LanguageManager.Instance.GetDefaultText(specGuideMissionData.desc_token);

            SetGuideMissionRewardImage();
            missionRewardAmountText.text = $"x{specGuideMissionData.item_count}";

            // 보상 수령 가능 여부에 따라 활성화 레이어 표시
            activateLayerObject.SetActive(IsRewardClaimable); // ! GUIDE_TODO IsCompleted
            activateBackgroundObject.SetActive(IsRewardClaimable); // ! GUIDE_TODO IsCompleted
        }

        private void SetGuideMissionRewardImage()
        {
            ItemId itemId = specGuideMissionData.item_id;
            var isCharacter = itemId.IsCharacter();
            var isCharacterPiece = itemId.IsCharacterPiece();

            missionRewardItemImage.gameObject.SetActive(!isCharacter);
            rewardCharacterBGImage.gameObject.SetActive(isCharacter);
            missionRewardCharacterImage.gameObject.SetActive(isCharacter);

            if (isCharacter)
            {
                itemId.GetCharacterId(out var charIndex);
                var characterData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);
                missionRewardCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(characterData.prefab_id)).Forget();
            }
            else if (isCharacterPiece)
            {
                itemId.GetCharacterId(out var charIndex);
                var characterPieceData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);
                missionRewardItemSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(characterPieceData.id)).Forget();
            }
            else
            {
                missionRewardItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(itemId)).Forget();
            }
        }

        private async UniTask OnClickMissionSlotButtonAsync()
        {
            if (specGuideMissionData == null) return;

            if (IsRewardClaimable) // ! GUIDE_TODO IsCompleted
            {
                await ClaimRewardAsync();
            }
            else
            {
                HandleNavigateByGuideType();
                ObjectRegistry.GetObject<GuideAlert>(RegistryKey.GuideAlert)?.UpdateAlert();
            }
        }

        #endregion

        #region Reward

        private static bool IS_DEBUG_CLAIM = false;

        public static void SetDebugClaim() { IS_DEBUG_CLAIM = true; }

        private static bool USE_DEBUG_CLAIM()
        {
            if (IS_DEBUG_CLAIM)
            {
                IS_DEBUG_CLAIM = false;
                return true;
            }
            return false;
        }

        private async UniTask ClaimRewardAsync()
        {
            var response = await guideMissionDataBridge.ClaimRewardAsync(guideMissionDataBridge.GuideMissionId);
            if ((response == null || !response.IsSuccess) && !USE_DEBUG_CLAIM())
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_ERROR_NETWORK");
                return;
            }

            // 보상 목록 생성 (LINQ 사용)
            var rewardItemList = response.Rewards?.Select(reward => new RewardItem(reward)).ToList()
                                 ?? new List<RewardItem>();

            // 보상 팝업 표시
            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), callback =>
            {
                // 팝업 닫힌 후 다음 가이드 미션 튜토리얼 체크
                AppEventManager.Instance.GuideMissionClear(specGuideMissionData.order);
                // 팝업 닫힌 후 다음 튜토리얼 시작
                if (!TutorialManager.IsSkipTutorial)
                    TutorialManager.Instance.TryStartOutgameTutorial().Forget();
            }).Forget();

            await guideMissionDataBridge.GetAsync();
        }

        #endregion

        #region Navigation

        private void HandleNavigateByGuideType()
        {
            var buildInfo = SpecDataManager.Instance.GetBuildInfo(specGuideMissionData.sub_key);
            switch (specGuideMissionData.guide_mission_type)
            {
                case GuideMissionType.SUMMON_CHARACTER:
                    NavigateToGacha();
                    break;

                case GuideMissionType.LEVELUP_CHARACTER_TARGET:
                case GuideMissionType.EXCEED_CHARACTER_TARGET:
                case GuideMissionType.TRANSCENDENCE_CHARACTER_TARGET:
                    NavigateToCharacterCollection();
                    break;

                case GuideMissionType.USE_BUILDING:
                    NavigateToBuildingPopup(buildInfo);
                    break;

                case GuideMissionType.UPGRADE_BUILDING:
                case GuideMissionType.INSTALL_BUILDING:
                    NavigateAndFocusToBuilding(buildInfo);
                    break;

                case GuideMissionType.UPGRADE_DIMENSION_CUBE_CORE_RESEARCH:
                    NavigateToLobbyAndOpenPopup(ElpisFacilityType.FacilityTypeDimensionLab,
                        async () => await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCoreResearchLayer>());
                    break;

                case GuideMissionType.CLEAR_STAGE:
                case GuideMissionType.ENTER_CHAPTER:
                    NavigateToStage();
                    break;

                case GuideMissionType.CLEAR_BABEL:
                    NavigateToBabel();
                    break;
            }
        }

        private void NavigateAndFocusToBuilding(ElpisBuildInfo buildInfo)
        {
            if (buildInfo == null) return;

            var facilityType = buildInfo.facility_type.ToServerType();
            NavigateToLobbyAndOpenPopup(facilityType,
                () => OpenElpisBuildLayerForFacility(facilityType));
        }

        private void NavigateToGacha()
        {
            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget();
            }
        }

        private void NavigateToCharacterCollection()
        {
            if (!guideMissionDataBridge.IsCompleted && specGuideMissionData.tutorial_id > 0)
                NavigateToLobbyAndOpenPopup(null, null);
            else
                NavigateToLobbyAndOpenPopup(null, () => SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget());
        }

        private void NavigateToBuildingPopup(ElpisBuildInfo buildInfo)
        {
            if (buildInfo == null) return;

            var facilityType = buildInfo.facility_type.ToServerType();
            var buildId = facilityType switch
            {
                ElpisFacilityType.FacilityTypeCommandCenter => (int)IdMap.ElpisBuild.CommandCenter,
                ElpisFacilityType.FacilityTypeDimensionLab => (int)IdMap.ElpisBuild.DimensionLab,
                _ => -1
            };
            if (buildId < 0) return;

            NavigateToLobbyAndOpenPopup(facilityType, async () =>
            {
                var elpisInfo = await NetManager.Instance.Elpis.FinishUpgradingFacilityAsync(buildId);
                ElpisBuildingPopup.OpenPopup(elpisInfo.Facility).Forget();
            });
        }

        /// <summary>
        /// 로비로 이동 후 팝업을 열고, facilityType이 지정된 경우 카메라 포커스도 수행한다.
        /// </summary>
        private void NavigateToLobbyAndOpenPopup(ElpisFacilityType? facilityType, Action popupAction)
        {
            var currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Lobby")
            {
                if (facilityType.HasValue)
                    FocusCameraOnFacility(facilityType.Value);
                popupAction?.Invoke();
            }
            else
            {
                pendingFacilityType = facilityType;
                pendingPopupAction = popupAction;
                SceneUILayerManager.OnSceneLoadedEvent += OnLobbySceneLoaded;
                NavigateToLobbyAsync().Forget();
            }
        }

        private async UniTask NavigateToLobbyAsync()
        {
            var battleReadyMain = BattleReadyMain.GetBattleReadyMain();
            if (battleReadyMain != null)
            {
                await battleReadyMain.OnClickGoToLobby();
            }
        }

        private void OnLobbySceneLoaded(string sceneName)
        {
            if (sceneName != "Lobby") return;

            SceneUILayerManager.OnSceneLoadedEvent -= OnLobbySceneLoaded;

            if (pendingFacilityType.HasValue)
            {
                FocusCameraOnFacility(pendingFacilityType.Value);
                pendingFacilityType = null;
            }

            pendingPopupAction?.Invoke();
            pendingPopupAction = null;
        }

        private void FocusCameraOnFacility(ElpisFacilityType facilityType)
        {
            var bridge = new ElpisDataBridge();
            var facility = bridge.GetFacilityByType(facilityType);
            if (facility == null) return;

            var buildId = (int)facility.BuildId;

            var lobbyBuildingUIs = FindObjectsByType<LobbyBuildingInteractionUI>(FindObjectsSortMode.None);
            foreach (var ui in lobbyBuildingUIs)
            {
                foreach (var info in ui.CachedFacilityInfos)
                {
                    if (info.buildInfo.build_id == buildId)
                    {
                        var cameraController = MainCameraHolder.CameraGestureController;
                        cameraController.ZoomAndMoveAsync(ui.TargetWorldPosition, 10.0f, 0.3f).Forget();
                        return;
                    }
                }
            }
        }

        private void OpenElpisBuildLayerForFacility(ElpisFacilityType facilityType)
        {
            var lobbyBuildingUIs = FindObjectsByType<LobbyBuildingInteractionUI>(FindObjectsSortMode.None);
            foreach (var ui in lobbyBuildingUIs)
            {
                foreach (var info in ui.CachedFacilityInfos)
                {
                    if (info.buildInfo.facility_type.ToServerType() == facilityType)
                    {
                        var newParam = new ElpisBuildLayer.ElpisBuildCacheData
                        {
                            facilityInfos = ui.CachedFacilityInfos,
                            targetLobbyBuildingUI = ui,
                            slotIndex = ui.SlotIndex
                        };
                        SceneUILayerManager.Instance.PushUILayerAsync<ElpisBuildLayer>(newParam).Forget();
                        return;
                    }
                }
            }
        }

        private void NavigateToBabel()
        {
            var currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Lobby")
            {
                LobbyMain.GetLobbyMain()?.OnClickDungeonButton();
            }
            else
            {
                NavigateToLobbyAndOpenPopup(null, () => LobbyMain.GetLobbyMain()?.OnClickDungeonButton());
            }
        }

        private void NavigateToStage()
        {
            NavigateToStageAsync().Forget();
        }

        private async UniTask NavigateToStageAsync()
        {
            var currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == "BattleReady")
            {
                var guideStageData = SpecDataManager.Instance.GetStageData(specGuideMissionData.sub_key);

                // 스테이지 해금 여부 확인
                if (!ServerDataManager.Instance.Battle.IsStageOpen((uint)guideStageData.stage_id))
                {
                    ToastManager.Instance.ShowToastByTokenKey("MSG_LOCK_STAGE");
                    return;
                }

                var currentStageData = SpecDataManager.Instance.GetStageData((int)LocalDataManager.Instance.GetLastPlayStageId());
                var isSameChapter = currentStageData.chapter_id == guideStageData.chapter_id;

                // 타겟 스테이지 설정
                LocalDataManager.Instance.SetLastPlayStageId((uint)guideStageData.stage_id);

                if (!isSameChapter)
                {
                    // 가이드 챕터로 이동 후 BattleReady 다시 로드
                    SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                    await SceneTransition.FadeInAsync();
                    InGameManager.Instance.EndInGame();

                    SceneLoading.GoToNextScene("BattleReady", guideStageData.chapter_id);
                }
                else
                {
                    // 가이드 스테이지로 전투 진입
                    var inGameParams = await NetManager.Instance.Battle.StartAsync(guideStageData.chapter_id, guideStageData.stage_id, 0, Array.Empty<string>());
                    if (inGameParams == null)
                        return;

                    SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                    await SceneTransition.FadeInAsync();
                    InGameManager.Instance.EndInGame();

                    SceneLoading.GoToNextSceneWithStageEnterTrigger("InGame", guideStageData.stage_id, inGameParams);
                }

                return;
            }

            // Lobby 씬에서는 LobbyMain.OnClickStartButton() 사용
            if (currentSceneName == "Lobby")
            {
                var lobbyMain = LobbyMain.GetLobbyMain();
                if (lobbyMain != null)
                {
                    await lobbyMain.OnClickStartButton();
                }
            }
        }

        #endregion

    }
}
