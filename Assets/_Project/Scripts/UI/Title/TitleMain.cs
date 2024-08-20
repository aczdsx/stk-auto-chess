using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.Auth;
using CookApps.gRPC.Universal;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Universal.V2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Overlay, "Prefabs/UI/Title/TitleMain.prefab")]
    public class TitleMain : UILayer
    {
        public static int SessionCount { get; private set; }

        private Dictionary<int, float> progressDict = new();

        [SerializeField] private GameObject touchToStart;
        
        [SerializeField] private GameObject _createGuestButtonLayer;
        [SerializeField] private GameObject _loginGuestButtonLayer;
        [SerializeField] private GameObject _loadingPopupObject;
        
        bool isLogin = false;
        bool isLoginProcess = false;
        
        [SerializeField] private TMP_InputField _guestIDInputField;
        [SerializeField] private TextMeshProUGUI _currentGuestIDText;
        
        private bool haveGuestID = false;
        
        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            SessionCount++;
            progressDict.Clear();
            touchToStart.SetActive(false);
            
            RunAllTasks().Forget();

            Debug.Log("TitleMain OnPreEnter --> " + gameObject.name);

            var playerID = Preference.LoadPreference(Pref.GUEST_ID, "");
            haveGuestID = string.IsNullOrEmpty(playerID) == false;
            
            _createGuestButtonLayer.SetActive(!haveGuestID);
            //_loginGuestButtonLayer.SetActive(haveGuestID);
            
            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_splash_001);
        }

        private async UniTask RunAllTasks()
        {
            await UniTask.NextFrame();
            // get list of all ITitleTask implementations in this assembly
            Type[] types = InheritHelper.GetAllImplementations<ITitleTask>();
            var allTasks = new List<ITitleTask>();

            foreach (Type type in types)
            {
                var titleTask = Activator.CreateInstance(type) as ITitleTask;
                if (titleTask == null)
                {
                    continue;
                }

                allTasks.Add(titleTask);
                progressDict.Add(titleTask.GetHashCode(), 0);
            }

            // run tasks in order
            for (var priority = ITitleTaskPriority.Step_0; priority < ITitleTaskPriority.MAX; priority++)
            {
                ITitleTaskPriority p = priority;
                ITitleTask[] tasks = allTasks.Where(x => x.Priority == p).ToArray();
                if (tasks.Length == 0)
                {
                    continue;
                }

                await RunSubTasks(tasks);

                touchToStart.SetActive(haveGuestID);
            }
        }

        private async UniTask RunSubTasks(ITitleTask[] tasks)
        {
            foreach (ITitleTask task in tasks)
            {
                task.Initialize(this, UpdateProgress);
            }

            await UniTask.WhenAll(
                tasks.Select(x => x.RunTask())
            );

            foreach (ITitleTask task in tasks)
            {
                (bool, string) error = task.HasError();
                if (error.Item1)
                {
                    await task.HandleError();
                }
            }
        }

        private void UpdateProgress(int hashCode, float progress)
        {
            progressDict[hashCode] = progress;
            float totalProgress = progressDict.Values.Sum() / progressDict.Count;
            // TODO: update progress bar
        }

        public async void OnClickTouchToStart()
        {
            if (isLogin == false) return;
            
            // 앱이벤트 Init
            InitCookAppsAuth();
                
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
                    int lastStageID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    if (UserDataManager.Instance.IsClearStage(lastStageID))
                    {
                        specStageData = SpecDataManager.Instance.GetNextStageData(lastStageID);
                    }

                    isLoginProcess = false;
                    
                    SceneLoading.GoToNextScene("InGame",
                        (InGameType.STAGE, (IGameStateUI) new InGameMainStateUIStageUI(), (int)specStageData.stage_id)).Forget();
                }
                else
                {
                    // 현재 PVP 유저 프로필 자동저장
                    await PVPManager.Instance.UpdatePVPProfileData();
                    
                    var transition = SceneTransition_FadeInOut.Create();
                    
                    isLoginProcess = false;
                    
                    int lastChapterID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastChapterID);
                    SceneLoading.GoToNextScene("Lobby", (int) specStageData.chapter_id, transition).Forget();
                }

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_splash);

                // 세션 타임 기록 unitask 실행
                await RecordSessionTime();
                
                // int lastChapter = 1;
                // SceneLoading.GoToNextScene("Lobby", lastChapter, transition).Forget();
            }
        }

        public void OnClickGuestLoginButton()
        {
            if (isLoginProcess)
                return;

            _loadingPopupObject.SetActive(true);
            isLoginProcess = true;
            LoginGuest().ContinueWith(() =>
            {
                //isLoginProcess = false;
                _loadingPopupObject.SetActive(false);
            }).Forget();
        }

        public void OnClickCrateGuestIDButton()
        {
            if (isLoginProcess)
                return;

            _loadingPopupObject.SetActive(true);
            isLoginProcess = true;
            CreateGuestAccount().ContinueWith(() =>
            {
                //isLoginProcess = false;
                _loadingPopupObject.SetActive(false);
            }).Forget();
        }
        
        public async UniTask LoginGuest()
        {
            // 처음 로그인 인지 체크
            // if (UserDataManager.Instance.IsHaveLoginData() == false)
            // {
            //     Debug.LogError("게스트 아이디를 생성해주세요.");
            //     return;
            // }
            
            if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Guest))
            {
                isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            else
            {
                while (!UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Guest))
                {
                    await UniTask.Yield();
                }

                isLogin =  await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            
            Debug.Log("UID ++++> " + UniversalGrpcManager.Instance.Uid);
            
            var resp = await UniversalGrpcManager.Instance.SelectServerAndPlayerAsync(UserDataManager.Instance.UserBasicData.ServerId, UserDataManager.Instance.UserBasicData.PlayerId);
            if (resp.IsError)
            {
                //FinishWithServerError();
                return;
            }
            
            // 벤 유저 체크
            if (resp.CommonResponseData.StatusCode == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToast("BANNED_USER_ALERT");
                
                return;
            }
            
            // 로그인 진행
            OnClickTouchToStart();
        }
        
        public async UniTask CreateGuestAccount()
        {
            // if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Guest))
            //     return;
        
            if (string.IsNullOrWhiteSpace(_guestIDInputField.text))
            {
                Debug.LogError("게스트 아이디가 공백임");
                return;
            }
            
            var authId = await UniversalGrpcManager.Instance.GenerateUuidAsync();
            if(string.IsNullOrEmpty(authId))
            {
                CADebug.LogError("말도 안되는 authId가 빈 문자열이 되는 상황 발생!!!");
                return;
            }
            await UniversalGrpcManager.Instance.AddAuthInfoAsync(AuthPlatform.Guest, authId);
            
            // 아래를 호출하지 않으면 에러 발생
            if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Guest))
            {
                isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            else
            {
                while (!UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Guest))
                {
                    await UniTask.Yield();
                }
            
                isLogin =  await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            
            // 플레이어 데이터 생성
            var newPlayerResponse = await UniversalGrpcManager.Instance.CreatePlayerAsync(1, _guestIDInputField.text);
            if (newPlayerResponse.IsError)
            {
                //FinishWithServerError();
                return;
            }
            
            Debug.Log("PlayID ++++> " + newPlayerResponse.PlayerId);
            
            var resp = await UniversalGrpcManager.Instance.SelectServerAndPlayerAsync(1, newPlayerResponse.PlayerId);
            if (resp.IsError)
            {
                //FinishWithServerError();
                return;
            }
            
            // 벤 유저 체크
            if (resp.CommonResponseData.StatusCode == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToast("BANNED_USER_ALERT");
                
                return;
            }
            
            Debug.Log("UID ++++> " + UniversalGrpcManager.Instance.Uid);
            
            // 유저 로그인 정보 저장
            var commonLoginData = UniversalGrpcManager.Instance.GetCommonRequestParam();
            var gameLoginData = UniversalGrpcManager.Instance.GetGameRequestParam();
            UserDataManager.Instance.SetUserLoginData(commonLoginData.Uid, gameLoginData.ServerId, gameLoginData.PlayerId, _guestIDInputField.text);
            
            // 로그인 진행
            OnClickTouchToStart();
        }

        // 유저 세션 타임 기록
        private async UniTask RecordSessionTime()
        {
            var specEventData = SpecDataManager.Instance.GetSpecEventData(EventType.ACC_PLAY_TIME);
            if (specEventData == null) return;

            while (true)
            {
                // var userDataValue = UserDataManager.Instance.GetUserEventData(specEventData.event_id).ActionCount;
                // Debug.Log("***********************RecordSessionTime Test***********************   =>  " + userDataValue);

                await UniTask.Delay(1000 * 60);

                UserDataManager.Instance.SetUserEventActionCount(specEventData.event_id, 1, true, true);

                // 앱이벤트용 플레이 타임 데이터 기록
                UserDataManager.Instance.SetUserTotalPlayTime(1, true);
            }
        }

        private void InitCookAppsAuth()
        {
#if SERVER_REAL
        CAppAuth.SetServer(EnumServer.PRODUCTION);
#else
            CAppAuth.SetServer(EnumServer.DEV);
#endif
            
            CAppAuth.SetUID(UniversalGrpcManager.Instance.Uid);
        }
    }
}
