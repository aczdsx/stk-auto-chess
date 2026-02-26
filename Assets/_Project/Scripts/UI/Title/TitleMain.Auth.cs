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

        private async UniTask OnClickSkipTutorialAsync()
        {
            TutorialManager.SetSkipTutorial();

            touchToStart.SetActive(false);
            guestLoginNode.SetActive(false);

            // 로비로 전환
            SceneTransition.Create<SceneTransition_FadeInOut>();
            var task = SceneTransition.FadeInAsync();

            // 게스트 로그인 처리
            await LoginManager.Instance.LoginGuest();

            if (!await AuthenticateAndInitializeAsync())
            {
                Debug.LogError("[Go To Lobby] Auth/init failed");
                return;
            }

            await task;

            // 마지막 플레이 스테이지 기준 챕터로 이동 (없으면 1챕터)
            var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
            var chapterId = specStageData?.chapter_id ?? 1;
            SceneLoading.GoToNextScene("Lobby", chapterId);
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
            var inGameParams = new InGameMainParams(InGameType.TEST, new InGameMainStateTest(), testConfig.StageChapterId);
            SceneLoading.GoToNextScene("InGame", inGameParams);
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
            var inGameParams = new InGameMainParams(InGameType.TEST, new InGameMainStateTest(), testConfig.StageId);
            SceneUILayerManager.Instance.ChangeScene("InGame_New", inGameParams);
        }
    }
}
