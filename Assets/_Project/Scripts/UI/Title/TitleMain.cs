using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CookApps.Auth;
using CookApps.BattleSystem;
using CookApps.Build;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using CookApps.AutoBattler.Prologue;
using CookApps.TeamBattle.Utility;

namespace CookApps.AutoBattler
{
    public class TitleMain : UILayer
    {
        public static int SessionCount { get; private set; }

        [SerializeField] private GameObject touchToStart;
        [SerializeField] private GameObject guestLoginNode;

        [SerializeField] private GameObject testNode;

        [Header("Addressables Download")]
        [SerializeField] private AssetReference _downloadVideoAssetReference;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
#if UNITY_EDITOR || (!RELEASE && ENABLE_CHEAT)
            SRDebug.Init();
            testNode.SetActive(true);
#else
            testNode.SetActive(false);
#endif
            SessionCount++;

            RunAllTasks().Forget();
        }

        protected override void OnBackButton(ref bool offPrevUI) { }

        private async UniTask RunAllTasks()
        {
            await UniTask.NextFrame();

            var tasks = new[]
            {
                SceneTransition.FadeOutAsync(),
                LanguageManager.Instance.InitializeAsync(),
                SpriteManager.Instance.Initialize("Data/SpriteManager.asset"),
                ConnectAppsflyer(),
                SoDataProvider.Instance.LoadSoDataBatch(new () {
                    (typeof(VignetteSO), "Data/VignetteData.asset"),
                    (typeof(Item_Chapter_SO), "Data/UIElementData/Item_Chapter_SO.asset"),
                    (typeof(ColorDataScriptableObject), "Data/ColorData.asset"),
                    (typeof(AudioConfigSO), "Data/AudioConfig.asset"),
                    (typeof(TargetLineConfig), "Data/TargetLineConfig.asset")
                })
            };

            SceneLoading.OnStartChangeScene += SceneLoadingTask.HandleLoading;

            await UniTask.WhenAll(tasks);

            // 뱃지 시스템 초기화
            BadgeManager.Instance.Initialize();

            await ConnectWithServer();

            // Addressables 다운로드 체크 및 진행
            await CheckAndDownloadAddressablesAsync();

            _ = InGameTouchManager.Instance;
            _ = TutorialManager.Instance;
            NaninovelTriggerManager.Instance.Initialize();
            InitTitleMain();
        }

        public void OnClickTouchToStart()
        {
            OnClickTouchStartAsync().Forget();
        }

        private async UniTask OnClickTouchStartAsync()
        {
            var recentAuthPlatform = LocalDataManager.Instance.GetRecentAuthData();
            if (recentAuthPlatform == null)
                return;

            touchToStart.SetActive(false);
            var resp = await NetManager.Instance.Auth.AuthenticateAsync(recentAuthPlatform.Platform, recentAuthPlatform.Id);
            if (!resp.IsSuccess)
            {
                touchToStart.SetActive(true);
                return;
            }

            CADebug.Log($"[Login] Authenticated. Platform: {recentAuthPlatform.Platform}, Uid: {recentAuthPlatform.Id}");


            // 앱 이벤트 Auth 설정
            CAppAuth.SetUID(resp.Data.Uid);

            // 앱이벤트 전송
            AppEventManager.Instance.Login();

            // 서버 데이터 초기화 (Elpis 포함)
            await NetManager.Instance.InitializeAsync();

            // 클라이언트 이벤트 추적 시작
            ClientEventTracker.Instance.StartTracking();

            // var transition1 = SceneTransition_FadeInOut.Create();
            // 프롤로그로 진입하게 해줘야함
            // SceneLoading.GoT`oNextScene("InGame",
            //         (InGameType.PROLOGUE, (IGameStateUICore)new InGameMainStatePrologue(), 0));
            // return;

            // [TODO] lastChapter에 로비에 진입할 챕터 넣어주세요.

            // 튜토리얼(1챕터) 진입 분기 로직
            var firstStageData = SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL)?[0];
            if (ServerDataManager.Instance.Battle.CurrentChapterId <= 1)
            {
                // 튜토리얼(1챕터) 진입 분기 로직
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);

                // 1. 첫 스테이지를 못 깬 경우 → 프롤로그 진행
                if (firstStageData != null && ServerDataManager.Instance.Battle.IsStageCleared((uint)firstStageData.stage_id) == false)
                {
                    SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                    await SceneTransition.FadeInAsync();

                    var inGameParams = new InGameMainParams(InGameType.PROLOGUE, new InGameMainStatePrologue(), 0);
                    SceneLoading.GoToNextSceneWithSpecialTrigger("InGame", "PrologueStart", inGameParams);
                    return;
                }

                // 2. 1챕터 마지막 스테이지를 못 깬 경우 → 못 깬 첫 스테이지로 바로 진입
                if (lastTutoStageData != null && ServerDataManager.Instance.Battle.IsStageCleared((uint)lastTutoStageData.stage_id) == false)
                {
                    // 1챕터에서 못 깬 첫 스테이지 찾기
                    var stageList = SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL);
                    StageInfo unclearedStage = null;
                    foreach (var stage in stageList)
                    {
                        if (ServerDataManager.Instance.Battle.IsStageCleared((uint)stage.stage_id) == false)
                        {
                            unclearedStage = stage;
                            break;
                        }
                    }

                    if (unclearedStage != null)
                    {
                        SceneTransition.Create<SceneTransition_FadeInOut>();
                        await SceneTransition.FadeInAsync();

                        var inGameParams = new InGameMainParams(InGameType.STAGE, new InGameMainStateStage(), unclearedStage.stage_id);
                        var progressData = ClientProgressData.Get();
                        if (unclearedStage.stage_id == 10003 && !progressData.hasNicknameSet)
                        {
                            SceneLoading.GoToNextSceneViaNaninovelScript("InGame", "Chapter0_04", inGameParams);
                        }
                        else
                        {
                            SceneLoading.GoToNextScene("InGame", inGameParams);
                        }
                        return;
                    }
                }
            }

            // 3. 1챕터 모두 클리어 → 로비로 진입
            {
                SceneTransition.Create<SceneTransition_FadeInOut>();
                await SceneTransition.FadeInAsync();

                var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
                var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                SceneLoading.GoToNextScene("Lobby", specStageData.chapter_id);

                return;
            }
        }

        public void OnClickGuestLoginButton()
        {
            GuestLoginAsync().Forget();
        }

        private async UniTask GuestLoginAsync()
        {
            guestLoginNode.SetActive(false);
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();
            await LoginManager.Instance.LoginGuest();
            SceneUILayerManager.Instance.PopUILayer(popup);
            OnClickTouchStartAsync().Forget();
        }

        private async UniTask ConnectAppsflyer()
        {
            await AppsFlyerManager.Instance.InitializeAsync(
                devKey: "rpPWfG9Nbc8v2Rk7fYm783",
                iosAppId: "6504894635",
                oneLinkId: "", // 미정
                debug: false
            );

            // 초기화 완료 대기
            await UniTask.NextFrame();
        }

        private async UniTask ConnectWithServer()
        {
            var matches = Regex.Matches(BuildInfo.GetVersionCode(), @"\d+");
            var res = ZString.Join("", matches.Select(x => x.Value));
            if (!int.TryParse(res, out var versionCode)) versionCode = 1000;

            NetManager.Instance.Startup();

            // 버전 체크
            var checkVersionResponse = await NetManager.Instance.Lobby.CheckVersionAsync();
            if (!checkVersionResponse.IsSuccess)
            {
                // 버전 체크 실패시 처리
                var toastString = LanguageManager.Instance.GetDefaultText("SERVER_ACCESS_FAIL");
                SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>(toastString).Forget();
                return;
            }
            // TODO: 업데이트 필요하면 업데이트 팝업 띄우기
            // checkVersionResponse.Data.UpdateStatus = CheckVersionUpdateStatus.UpdateStatusForce

            await SpecDataManager.Instance.Initialize(checkVersionResponse.Data.SpecVersion);
            GlobalEffectCodeManager.Instance.Initialize(); // userdatamanager.initialize보다 먼저 호출되어야함
        }

        private void InitTitleMain()
        {
            touchToStart.GetComponent<CAButton>().DefaultClickSoundType = DefaultClickSoundType.Splash;

            if (LocalDataManager.Instance.GetRecentAuthData() == null)
            {
                // 게스트 로그인 only
                guestLoginNode.SetActive(true);
                touchToStart.SetActive(false);
            }
            else
            {
                guestLoginNode.SetActive(false);
                touchToStart.SetActive(true);
            }

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_splash01);
        }

        /// <summary>
        /// 테스트 모드 버튼 클릭 (Title UI에 버튼 추가 후 연결)
        /// </summary>
        public void OnClickTestModeButton()
        {
            GoToTestModeAsync().Forget();
        }

        private async UniTask GoToTestModeAsync()
        {
            touchToStart.SetActive(false);
            guestLoginNode.SetActive(false);

            // 게스트 로그인 처리
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();
            await LoginManager.Instance.LoginGuest();

            var recentAuthPlatform = LocalDataManager.Instance.GetRecentAuthData();
            var resp = await NetManager.Instance.Auth.AuthenticateAsync(recentAuthPlatform.Platform, recentAuthPlatform.Id);
            if (!resp.IsSuccess)
            {
                SceneUILayerManager.Instance.PopUILayer(popup);
                Debug.LogError("[Test Mode] Auth failed");
                return;
            }

            // 서버 데이터 가져오기
            await UniTask.WhenAll(
                NetManager.Instance.CustomLobby.GetMyPlayerDataAsync(),
                NetManager.Instance.Inventory.ListAsync(),
                NetManager.Instance.Character.ListAsync(),
                NetManager.Instance.Initialize_Elpis()
            );

            SceneUILayerManager.Instance.PopUILayer(popup);
            // 테스트 씬으로 전환
            SceneTransition.Create<SceneTransition_FadeInOut>();

            var testConfig = await Addressables.LoadAssetAsync<InGameTestConfig>("Data/InGameTestConfig.asset");
            await SceneTransition.FadeInAsync();
            var inGameParams = new InGameMainParams(InGameType.TEST, new InGameMainStateTest(), testConfig.StageChapterId);
            SceneLoading.GoToNextScene("InGame", inGameParams);
        }

        /// <summary>
        /// 로비 직행 버튼 클릭 (Title UI에 버튼 추가 후 연결)
        /// </summary>
        public void OnClickGoToLobbyButton()
        {
            GoToLobbyAsync().Forget();
        }

        /// <summary>
        /// 튜토리얼 스킵 버튼
        /// </summary>
        public void OnClickSkipTutorial()
        {
            TutorialManager.SetSkipTutorial();
            GoToLobbyAsync().Forget();
        }

        private async UniTask GoToLobbyAsync()
        {
            touchToStart.SetActive(false);
            guestLoginNode.SetActive(false);

            // 게스트 로그인 처리
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();
            await LoginManager.Instance.LoginGuest();

            var recentAuthPlatform = LocalDataManager.Instance.GetRecentAuthData();
            var resp = await NetManager.Instance.Auth.AuthenticateAsync(recentAuthPlatform.Platform, recentAuthPlatform.Id);
            if (!resp.IsSuccess)
            {
                SceneUILayerManager.Instance.PopUILayer(popup);
                Debug.LogError("[Go To Lobby] Auth failed");
                return;
            }

            // 앱 이벤트 Auth 설정
            CAppAuth.SetUID(resp.Data.Uid);

            // 서버 데이터 초기화
            await NetManager.Instance.InitializeAsync();

            // 클라이언트 이벤트 추적 시작
            ClientEventTracker.Instance.StartTracking();

            SceneUILayerManager.Instance.PopUILayer(popup);

            // 로비로 전환
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();

            // 마지막 플레이 스테이지 기준 챕터로 이동 (없으면 1챕터)
            var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
            var chapterId = specStageData?.chapter_id ?? 1;
            SceneLoading.GoToNextScene("Lobby", chapterId);
        }

        #region Addressables Download

        // 테스트 모드: true로 설정하면 가라 데이터로 다운로드 UI 테스트
        private const bool TestDownloadMode = false;
        private const long TestDownloadSize = 150 * 1024 * 1024; // 150MB 가라 데이터
        private const float TestDownloadDuration = 30f; // 5초 동안 다운로드 시뮬레이션

        /// <summary>
        /// Addressables 다운로드 사이즈 체크 및 다운로드 진행
        /// </summary>
        private async UniTask CheckAndDownloadAddressablesAsync()
        {
            if (TestDownloadMode)
            {
                await TestDownloadAsync();
                return;
            }

            // 카탈로그 업데이트 체크
            var catalogsToUpdate = await Addressables.CheckForCatalogUpdates().WaitUntilDone();
            if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
            {
                await Addressables.UpdateCatalogs(catalogsToUpdate).WaitUntilDone();
            }

            // 전체 다운로드 사이즈 체크
            var downloadKeys = await GetAllDownloadKeysAsync();
            if (downloadKeys.Count == 0)
            {
                CADebug.Log("[TitleMain] No addressables to download");
                return;
            }

            long totalDownloadSize = 0;
            for (int i = 0; i < downloadKeys.Count; i++)
            {
                totalDownloadSize += downloadKeys[i].Size;
            }

            CADebug.Log($"[TitleMain] Total download size: {DownloadConfirmPopup.FormatFileSize(totalDownloadSize)} ({downloadKeys.Count} keys)");

            // 다운로드 확인 팝업 표시
            bool userConfirmed = await ShowDownloadConfirmPopupAsync(totalDownloadSize);
            if (!userConfirmed)
            {
                CADebug.Log("[TitleMain] User cancelled download");
                return;
            }

            // 다운로드 진행
            await DownloadAddressablesAsync(downloadKeys, totalDownloadSize);
        }

        /// <summary>
        /// 가라 데이터로 다운로드 UI 테스트
        /// </summary>
        private async UniTask TestDownloadAsync()
        {
            CADebug.Log($"[TitleMain] TEST MODE - 가라 다운로드 시작: {DownloadConfirmPopup.FormatFileSize(TestDownloadSize)}");

            // 다운로드 확인 팝업 표시
            bool userConfirmed = await ShowDownloadConfirmPopupAsync(TestDownloadSize);
            if (!userConfirmed)
            {
                CADebug.Log("[TitleMain] TEST MODE - User cancelled download");
                return;
            }

            // 가라 다운로드 진행
            var tcs = new UniTaskCompletionSource<bool>();
            bool isCancelled = false;

            var popupData = new DownloadProgressPopupData(
                videoAssetReference: _downloadVideoAssetReference,
                totalDownloadSizeBytes: TestDownloadSize,
                onComplete: () => tcs.TrySetResult(true),
                onCancel: () =>
                {
                    isCancelled = true;
                    tcs.TrySetResult(false);
                }
            );

            var progressPopup = await SceneUILayerManager.Instance.PushUILayerAsync<DownloadProgressPopup>(popupData);

            // 가라 다운로드 시뮬레이션
            float elapsed = 0f;
            while (elapsed < TestDownloadDuration && !isCancelled)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / TestDownloadDuration);
                long downloadedBytes = (long)(TestDownloadSize * progress);

                progressPopup.UpdateProgress(progress, downloadedBytes);
                await UniTask.Yield();
            }

            if (!isCancelled)
            {
                progressPopup.UpdateProgress(1f, TestDownloadSize);
                await UniTask.Delay(500);
                progressPopup.OnDownloadComplete();
            }

            await tcs.Task;
            CADebug.Log("[TitleMain] TEST MODE - 가라 다운로드 완료");
        }

        private const string PreloadLabel = "preload";

        /// <summary>
        /// preload 라벨이 붙은 에셋의 다운로드 키 수집
        /// </summary>
        private async UniTask<List<(object Key, long Size)>> GetAllDownloadKeysAsync()
        {
            var result = new List<(object Key, long Size)>();

            // preload 라벨의 다운로드 사이즈 체크
            var sizeHandle = Addressables.GetDownloadSizeAsync(PreloadLabel);
            long size = await sizeHandle.WaitUntilDone();

            if (size > 0)
            {
                result.Add((PreloadLabel, size));
            }

            return result;
        }

        /// <summary>
        /// 다운로드 확인 팝업 표시
        /// </summary>
        private async UniTask<bool> ShowDownloadConfirmPopupAsync(long downloadSize)
        {
            var tcs = new UniTaskCompletionSource<bool>();

            var popupData = new DownloadConfirmPopupData(
                downloadSizeBytes: downloadSize,
                onConfirm: () => tcs.TrySetResult(true),
                onCancel: () => tcs.TrySetResult(false)
            );

            await SceneUILayerManager.Instance.PushUILayerAsync<DownloadConfirmPopup>(popupData);

            return await tcs.Task;
        }

        /// <summary>
        /// Addressables 다운로드 실행
        /// </summary>
        private async UniTask DownloadAddressablesAsync(List<(object Key, long Size)> downloadKeys, long totalDownloadSize)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            bool isCancelled = false;

            var popupData = new DownloadProgressPopupData(
                videoAssetReference: _downloadVideoAssetReference,
                totalDownloadSizeBytes: totalDownloadSize,
                onComplete: () => tcs.TrySetResult(true),
                onCancel: () =>
                {
                    isCancelled = true;
                    tcs.TrySetResult(false);
                }
            );

            var progressPopup = await SceneUILayerManager.Instance.PushUILayerAsync<DownloadProgressPopup>(popupData);

            // 각 키별로 다운로드
            long downloadedBytes = 0;
            for (int i = 0; i < downloadKeys.Count; i++)
            {
                if (isCancelled)
                    break;

                var (key, expectedSize) = downloadKeys[i];
                var downloadHandle = Addressables.DownloadDependenciesAsync(key, false);

                // 다운로드 진행률 업데이트
                while (!downloadHandle.IsDone && !isCancelled)
                {
                    var status = downloadHandle.GetDownloadStatus();
                    long currentDownloaded = downloadedBytes + status.DownloadedBytes;
                    float progress = (float)currentDownloaded / totalDownloadSize;

                    progressPopup.UpdateProgress(progress, currentDownloaded);

                    await UniTask.Yield();
                }

                if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    downloadedBytes += expectedSize;
                }
                else if (!isCancelled)
                {
                    CADebug.LogError($"[TitleMain] Failed to download: {key}");
                    progressPopup.OnDownloadFailed($"Failed to download: {key}");
                    Addressables.Release(downloadHandle);
                    return;
                }

                Addressables.Release(downloadHandle);
            }

            if (!isCancelled)
            {
                progressPopup.UpdateProgress(1f, totalDownloadSize);
                await UniTask.Delay(500);
                progressPopup.OnDownloadComplete();
            }

            await tcs.Task;
        }

        #endregion
    }
}