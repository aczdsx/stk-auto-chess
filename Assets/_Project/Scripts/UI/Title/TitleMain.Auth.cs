using CookApps.Auth;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CookApps.AutoBattler.Prologue;
using CookApps.BattleSystem;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler
{
    public partial class TitleMain
    {
        private UniTask<bool> _backgroundAuthTask;

        /// <summary>
        /// 백그라운드에서 인증을 미리 시작 (touchToStart 표시와 동시에 호출)
        /// </summary>
        private void StartBackgroundAuth()
        {
            _backgroundAuthTask = AuthenticateAndInitializeAsync();
        }

        /// <summary>
        /// 인증 → 유저 데이터 → 앱 이벤트 → 서버 데이터 → 이벤트 추적 공통 시퀀스
        /// </summary>
        /// <returns>성공 여부</returns>
        private async UniTask<bool> AuthenticateAndInitializeAsync()
        {
            var recentAuthPlatform = LocalDataManager.Instance.GetRecentAuthData();
            if (recentAuthPlatform == null)
                return false;

            var resp = await NetManager.Instance.Auth.AuthenticateAsync(recentAuthPlatform.Platform, recentAuthPlatform.Id);
            if (!resp.IsSuccess)
                return false;

            CADebug.Log($"[Login] Authenticated. Platform: {recentAuthPlatform.Platform}, Uid: {recentAuthPlatform.Id}");

            // 앱 이벤트 Auth 설정
            CAppAuth.SetUID(resp.Data.Uid);

            // 앱이벤트 전송
            AppEventManager.Instance.Login();

            // 서버 데이터 초기화 (Elpis 포함)
            await NetManager.Instance.InitializeAsync();

            // 클라이언트 이벤트 추적 시작
            ClientEventTracker.Instance.StartTracking();

            // 튜토리얼 스킵 상태 적용
            ApplySkipTutorialState();

            return true;
        }

        private async UniTask OnClickTouchStartAsync()
        {
            touchToStart.SetActive(false);

            // 백그라운드 인증 결과 대기 (이미 완료됐으면 즉시 반환)
            if (!await _backgroundAuthTask)
            {
                touchToStart.SetActive(true);
                return;
            }

            await RouteToNextSceneAsync();
        }

        private async UniTask OnClickGuestLoginAsync()
        {
            guestLoginNode.SetActive(false);
            SceneTransition.Create<SceneTransition_FadeInOut>();
            SceneTransition.FadeInAsync().Forget();
            await LoginManager.Instance.LoginGuest();
            if (!await AuthenticateAndInitializeAsync())
            {
                guestLoginNode.SetActive(true);
                return;
            }
            await RouteToNextSceneAsync();
        }

        private UniTask OnClickSkipTutorialAsync()
        {
            var data = ClientConfigData.Get();
            data.SetSkipTutorial(!data.IsSkipTutorial);
            ApplySkipTutorialState();
            return UniTask.CompletedTask;
        }

        private void ApplySkipTutorialState()
        {
            var data = ClientConfigData.Get();
            if (data != null && data.IsSkipTutorial)
                TutorialManager.SetSkipTutorial();

#if UNITY_EDITOR || (!RELEASE && ENABLE_CHEAT)
            testNode.SetActive(true);
#endif

            var label = skipTutorialButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
            {
                var isSkip = data?.IsSkipTutorial ?? false;
                label.text = isSkip ? "Tutorial: OFF" : "Tutorial: ON";
            }
        }

        private async UniTask OnClickInGameTestAsync()
        {
            touchToStart.SetActive(false);
            guestLoginNode.SetActive(false);

            // 로비로 전환
            SceneTransition.Create<SceneTransition_FadeInOut>();
            var task = SceneTransition.FadeInAsync();

            // 게스트 로그인 처리
            await LoginManager.Instance.LoginGuest();

            if (!await AuthenticateAndInitializeAsync())
            {
                Debug.LogError("[Test Mode] Auth/init failed");
                return;
            }

            await task;

            // 테스트 씬으로 전환
            var testConfig = await Addressables.LoadAssetAsync<InGameTestConfig>("Data/InGameTestConfig.asset");
            TutorialManager.SetSkipTutorial();
            var inGameParams = new InGameMainParams(InGameType.TEST, new InGameMainStateTest(), testConfig.StageChapterId);
            SceneLoading.GoToNextScene("InGame_New", inGameParams);
        }

        private async UniTask OnClickNewInGameTestAsync()
        {
            touchToStart.SetActive(false);
            guestLoginNode.SetActive(false);

            // 로비로 전환
            SceneTransition.Create<SceneTransition_FadeInOut>();
            var task = SceneTransition.FadeInAsync();

            // 게스트 로그인 처리
            await LoginManager.Instance.LoginGuest();

            if (!await AuthenticateAndInitializeAsync())
            {
                Debug.LogError("[Test Mode] Auth/init failed");
                return;
            }

            await task;

            // 테스트 씬으로 전환
            var testConfig = await Addressables.LoadAssetAsync<InGameTestConfig>("Data/InGameTestConfig.asset");
            TutorialManager.SetSkipTutorial();
            var inGameParams = new InGameMainParams(InGameType.TEST, new InGameMainStateTest(), testConfig.StageId);
            SceneUILayerManager.Instance.ChangeScene("InGame_New", inGameParams);
        }
    }
}
