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
        [SerializeField] private CAButton guideMissionButton;

        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI missionDescText;

        [SerializeField] private Image missionRewardItemImage;
        [SerializeField] private SpriteLoader missionRewardItemSpriteLoader;
        [SerializeField] private Image rewardCharacterBGImage;
        [SerializeField] private Image missionRewardCharacterImage;
        [SerializeField] private SpriteLoader missionRewardCharacterSpriteLoader;
        [SerializeField] private TextMeshProUGUI missionRewardAmountText;

        private GuideMissionDataBridge dataBridge;
        private GuideMissionInfo specGuideMissionData;
        private ElpisFacilityType? pendingFacilityType;

        private void Awake()
        {
            dataBridge = new GuideMissionDataBridge();

            guideMissionButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickMissionSlotButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);

            // 가이드 미션 데이터 변경 시 UI 갱신
            dataBridge.OnChanged
                .Subscribe(this, (_, self) => self.RefreshGuideMissionSlot())
                .AddTo(this);
        }

        public void InitGuideMissionSlot()
        {

            RefreshGuideMissionSlot();
        }

        #region Set UI

        public void RefreshGuideMissionSlot()
        {
            // 모든 가이드 미션 완료 또는 최대 오더 초과 시 off 처리
            if (dataBridge.IsAllCompleted || dataBridge.Order > SpecDataManager.Instance.GetGuideMissionMaxOrder())
            {
                gameObject.SetActive(false);
                return;
            }

            // 가이드 미션 슬롯 데이터 세팅
            var guideMissionId = (int)dataBridge.GuideMissionId;
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

        private void SetGuideMissionSlotUI()
        {
            missionTitleText.text = LanguageManager.Instance.GetDefaultText(specGuideMissionData.name_token);
            missionDescText.text = LanguageManager.Instance.GetDefaultText(specGuideMissionData.desc_token);

            SetGuideMissionRewardImage();
            missionRewardAmountText.text = $"x{specGuideMissionData.item_count}";

            // 보상 수령 가능 여부에 따라 활성화 레이어 표시
            activateLayerObject.SetActive(dataBridge.CanClaimReward);
        }

        private void SetGuideMissionRewardImage()
        {
            ItemId itemId = specGuideMissionData.item_id;
            var isCharacter = itemId.IsCharacter();
            var isCharacterPiece = itemId.IsCharacterPiece();

            missionRewardItemImage.gameObject.SetActive(!isCharacter && !isCharacterPiece);
            rewardCharacterBGImage.gameObject.SetActive(isCharacter);
            missionRewardCharacterImage.gameObject.SetActive(isCharacterPiece);

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
            await GuideMissionTestUtility.HandleIteratively();
            if (specGuideMissionData == null) return;

            if (dataBridge.CanClaimReward)
            {
                await ClaimRewardAsync();
            }
            else
            {
                HandleGuideMissionNavigation();
            }
        }

        #endregion

        #region Reward

        private async UniTask ClaimRewardAsync()
        {
            var response = await dataBridge.ClaimRewardAsync(dataBridge.GuideMissionId);
            if (response == null || !response.IsSuccess)
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
                TutorialManager.Instance.TryStartOutgameTutorial().Forget();
            }).Forget();
        }

        #endregion

        #region Navigation

        private Action pendingPopupAction;

        private void HandleGuideMissionNavigation()
        {
            // HandleGuideMissionNavigateByGuideType();
            HandleGuideMissionNavigateByGuideID();
            ObjectRegistry.GetObject<GuideAlert>(RegistryKey.GuideAlert)?.UpdateAlert();
        }

        private void HandleGuideMissionNavigateByGuideID()
        {
            switch (specGuideMissionData.id)
            {
                // 101: 캐릭터 소환
                case 101:
                    NavigateToLobbyAndOpenPopup(() => SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget());
                    break;

                // 201, 404: 시설 설치
                case 201:
                case 404:
                    var nestBridge = new ElpisDataBridge();
                    NavigateToLobbyAndOpenPopupWithFocus(ElpisFacilityType.FacilityTypeNest, async () => await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterPopup>(nestBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeNest)));
                    break;

                // 202, 305, 402: 캐릭터 강화
                case 202:
                case 305:
                case 402:
                    NavigateToLobbyAndOpenPopup(() => SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget());
                    break;

                // 301~304, 307~310: 스테이지 클리어
                case 301:
                case 302:
                case 303:
                case 304:
                case 306:
                case 307:
                case 308:
                case 309:
                case 310:
                    NavigateToStage();
                    break;

                // 401, 403: 커맨드 센터 사용
                case 401:
                case 403:
                    var commandCenterBridge = new ElpisDataBridge();
                    NavigateToLobbyAndOpenPopupWithFocus(ElpisFacilityType.FacilityTypeCommandCenter, async () => await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterPopup>(commandCenterBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter)));
                    break;

                // 405: 디멘션 큐브 설치
                case 405:
                    var dimensionInstallBridge = new ElpisDataBridge();
                    NavigateToLobbyAndOpenPopupWithFocus(ElpisFacilityType.FacilityTypeDimensionLab, async () => await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterPopup>(dimensionInstallBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeDimensionLab)));
                    break;

                // 406: 디멘션 큐브 사용
                case 406:
                    var dimensionUseBridge = new ElpisDataBridge();
                    NavigateToLobbyAndOpenPopupWithFocus(ElpisFacilityType.FacilityTypeDimensionLab, async () => await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterPopup>(dimensionUseBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeDimensionLab)));
                    break;
                
                // 407: 배틀 시뮬레이션 진입
                case 407:
                    // 배틀 시뮬레이션 네비게이션 추가 필요
                    break;
                
                // 501: 챕터 진입
                case 501:
                    NavigateToStage();
                    break;

                case 502:
                case 503:
                case 504:
                case 507:
                case 508:
                case 509:
                    NavigateToStage();
                    break;
                
                // 505, 506, 601: 바벨의 탑 클리어
                case 505:
                case 506:
                case 601:
                    // 바벨의 탑 네비게이션 추가 필요
                    break;

                default:
                    // ID에 해당하는 네비게이션이 없으면 타입 기반으로 fallback
                    HandleGuideMissionNavigateByGuideType();
                    break;
            }
        }
        private void HandleGuideMissionNavigateByGuideType()
        {
            switch (specGuideMissionData.guide_mission_type)
            {
                case GuideMissionType.SUMMON_CHARCTER:
                case GuideMissionType.SUMMON_CHARACTER_NORMAL:
                case GuideMissionType.SUMMON_WEAPHON_NORMAL:
                    NavigateToLobbyAndOpenPopup(() => SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget());
                    break;

                case GuideMissionType.CHARACTER_LEVELUP:
                case GuideMissionType.LEVELUP_CHARACTER_TARGET:
                case GuideMissionType.SET_LV_CHARACTER_TARGET:
                case GuideMissionType.SUM_CHARACTER_LEVEL:
                case GuideMissionType.SET_CHARACTER:
                    NavigateToLobbyAndOpenPopup(() => SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget());
                    break;

                case GuideMissionType.PLAY_PVP:
                case GuideMissionType.SET_PVP_DEF_DECK:
                    // PVP 콘텐츠 화면으로 이동하는 코드
                    break;

                case GuideMissionType.CLEAR_TRIAL:
                    // 시련의 탑 콘텐츠 화면으로 이동하는 코드
                    break;

                case GuideMissionType.CLEAR_BABEL:
                    // 바벨의 탑 콘텐츠 화면으로 이동하는 코드
                    break;

                case GuideMissionType.CLICK_ATTENDANCE:
                    // 출석부 팝업을 띄우는 코드
                    break;

                case GuideMissionType.USE_BUILDING:
                case GuideMissionType.UPGRADE_BUILDING:
                case GuideMissionType.INSTALL_BUILDING:
                    var edb = new ElpisDataBridge();
                    NavigateToLobbyAndOpenPopup(async () => await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterPopup>(edb.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter) ));
                    break;

                case GuideMissionType.OPEN_IDLECHEST:
                    // 방치 보상 팝업으로 이동하는 코드
                    break;

                case GuideMissionType.OPEN_CHEST:
                    // 가방 또는 보물상자 팝업으로 이동하는 코드
                    break;

                case GuideMissionType.DIMENSION_CUBE_LEVEL:
                    // 차원큐브 관련 화면으로 이동하는 코드
                    break;

                case GuideMissionType.ENTER_ELPIS:
                    // 엘피스 관련 화면으로 이동하는 코드
                    break;

                case GuideMissionType.CLEAR_STAGE:
                case GuideMissionType.ENTER_STAGE:
                case GuideMissionType.ENTER_CHAPTER:
                    NavigateToStage();
                    break;

                case GuideMissionType.END_DIALOGUE:
                case GuideMissionType.CLEAR_TUTORIAL:
                    // 특별한 이동 동작이 필요 없을 수 있음. 필요 시 추가
                    break;
            }

        }

        private void NavigateToLobbyAndOpenPopup(Action popupAction)
        {
            var currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Lobby")
            {
                popupAction?.Invoke();
            }
            else
            {
                NavigateToLobbyWithPopupAsync(popupAction).Forget();
            }
        }

        private void NavigateToLobbyAndOpenPopupWithFocus(ElpisFacilityType facilityType, Action popupAction)
        {
            var currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Lobby")
            {
                FocusCameraOnFacility(facilityType);
                popupAction?.Invoke();
            }
            else
            {
                NavigateToLobbyWithPopupAndFocusAsync(facilityType, popupAction).Forget();
            }
        }

        private async UniTask NavigateToLobbyWithPopupAsync(Action popupAction)
        {
            pendingPopupAction = popupAction;
            SceneUILayerManager.OnSceneLoadedEvent += OnLobbySceneLoaded;

            var lastStageId = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var stageData = SpecDataManager.Instance.GetStageData(lastStageId);
            var chapterId = stageData?.chapter_id ?? 1;

            InGameManager.Instance.EndInGame();
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextScene("Lobby", chapterId);
        }

        private async UniTask NavigateToLobbyWithPopupAndFocusAsync(ElpisFacilityType facilityType, Action popupAction)
        {
            pendingFacilityType = facilityType;
            pendingPopupAction = popupAction;
            SceneUILayerManager.OnSceneLoadedEvent += OnLobbySceneLoadedWithFocus;

            var lastStageId = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var stageData = SpecDataManager.Instance.GetStageData(lastStageId);
            var chapterId = stageData?.chapter_id ?? 1;

            InGameManager.Instance.EndInGame();
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextScene("Lobby", chapterId);
        }

        private void OnLobbySceneLoaded(string sceneName)
        {
            if (sceneName == "Lobby")
            {
                SceneUILayerManager.OnSceneLoadedEvent -= OnLobbySceneLoaded;
                pendingPopupAction?.Invoke();
                pendingPopupAction = null;
            }
        }

        private void OnLobbySceneLoadedWithFocus(string sceneName)
        {
            if (sceneName == "Lobby")
            {
                SceneUILayerManager.OnSceneLoadedEvent -= OnLobbySceneLoadedWithFocus;

                if (pendingFacilityType.HasValue)
                {
                    FocusCameraOnFacility(pendingFacilityType.Value);
                    pendingFacilityType = null;
                }

                pendingPopupAction?.Invoke();
                pendingPopupAction = null;
            }
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
                        var targetPosition = ui.TargetWorldPosition;
                        var targetZoom = 10.0f;

                        cameraController.ZoomAndMoveAsync(targetPosition, targetZoom, 0.3f).Forget();
                        return;
                    }
                }
            }
        }

        private void NavigateToStage()
        {
            NavigateToStageAsync().Forget();
        }

        private async UniTask NavigateToStageAsync()
        {
            // var guideStageData = SpecDataManager.Instance.GetStageData(specGuideMissionData.sub_key);
            // if (guideStageData == null) return;

            // // 가이드 미션의 목표 스테이지를 타겟으로 설정
            // var targetStageId = specGuideMissionData.sub_key;

            // // CLEAR_STAGE인 경우, 현재 진행 가능한 스테이지로 이동
            // if (specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_STAGE)
            // {
            //     var latestStageId = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
            //     var nextStageData = SpecDataManager.Instance.GetNextStageData(latestStageId);

            //     if (nextStageData != null && nextStageData.chapter_id == guideStageData.chapter_id)
            //     {
            //         targetStageId = nextStageData.stage_id;
            //     }
            //     else
            //     {
            //         // 해당 챕터의 첫 스테이지로 이동
            //         var firstStageData = SpecDataManager.Instance.GetStageData(guideStageData.chapter_id, 1, guideStageData.difficulty_type);
            //         if (firstStageData != null)
            //         {
            //             targetStageId = firstStageData.stage_id;
            //         }
            //     }
            // }

            // LocalDataManager.Instance.SetLastPlayStageId((uint)targetStageId);

            // SceneTransition.Create<SceneTransition_FadeInOut>();
            // await SceneTransition.FadeInAsync();
            // var currentStageData = SpecDataManager.Instance.GetStageData(BattleDataBridge.GetTargetStageId());
            // SceneLoading.GoToNextScene("BattleReady", currentStageData.chapter_id);
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            var currentStageData = SpecDataManager.Instance.GetStageData(BattleDataBridge.GetTargetStageId());
            SceneLoading.GoToNextScene("BattleReady", currentStageData.chapter_id);
        }

        #endregion

    }
}
