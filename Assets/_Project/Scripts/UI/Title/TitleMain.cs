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

namespace CookApps.AutoBattler
{
    public class TitleMain : UILayer
    {
        public static int SessionCount { get; private set; }

        [SerializeField] private GameObject touchToStart;
        [SerializeField] private GameObject guestLoginNode;

        [Header("Addressables Download")]
        [SerializeField] private AssetReference _downloadVideoAssetReference;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
#if !RELEASE && ENABLE_CHEAT
            SRDebug.Init();
#endif
            SessionCount++;

            RunAllTasks().Forget();
        }

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
                    (typeof(ParachuteCurveData), "Data/ParachuteCurveData.asset")
                })
            };

            SceneLoading.OnStartChangeScene += SceneLoadingTask.HandleLoading;

            await UniTask.WhenAll(tasks);

            await ConnectWithServer();

            // Addressables 다운로드 체크 및 진행
            // await CheckAndDownloadAddressablesAsync();

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
            touchToStart.SetActive(false);
            var recentAuthPlatform = LocalDataManager.Instance.GetRecentAuthData();
            var resp = await NetManager.Instance.Auth.AuthenticateAsync(recentAuthPlatform.Platform, recentAuthPlatform.Id);
            if (!resp.IsSuccess)
            {
                touchToStart.SetActive(true);
                return;
            }

            CADebug.Log($"[Login] Authenticated. Platform: {recentAuthPlatform.Platform}, Uid: {recentAuthPlatform.Id}");

            // 유저 로그인 정보 저장
            bool res = await UserDataManager.Instance.Initialize();
            if (!res)
            {
                touchToStart.SetActive(true);
                return;
            }

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
                // 초반 플로우 체크 및 진행
#if _SJHONG_TEST_
                MyDebug.MyLog(SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL), MyDebug.Constants.BLUE);

                LocalDataManager.Instance.SetLastPlayStageId((uint)SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL)?.Last().stage_id);
                MyDebug.MyLog($"{firstStageData.stage_id} < {LocalDataManager.Instance.GetLastPlayStageId()} == {firstStageData.stage_id < LocalDataManager.Instance.GetLastPlayStageId()}");
                if (firstStageData != null && !(firstStageData.chapter_id < LocalDataManager.Instance.GetLastPlayStageId())) {
                    SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                    var firstStage = SpecDataManager.Instance.StageInfo.All[0];
                    var task = NetManager.Instance.Battle.StartAsync(firstStage.chapter_id, firstStage.stage_id, (int)InGameType.STAGE, System.Array.Empty<string>());
                    await SceneTransition.FadeInAsync();
                    var inGameParams = await task;
                    SceneLoading.GoToNextScene("InGame", inGameParams);
                }
                else 
                {
                    SceneTransition.Create<SceneTransition_FadeInOut>();
                    await SceneTransition.FadeInAsync();

                    var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    SceneLoading.GoToNextScene("Lobby", specStageData.chapter_id);

                    return;
                }
#else
                // 초반 플로우 체크 및 진행
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);

                // 1. 첫 스테이지를 못 깬 경우 → 프롤로그 진행
                if (firstStageData != null && ServerDataManager.Instance.Battle.IsStageCleared((uint)firstStageData.stage_id) == false)
                {
                    // SceneLoading.GoToNextScene("InGame",
                    //     (InGameType.STAGE, (IGameStateUICore)new InGameMainStateStage(), lastTutoStageData.stage_id));
                    SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                    await SceneTransition.FadeInAsync();
                    
                    var inGameParams = new InGameMainParams(InGameType.PROLOGUE, new InGameMainStatePrologue(), 0);
                    SceneLoading.GoToNextSceneWithSpecialTrigger("InGame", "PrologueStart", inGameParams);
                    return;
                }
                else
                {
                    SceneTransition.Create<SceneTransition_FadeInOut>();
                    await SceneTransition.FadeInAsync();

                    var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    SceneLoading.GoToNextScene("Lobby", specStageData.chapter_id);

                    return;
                }
#endif
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

        /// <summary>
        /// 닉네임이 없으면 Chapter0_04 나니노벨 재생 후 씬 이동
        /// 닉네임이 있으면 바로 씬 이동
        /// </summary>
        private async UniTask GoToSceneWithNicknameCheckAsync(string sceneName, object param)
        {
            var nickname = ServerDataManager.Instance.PlayerData?.Nickname;

            if (string.IsNullOrEmpty(nickname))
            {
                // 닉네임 없음 → Chapter0_04 나니노벨 재생 후 씬 이동
                SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                await SceneTransition.FadeInAsync();

                await SceneUILayerManager.Instance.PushUILayerAsync<NaninovelMain>(
                    ("Chapter0_04", (System.Action)(() => SceneLoading.GoToNextScene(sceneName, param)))
                );
                return;
            }

            // 닉네임 있음 → 바로 씬 이동
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextScene(sceneName, param);
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

            // 유저 데이터 초기화
            bool res = await UserDataManager.Instance.Initialize();
            if (!res)
            {
                SceneUILayerManager.Instance.PopUILayer(popup);
                Debug.LogError("[Test Mode] UserDataManager init failed");
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

            // 유저 데이터 초기화
            bool res = await UserDataManager.Instance.Initialize();
            if (!res)
            {
                SceneUILayerManager.Instance.PopUILayer(popup);
                Debug.LogError("[Go To Lobby] UserDataManager init failed");
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

        /// <summary>
        /// Addressables 다운로드 사이즈 체크 및 다운로드 진행
        /// </summary>
        private async UniTask CheckAndDownloadAddressablesAsync()
        {
            // 카탈로그 업데이트 체크
            var catalogsToUpdate = await Addressables.CheckForCatalogUpdates().ToUniTask();
            if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
            {
                await Addressables.UpdateCatalogs(catalogsToUpdate).ToUniTask();
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
        /// 다운로드가 필요한 모든 키 수집
        /// </summary>
        private async UniTask<List<(object Key, long Size)>> GetAllDownloadKeysAsync()
        {
            var result = new List<(object Key, long Size)>();
            var checkedKeys = new HashSet<string>();

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    // 중복 체크
                    string keyStr = key.ToString();
                    if (checkedKeys.Contains(keyStr))
                        continue;
                    checkedKeys.Add(keyStr);

                    // 다운로드 사이즈 체크
                    var sizeHandle = Addressables.GetDownloadSizeAsync(key);
                    long size = await sizeHandle.ToUniTask();

                    if (size > 0)
                    {
                        result.Add((key, size));
                    }
                }
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