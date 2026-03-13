using System.Linq;
using System.Text.RegularExpressions;
using CookApps.BattleSystem;
using CookApps.Build;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CookApps.TeamBattle.Utility;
using R3;

namespace CookApps.AutoBattler
{
    public partial class TitleMain : UILayer
    {
        public static int SessionCount { get; private set; }

        [SerializeField] private GameObject touchToStart;
        [SerializeField] private GameObject guestLoginNode;

        [SerializeField] private GameObject testNode;

        [Header("Addressables Download")]
        [SerializeField] private AssetReference downloadVideoAssetReference;

        [SerializeField] private CAButton startButton;
        [SerializeField] private CAButton skipTutorialButton;
        [SerializeField] private CAButton guestLoginButton;
        [SerializeField] private CAButton inGameTestButton;
        [SerializeField] private CAButton newInGameTestButton;

        protected override void Awake()
        {
            base.Awake();
            guestLoginButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickGuestLoginAsync()).AddTo(this);
            startButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickTouchStartAsync()).AddTo(this);
            skipTutorialButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickSkipTutorialAsync()).AddTo(this);
            inGameTestButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickInGameTestAsync()).AddTo(this);
            newInGameTestButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickNewInGameTestAsync()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
#if UNITY_EDITOR || (!RELEASE && ENABLE_CHEAT)
            SRDebug.Init();
#endif
            testNode.SetActive(false);
            SessionCount++;

            RunAllTasks().Forget();
        }

        protected override void OnBackButton(ref bool offPrevUI) { }

        private async UniTask RunAllTasks()
        {
            touchToStart.SetActive(false);
            guestLoginNode.SetActive(false);
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
                    (typeof(TargetLineConfig), "Data/TargetLineConfig.asset"),
                    (typeof(AutoChess.View.CombatVfxConfigSO), "Data/CombatVfxConfig.asset")
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
                StartBackgroundAuth();
            }

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_splash01);
        }
    }
}
