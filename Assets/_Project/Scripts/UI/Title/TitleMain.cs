using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CookApps.Auth;
using CookApps.Build;
using CookApps.gRPC;
using CookApps.PlatformAuth;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Overlay, "Prefabs/UI/Title/TitleMain.prefab")]
    public class TitleMain : UILayer
    {
        const float LOGIN_DELAY_TIME = 3.0f;
        
        public static int SessionCount { get; private set; }

        private Dictionary<int, float> progressDict = new();

        [SerializeField] private GameObject touchToStart;

        [SerializeField] private GameObject _createGuestButtonLayer;
        [SerializeField] private GameObject _loginGuestButtonLayer;
        [SerializeField] private GameObject _appleLoginButtonLayer;
        [SerializeField] private GameObject _googleLoginButtonLayer;
        [SerializeField] private GameObject _facebookLoginButtonLayer;

        [SerializeField] private GameObject _loadingPopupObject;
        [SerializeField] private ToastSystemPopup _toastPopupObject;

        private bool isLogin = false;
        private bool isLoginProcess = false;

        private bool loginDelay = true;

        [SerializeField] private TMP_InputField _guestIDInputField;
        [SerializeField] private TextMeshProUGUI _currentGuestIDText;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
        }

        private void Start()
        {
#if !RELEASE && ENABLE_CHEAT
            SRDebug.Init();
#endif
            
            isLoginProcess = false;

            ClearTitleMain();
            progressDict.Clear();

            RunAllTasks().Forget();

            Invoke(nameof(LoginDelay), LOGIN_DELAY_TIME);
        }

        private void InitTitleMain()
        {
            SessionCount++;

            Debug.Log("TitleMain OnPreEnter --> " + gameObject.name);

            isLogin = LoginManager.Instance.CheckIsLoggedIn();

            //touchToStart.SetActive(isLogin);
            touchToStart.SetActive(true);   // 게스트 로그인 only
            
// 24.9.27 - 게스트 로그인 only
// #if UNITY_ANDROID
//             // if (_appleLoginButtonLayer != null) _appleLoginButtonLayer?.SetActive(!isLogin);
//             if (_appleLoginButtonLayer != null) _appleLoginButtonLayer?.SetActive(!isLogin); // TEST
//             if (_googleLoginButtonLayer != null) _googleLoginButtonLayer?.SetActive(!isLogin);
//             if (_facebookLoginButtonLayer != null) _facebookLoginButtonLayer?.SetActive(!isLogin);
//             if (_loginGuestButtonLayer != null) _loginGuestButtonLayer?.SetActive(!isLogin);
// #elif UNITY_IOS
//             if (_appleLoginButtonLayer != null) _appleLoginButtonLayer?.SetActive(!isLogin);
//             if (_googleLoginButtonLayer != null) _googleLoginButtonLayer?.SetActive(!isLogin);
//             if (_facebookLoginButtonLayer != null) _facebookLoginButtonLayer?.SetActive(!isLogin);
//             if (_loginGuestButtonLayer != null) _loginGuestButtonLayer?.SetActive(!isLogin);
// #else
//             if (_appleLoginButtonLayer != null) _appleLoginButtonLayer?.SetActive(!isLogin);
//             if (_googleLoginButtonLayer != null) _googleLoginButtonLayer?.SetActive(!isLogin);
//             if (_facebookLoginButtonLayer != null) _facebookLoginButtonLayer?.SetActive(!isLogin);
//             if (_loginGuestButtonLayer != null) _loginGuestButtonLayer?.SetActive(!isLogin);
// #endif

            // 언어 설정
            LanguageManager.Instance.InitLanguage();

            Invoke(nameof(LoginDelay), 3.0f);

            //_createGuestButtonLayer.SetActive(!haveGuestID);
            //_loginGuestButtonLayer.SetActive(haveGuestID);

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_splash_001);
        }

        private void LoginDelay()
        {
            loginDelay = false;
        }

        private async UniTask RunAllTasks()
        {
            await UniTask.NextFrame();
            
            await AtlasManager.Instance.Initialize("Data/AtlasManager.asset");
            
            SceneLoading.OnStartChangeScene += AtlasManager.Instance.OnStartChangeScene;
            SceneLoading.OnStartChangeScene += SceneLoadingTask.HandleLoading;
            
            await ConnectWithServer();
            
            InitTitleMain();
        }

        public async void OnClickTouchToStart()
        {
            if (isLogin == false) return;

            // 없으면 첫번째 서버에 플레이어를 생성
            var userNickName = BMUtil.GenerateRandomId(10);
            var playerId = await GetServerPlayerId(userNickName);
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }
            // 닉네임 중복 체크
            // if (newPlayerResponse.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_FAIL_NICKNAME_ALREADY_EXIST)
            // {
            //     var toastStirng = LanguageManager.Instance.GetLanguageText("SERVER_ALREADY_USE_NICKNAME");
            //     _toastPopupObject.SetToastSystemPopupByManual(toastStirng, 2.0f);
            //     isLoginProcess = false;
            //     return;
            // }

            // 서버 에러 체크
            // if (newPlayerResponse.IsError)
            // {
            //     //FinishWithServerError();
            //     var toastStirng = LanguageManager.Instance.GetLanguageText("SERVER_ACCESS_FAIL");
            //     _toastPopupObject.SetToastSystemPopupByManual(toastStirng, 2.0f);
            //     isLoginProcess = false;
            //     return;
            // }

            // Debug.Log("PlayID ++++> " + newPlayerResponse.PlayerId);

            // var resp = await GrpcManager.Instance.Server.JoinAsync(1, newPlayerResponse.PlayerId);
            // if (resp.IsError)
            // {
            //     //FinishWithServerError();
            //     var toastStirng = LanguageManager.Instance.GetLanguageText("SERVER_ACCESS_FAIL");
            //     _toastPopupObject.SetToastSystemPopupByManual(toastStirng, 2.0f);
            //     isLoginProcess = false;
            //     return;
            // }

            // 벤 유저 체크
            // if (resp.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            // {
            //     var toastStirng = LanguageManager.Instance.GetLanguageText("BANNED_USER_ALERT");
            //     _toastPopupObject.SetToastSystemPopupByManual(toastStirng, 2.0f);
            //     isLoginProcess = false;
            //     return;
            // }

            Debug.Log("UID ++++> " + GrpcManager.Instance.Auth.AuthenticateData.Uid);
            
            // 유저 로그인 정보 저장
            bool res = await UserDataManager.Instance.Initialize();
            if (!res)
            {
                // TODO:
            }
            // var commonLoginData = UniversalGrpcManager.Instance.GetCommonRequestParam();
            // var gameLoginData = UniversalGrpcManager.Instance.GetGameRequestParam();
            UserDataManager.Instance.SetUserLoginData(GrpcManager.Instance.Auth.AuthenticateData.Uid, 1, playerId);

            // 앱이벤트 Init
            //InitCookAppsAuth();

            // 앱 이벤트 Auth 설정
            CAppAuth.SetUID(GrpcManager.Instance.Auth.AuthenticateData.Uid);

#if SERVER_REAL
            CAppAuth.SetServer(EnumServer.PRODUCTION);
#else
            CAppAuth.SetServer(EnumServer.DEV);
#endif
            // 앱이벤트 전송
            AppEventManager.Instance.Login();

            {
                //var transition = SceneTransition_FadeInOut.Create();
                // [TODO] lastChapter에 로비에 진입할 챕터 넣어주세요.

                // 현재 PVP 유저 프로필 자동저장
                await PVPManager.Instance.UpdatePVPProfileData();

                // 초반 플로우 체크 및 진행
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);
                if (UserDataManager.Instance.IsClearStage(lastTutoStageData.stage_id) == false)
                {
                    var lastStageID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    if (UserDataManager.Instance.IsClearStage(lastStageID)) specStageData = SpecDataManager.Instance.GetNextStageData(lastStageID);

                    isLoginProcess = false;

                    SceneLoading.GoToNextScene("InGame",
                        (InGameType.STAGE, (IGameStateUI)new InGameMainStateUIStageUI(), (int)specStageData.stage_id)).Forget();
                }
                else
                {
                    var transition = SceneTransition_FadeInOut.Create();

                    isLoginProcess = false;

                    var lastChapterID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastChapterID);
                    SceneLoading.GoToNextScene("Lobby", (int)specStageData.chapter_id, transition).Forget();
                }

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_splash);

                // 세션 타임 기록 unitask 실행
                await RecordSessionTime();
            }
        }

        public void OnClickCommonLoginButton()
        {
            if (isLoginProcess) return;
            if (loginDelay) return;
            if (isLogin == false) return;

            isLoginProcess = true;
            //SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();
            _loadingPopupObject.SetActive(true);
            

            LoginPlatform(LoginManager.Instance.CurrentAuthPlatform).ContinueWith(() =>
            {
                //SceneUILayerManager.Instance.PopUILayer("LoadingPopup");
                _loadingPopupObject.SetActive(false);
                //isLoginProcess = false;
            }).Forget();
        }

        public void OnClickGuestLoginButton()
        {
            if (isLoginProcess) return;
            if (loginDelay) return;

            if (isLogin)
            {
                // SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();

                isLoginProcess = true;
                _loadingPopupObject.SetActive(true);
                
                LoginPlatform(AuthPlatform.Guest).ContinueWith(() =>
                {
                    // SceneUILayerManager.Instance.PopUILayer("LoadingPopup");
                    _loadingPopupObject.SetActive(false);
                    //isLoginProcess = false;
                }).Forget();
            }
            else
            {
                // SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();

                isLoginProcess = true;
                _loadingPopupObject.SetActive(true);
                
                CreateNewAccount(AuthPlatform.Guest).ContinueWith(() =>
                {
                    // SceneUILayerManager.Instance.PopUILayer("LoadingPopup");
                    _loadingPopupObject.SetActive(false);
                    //isLoginProcess = false;
                    
                }).Forget();
            }
        }

        public void OnClickCreateGuestIDButton()
        {
            if (isLoginProcess) return;
            if (loginDelay) return;

            _loadingPopupObject.SetActive(true);
            isLoginProcess = true;
            CreateNewAccount(AuthPlatform.Guest).ContinueWith(() =>
            {
                _loadingPopupObject.SetActive(false);
            }).Forget();
        }

        public void OnClickAppleLoginButton()
        {
            if (isLoginProcess) return;
            if (loginDelay) return;

            ProcessAppleLogin();
        }

        public async void ProcessAppleLogin()
        {
            isLoginProcess = true;
            //SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>();
            _loadingPopupObject.SetActive(true);

            var result = await LoginManager.Instance.LoginApple();
            if (result)
            {
                await CreateNewAccount(AuthPlatform.Apple);
                //SceneUILayerManager.Instance.PopUILayer("LoadingPopup");
                _loadingPopupObject.SetActive(false);
                //isLoginProcess = false;
            }
            else
            {
                await LoginPlatform(AuthPlatform.Apple);
                //SceneUILayerManager.Instance.PopUILayer("LoadingPopup");
                _loadingPopupObject.SetActive(false);
                //isLoginProcess = false;
            }
        }

        public async UniTask LoginPlatform(AuthPlatform authPlatform)
        {
            if (GrpcManager.Instance.Auth.IsLoggedIn(authPlatform))
            {
                isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            }
            else
            {
                while (!GrpcManager.Instance.Auth.IsLoggedIn(authPlatform)) await UniTask.Yield();
            
                isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            }
            //
            // Debug.Log("UID ++++> " + GrpcManager.Instance.Auth.AuthenticateData.Uid);
            //
            // var resp = await GrpcManager.Instance.Server.JoinAsync((uint)UserDataManager.Instance.UserBasicData.ServerId,
            //     UserDataManager.Instance.UserBasicData.PlayerId);
            // if (resp.IsError)
            // {
            //     //FinishWithServerError();
            //     var toastStirng = LanguageManager.Instance.GetLanguageText("SERVER_ACCESS_FAIL");
            //     _toastPopupObject.SetToastSystemPopupByManual(toastStirng, 2.0f);
            //     isLoginProcess = false;
            //     return;
            // }
            //
            // // 벤 유저 체크
            // if (resp.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            // {
            //     var toastStirng = LanguageManager.Instance.GetLanguageText("BANNED_USER_ALERT");
            //     _toastPopupObject.SetToastSystemPopupByManual(toastStirng, 2.0f);
            //     isLoginProcess = false;
            //     return;
            // }

            // 로그인 진행
            OnClickTouchToStart();
        }

        public async UniTask CreateNewAccount(AuthPlatform authPlatform)
        {
            if (authPlatform == AuthPlatform.Guest)
            {
                // 디바이스 ID로 authID 저장
                var uuID = await GrpcManager.Instance.Lobby.GenerateUuidAsync();     // 앱 삭제 시 초기화 ver
                //var uuID = DeviceIdHolder.DeviceId; // 앱 삭제에도 유지 ver
                await GrpcManager.Instance.Auth.CreateAsync(authPlatform, uuID);
            }

            isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            
            // 로그인 진행
            OnClickTouchToStart();
        }
        
        public async UniTask ConnectWithServer()
        {
            var matches = Regex.Matches(BuildInfo.GetVersionCode(), @"\d+");
            var res = ZString.Join("", matches.Select(x => x.Value));
            if (!int.TryParse(res, out var versionCode)) versionCode = 1000;
            
#if SERVER_REAL
            var initializeParam = new GrpcInitializeParam(
                "stkauto.prod.cookappsgames.com",
                port: 443,
                ChannelCredentials.SecureSSL,
                versionCode,
#else
            var initializeParam = new GrpcInitializeParam(
                "stkauto-gyc71v.dev.cookappsgames.com",
                443,
                ChannelCredentials.SecureSSL,
                versionCode,
            
            // 재상님 로컬
            // var initializeParam = new GrpcInitializeParam(
            //     "192.168.2.65",
            //     50051,
            //     ChannelCredentials.Insecure,
            //     versionCode,
#endif

#if UNITY_IOS
                store:StoreMap.AppStore,
#else
                StoreMap.GooglePlay,
#endif
                GrpcExceptionHandler.HandleSuccess,
                GrpcExceptionHandler.HandleServerException,
                GrpcExceptionHandler.HandleGrpcException,
                true
            );

            GrpcManager.Instance.StartUp(initializeParam);

            // 버전 체크
            var checkVersionResponse = await GrpcManager.Instance.Lobby.CheckVersionAsync();
            if (checkVersionResponse.IsError)
            {
                // TODO: 버전 체크 실패시 처리
            }
            // TODO: 업데이트 필요하면 업데이트 팝업 띄우기
            // checkVersionResponse.Data.UpdateStatus = CheckVersionUpdateStatus.UpdateStatusForce

            await SpecDataManager.Instance.Initialize(checkVersionResponse.Data.SpecVersion);
            GlobalEffectCodeManager.Instance.Initialize(); // userdatamanager.initialize보다 먼저 호출되어야함
        }

        /*
         * todo : grpc 상황에 맞게 처리하세요
         * 1. 서버 리스트를 받아온다.
         * 2. 서버 리스트에 플레이어 정보가 있는지 확인한다.
         * 3. (플레이어 정보가 있으면 선택, 없으면 서버에 플레이어를 생성한다.)
         * 4. 서버에 플레이어를 조인한다.
         */
        // Server Player 처리
        private async UniTask<string> GetServerPlayerId(string nickName)
        {
            var getLastPlayerResponse = await GrpcManager.Instance.Player.GetLastSelectedAsync();
            if (!getLastPlayerResponse.IsError)
            {
                var playerId = getLastPlayerResponse.Item?.PlayerId;
                if (!string.IsNullOrEmpty(playerId))
                    return playerId;
            }

            // 서버 리스트를 받아온다.
            var serverListResponse = await GrpcManager.Instance.Server.ListAsync();
            if (serverListResponse.IsError) 
                return string.Empty;
            
            // 서버의 유저 정보에서 첫번째 선택 ( 서버에 플레이어가 여러명일 경우 처리 방법은 다를 수 있음 )
            UserServerData userServerData = serverListResponse.Data.UserServerList.FirstOrDefault();
            // 서버에서 유저 정보가 있으면 해당 서버의 플레이어 ServerJoin 이후 Id 반환
            if (userServerData != null)
            {
                return await ServerJoin(userServerData.ServerId, userServerData.PlayerId);   
            }
            
            // IsJoinable 가능한 첫번째 서버 ( 서버 선택 UI를 통해 선택하게 할 수도 있음 )
            var firstServer = serverListResponse.Data.ServerList.FirstOrDefault(x => x.IsJoinable);
            if (firstServer == null)
            {   
                // 서버가 없음 서버팀에 문의 해 주세요!!!
                return string.Empty;
            }
            
            uint selectedServerId = firstServer.ServerId;
            PlayerCreateResponse createResponse = await GrpcManager.Instance.Player.CreateAsync(firstServer.ServerId, nickName);
            if(createResponse.IsError) 
                return string.Empty;
            return await ServerJoin(selectedServerId, createResponse.PlayerId);

            //----------------------------------------------------------------------
            async UniTask<string> ServerJoin(uint serverId, string playerId)
            {
                ServerJoinResponse joinResponse = await GrpcManager.Instance.Server.JoinAsync(serverId, playerId);
                return joinResponse.IsError ? string.Empty : playerId;
            }
        }

        // 유저 세션 타임 기록
        private async UniTask RecordSessionTime()
        {
            var specEventData = SpecDataManager.Instance.GetSpecEventData(EventType.ACC_PLAY_TIME);
            if (specEventData == null) return;

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

        private void ClearTitleMain()
        {
            if (_appleLoginButtonLayer != null) _appleLoginButtonLayer?.SetActive(false);
            if (_googleLoginButtonLayer != null) _googleLoginButtonLayer?.SetActive(false);
            if (_facebookLoginButtonLayer != null) _facebookLoginButtonLayer?.SetActive(false);
            if (_loginGuestButtonLayer != null) _loginGuestButtonLayer?.SetActive(false);
        }
    }
}