using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CookApps.Auth;
using CookApps.Build;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppsFlyerSDK;
using R3;
using CookApps.BattleSystem;

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

            await SceneTransition.FadeOutAsync();

            var tasks = new[] {
                SpriteManager.Instance.Initialize("Data/SpriteManager.asset"),
                ConnectAppsflyer(),
                ConnectWithServer(),
                SoDataProvider.Instance.LoadSoDataBatch(new () {
                    (typeof(VignetteSO), "Data/VignetteData.asset"),
                    (typeof(Item_Chapter_SO), "Data/UIElementData/Item_Chapter_SO.asset"),
                    (typeof(ColorDataScriptableObject), "Data/ColorData.asset"),
                    (typeof(ParachuteCurveData), "Data/ParachuteCurveData.asset")
                })
            };

            SceneLoading.OnStartChangeScene += SceneLoadingTask.HandleLoading;

            await UniTask.WhenAll(tasks);

            _ = InGameTouchManager.Instance;
            _ = TutorialManager.Instance;

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

            NetManager.Instance.Initialize_Elpis().Forget();

            // var transition1 = SceneTransition_FadeInOut.Create();
            // 프롤로그로 진입하게 해줘야함
            // SceneLoading.GoToNextScene("InGame",
            //         (InGameType.PROLOGUE, (IGameStateUICore)new InGameMainStatePrologue(), 0));
            // return;


            // SceneTransition.Create<SceneTransition_FadeInOut>();
            // SceneTransition.FadeInAsync().Forget();
            // SceneLoading.GoToNextScene("InGame",
            //         (InGameType.PROLOGUE, (IGameStateUICore)new InGameMainStatePrologue(), 0), naninovelScriptName: "Scripts/0-1");
            // return;

            {
                // [TODO] lastChapter에 로비에 진입할 챕터 넣어주세요.  

                // 초반 플로우 체크 및 진행
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);
                if (UserDataManager.Instance.IsClearStage(lastTutoStageData.stage_id) == false)
                {
                    var lastStageID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    if (UserDataManager.Instance.IsClearStage(lastStageID)) specStageData = SpecDataManager.Instance.GetNextStageData(lastStageID);

                    SceneLoading.GoToNextScene("InGame",
                        (InGameType.STAGE, (IGameStateUICore)new InGameMainStateStage(), specStageData.stage_id));
                }
                else
                {
                    SceneTransition.Create<SceneTransition_FadeInOut>();
                    SceneTransition.FadeInAsync().Forget();

                    var lastChapterID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastChapterID);
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
                var toastString = LanguageManager.Instance.GetLanguageText("SERVER_ACCESS_FAIL");
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

            // 언어 설정
            LanguageManager.Instance.InitLanguage();

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_splash_001);
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
    }
}