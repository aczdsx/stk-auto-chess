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

namespace CookApps.AutoBattler
{
    public class TitleMain : UILayer
    {
        public static int SessionCount { get; private set; }

        [SerializeField] private GameObject touchToStart;
        [SerializeField] private GameObject guestLoginNode;

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

            // var transition1 = SceneTransition_FadeInOut.Create();
            // 프롤로그로 진입하게 해줘야함
            // SceneLoading.GoT`oNextScene("InGame",
            //         (InGameType.PROLOGUE, (IGameStateUICore)new InGameMainStatePrologue(), 0));
            // return;


            {
                // [TODO] lastChapter에 로비에 진입할 챕터 넣어주세요.  

                // 초반 플로우 체크 및 진행
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);
                // if (false)
                if (ServerDataManager.Instance.Battle.IsStageCleared((uint)lastTutoStageData.stage_id) == false)
                {
                    // SceneLoading.GoToNextScene("InGame",
                    //     (InGameType.STAGE, (IGameStateUICore)new InGameMainStateStage(), lastTutoStageData.stage_id));
                    SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                    await SceneTransition.FadeInAsync();

                //     var inGameParams = new InGameMainParams(
                //         InGameType.PROLOGUE,
                //         new InGameMainStatePrologue(),
                //         0);

                //     SceneLoading.GoToNextSceneWithSpecialTrigger("InGame", "PrologueStart", inGameParams);
                //     return;
                // }
                // else
                {
                    SceneTransition.Create<SceneTransition_FadeInOut>();
                    await SceneTransition.FadeInAsync();

                    var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    SceneLoading.GoToNextScene("Lobby", specStageData.chapter_id);

                    return;
                }

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_splash);

                // 세션 타임 기록 unitask 실행
                RecordSessionTime().Forget();
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

        // 유저 세션 타임 기록
        private static async UniTask RecordSessionTime()
        {
            // TODO: @twhan TimeSystem 따위의 시간 관련 시스템이 완성되면 수정 필요
            var specEventData = SpecDataManager.Instance.GetSpecEventData(EventType.ACC_PLAY_TIME);
            if (specEventData == null)
                return;

            while (true)
            {
                //var userDataValue = UserDataManager.Instance.GetUserEventData(specEventData.event_id).ActionCount;
                //Debug.Log("***********************RecordSessionTime Test***********************   =>  " + userDataValue);

                await UniTask.Delay(1000 * 60);

                UserDataManager.Instance.SetUserEventActionCount(specEventData.event_id, 1, true, true);

                // 앱이벤트용 플레이 타임 데이터 기록
                UserDataManager.Instance.SetUserTotalPlayTime(1, true);
            }
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

            var testConfig = await Addressables.LoadAssetAsync<InGameTestConfig>("TestConfig/InGameTestConfig.asset");
            await SceneTransition.FadeInAsync();
            var inGameParams = new InGameMainParams(InGameType.TEST, new InGameMainStateTest(), testConfig.StageChapterId);
            SceneLoading.GoToNextScene("InGame", inGameParams);
        }
    }
}