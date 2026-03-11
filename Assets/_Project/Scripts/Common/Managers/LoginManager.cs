using System.Threading.Tasks;
using CookApps.PlatformAuth;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Google;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public class LoginManager : Singleton<LoginManager>
    {
        #region Guest

        public async UniTask LoginGuest()
        {
            if (LocalDataManager.Instance.HasAuthPlatform(AuthPlatform.Guest))
                return;

            while (true)
            {
                var id = System.Guid.NewGuid();
                var authId = id.ToString("N");
                var resp = await NetManager.Instance.Auth.AuthenticateAsync(AuthPlatform.Guest, authId);
                if (resp.IsSuccess)
                {
                    LocalDataManager.Instance.AddAuth(AuthPlatform.Guest, authId);
                    break;
                }

                await UniTask.Delay(1000);
            }
        }

        #endregion

        #region Apple
        public async UniTask<string> LoginApple()
        {
            if (LocalDataManager.Instance.HasAuthPlatform(AuthPlatform.Apple))
                return LocalDataManager.Instance.GetAuthId(AuthPlatform.Apple);

            var isLoginProcessComplete = false;
            string cachedUserId = null;
            CookAppsPlatformAuth.AppleSignin((EnumSignResult result, PlatformUserInfo platformUserInfo) =>
            {
                // 로그인 성공
                if (result == EnumSignResult.SUCCESS)
                {
                    cachedUserId = platformUserInfo.UserId;
                }

                isLoginProcessComplete = true;
            });

            while (!isLoginProcessComplete)
                await UniTask.Yield();

            if (cachedUserId == null)
                return null;

            var resp = await NetManager.Instance.Auth.BindAsync(AuthPlatform.Apple, cachedUserId);
            if (resp.Exception != null)
            {
                return null;
            }

            LocalDataManager.Instance.AddAuth(AuthPlatform.Apple, cachedUserId);
            return cachedUserId;
        }

        public void LogOut(bool withUniversal = true)
        {
            if (withUniversal)
                NetManager.Instance.Auth.UnregisterAsync();
        }

        #endregion
        //
        // #region Facebook
        //
        // public async UniTask<bool> LoginFacebook()
        // {
        //     if (NetManager.Instance.Auth.IsLoggedIn(AuthPlatform.Facebook))
        //         return false;
        //
        //     isLoginProcessComplete = false;
        //     CookAppsPlatformAuth.FacebookSignin(SignInResultFacebook);
        //     while (!isLoginProcessComplete)
        //         await UniTask.Yield();
        //
        //     if (cachedUserId == null)
        //     {
        //         isLoginProcessComplete = false;
        //         return false;
        //     }
        //
        //     var resp = await NetManager.Instance.Auth.CreateAsync(AuthPlatform.Facebook, cachedUserId);
        //     if (resp.IsSuccess)
        //     {
        //         isLoginProcessComplete = false;
        //         cachedUserId = null;
        //         return true;
        //     }
        //
        //     // var popupData = new PopupAlertData("Popup_Social_Login_Check_Title", "Popup_Social_Login_Check_Desc", false, "Popup_Social_Login_Check_Button_Yes", "Common_Cancel", true);
        //     // (_, var res) = await SceneUIManager.Instance.RequestPushUIAsync("Popup_Alert", popupData);
        //     // if (res is PopupAlertResult.Ok)
        //     // {
        //     //     LogOut();
        //     //     await UniversalNetManager.Instance.AddAuthInfoAsync(AuthPlatform.Facebook, cachedUserId);
        //     //     isLoginProcessComplete = false;
        //     //     cachedUserId = null;
        //     //     return true;
        //     // }
        //
        //     return false;
        // }
        //
        // private void SignInResultFacebook(EnumSignResult result, PlatformUserInfo platformUserInfo)
        // {
        //     if (result == EnumSignResult.SUCCESS)
        //     {
        //         cachedUserId = platformUserInfo.UserId;
        //         var socialPlatform = platformUserInfo.SocialPlatform;
        //     }
        //
        //     isLoginProcessComplete = true;
        // }
        //
        // #endregion
        //
        // #region Google
        //
        // public async UniTask<bool> LoginGoogle()
        // {
        //     if (NetManager.Instance.Auth.IsLoggedIn(AuthPlatform.Google))
        //         return false;
        //
        //     isLoginProcessComplete = false;
        //     CookAppsPlatformAuth.GoogleSignIn(SignInResult);
        //     while (!isLoginProcessComplete)
        //         await UniTask.Yield();
        //
        //     if (cachedUserId == null)
        //     {
        //         isLoginProcessComplete = false;
        //         return false;
        //     }
        //
        //     var resp = await NetManager.Instance.Auth.CreateAsync(AuthPlatform.Google, cachedUserId);
        //     if (resp.IsSuccess)
        //     {
        //         isLoginProcessComplete = false;
        //         cachedUserId = null;
        //         return true;
        //     }
        //
        //     // var popupData = new PopupAlertData("Popup_Social_Login_Check_Title", "Popup_Social_Login_Check_Desc", false, "Popup_Social_Login_Check_Button_Yes", "Common_Cancel", true);
        //     // (_, var res) = await SceneUIManager.Instance.RequestPushUIAsync("Popup_Alert", popupData);
        //     // if (res is PopupAlertResult.Ok)
        //     // {
        //     //     LogOut();
        //     //     await UniversalNetManager.Instance.AddAuthInfoAsync(AuthPlatform.Google, cachedUserId);
        //     //     isLoginProcessComplete = false;
        //     //     cachedUserId = null;
        //     //     return true;
        //     // }
        //
        //     return false;
        // }
        //
        // private void SignInResult(EnumSignResult result, PlatformUserInfo platformUserInfo)
        // {
        //     // 로그인 성공
        //     if (result == EnumSignResult.SUCCESS)
        //     {
        //         cachedUserId = platformUserInfo.UserId;
        //         var socialPlatform = platformUserInfo.SocialPlatform;
        //     }
        //
        //     isLoginProcessComplete = true;
        // }
        //
        // #endregion


        #region private

        private const string WebClientId = "889084882690-hs9lufm82e5ks3kpcvbi8376m85hr7aq.apps.googleusercontent.com";

        private GoogleSignInConfiguration _configuration;

        private bool _isWaitDispatcher;

        private bool _isSignInResult;

        private string _userID;

        private string _errorLog;

        private void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using var enumerator = task.Exception.InnerExceptions.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    _isSignInResult = false;
                    _userID = string.Empty;
                    var error = (GoogleSignIn.SignInException)enumerator.Current;
                    _errorLog = $"GOT ERROR : {(null != error ? error.Status : string.Empty)} / {(null != error ? error.Message : string.Empty)}";
                }
                else
                {
                    _isSignInResult = false;
                    _userID = string.Empty;
                    _errorLog = $"GOT Exception : {task.Exception}";
                }
            }
            else if (task.IsCanceled)
            {
                _isSignInResult = false;
                _userID = string.Empty;
                _errorLog = "GOT Canceled : task.IsCanceled";
            }
            else
            {
                _isSignInResult = true;
                _userID = task.Result.UserId;
                _errorLog = string.Empty;
            }
        }

        #endregion
    }
}