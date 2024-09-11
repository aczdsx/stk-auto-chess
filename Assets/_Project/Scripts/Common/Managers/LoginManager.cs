using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CookApps.PlatformAuth;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Google;
using Tech.Hive.V1;


namespace CookApps.AutoBattler
{
    public class LoginManager : Singleton<LoginManager>
    {
        public bool CheckIsLoggedIn()
        {
            if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Guest) ||
                GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Apple) ||
                GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Facebook) ||
                GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Google))
                return true;

            return false;
        }

        #region Guest

        public async UniTask LoginGuest()
        {
            if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Guest))
                return;

            var authId = await GrpcManager.Instance.Lobby.GenerateUuidAsync();
            if (string.IsNullOrEmpty(authId))
            {
                CADebug.LogError("말도 안되는 authId가 빈 문자열이 되는 상황 발생!!!");
                return;
            }

            await GrpcManager.Instance.Auth.CreateAsync(AuthPlatform.Guest, authId);
        }

        #endregion

        private bool isLoginProcessComplete = false;
        private string cachedUserId;

        #region Apple

        public async UniTask<bool> LoginApple(bool needAutoLogin = true)
        {
            if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Apple))
                return false;

            isLoginProcessComplete = false;
            CookAppsPlatformAuth.AppleSignin(SignInResultApple);
            while (!isLoginProcessComplete)
                await UniTask.Yield();

            if (cachedUserId == null)
            {
                isLoginProcessComplete = false;
                return false;
            }

            var isSuccess = await GrpcManager.Instance.Auth.CreateAsync(AuthPlatform.Apple, cachedUserId);
            if (isSuccess)
            {
                isLoginProcessComplete = false;
                cachedUserId = null;
                return true;
            }

            // var popupData = new PopupAlertData("Popup_Social_Login_Check_Title", "Popup_Social_Login_Check_Desc", false, "Popup_Social_Login_Check_Button_Yes", "Common_Cancel", true);
            // (_, var res) = await SceneUIManager.Instance.RequestPushUIAsync("Popup_Alert", popupData);
            // if (res is PopupAlertResult.Ok)
            // {
            //     LogOut();
            //     await UniversalGrpcManager.Instance.AddAuthInfoAsync(AuthPlatform.Apple, cachedUserId);
            //     isLoginProcessComplete = false;
            //     cachedUserId = null;
            //     return true;
            // }

            return false;
        }

        private void SignInResultApple(EnumSignResult result, PlatformUserInfo platformUserInfo)
        {
            // 로그인 성공
            if (result == EnumSignResult.SUCCESS)
            {
                cachedUserId = platformUserInfo.UserId;
                var socialPlatform = platformUserInfo.SocialPlatform;
            }

            isLoginProcessComplete = true;
        }

        public void LogOut(bool withUniversal = true)
        {
            if (withUniversal)
                GrpcManager.Instance.Auth.Logout();
        }

        #endregion

        #region Facebook

        public async UniTask<bool> LoginFacebook()
        {
            if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Facebook))
                return false;

            isLoginProcessComplete = false;
            CookAppsPlatformAuth.FacebookSignin(SignInResultFacebook);
            while (!isLoginProcessComplete)
                await UniTask.Yield();

            if (cachedUserId == null)
            {
                isLoginProcessComplete = false;
                return false;
            }

            var isSuccess = await GrpcManager.Instance.Auth.CreateAsync(AuthPlatform.Facebook, cachedUserId);
            if (isSuccess)
            {
                isLoginProcessComplete = false;
                cachedUserId = null;
                return false;
            }

            // var popupData = new PopupAlertData("Popup_Social_Login_Check_Title", "Popup_Social_Login_Check_Desc", false, "Popup_Social_Login_Check_Button_Yes", "Common_Cancel", true);
            // (_, var res) = await SceneUIManager.Instance.RequestPushUIAsync("Popup_Alert", popupData);
            // if (res is PopupAlertResult.Ok)
            // {
            //     LogOut();
            //     await UniversalGrpcManager.Instance.AddAuthInfoAsync(AuthPlatform.Facebook, cachedUserId);
            //     isLoginProcessComplete = false;
            //     cachedUserId = null;
            //     return true;
            // }

            return false;
        }

        private void SignInResultFacebook(EnumSignResult result, PlatformUserInfo platformUserInfo)
        {
            if (result == EnumSignResult.SUCCESS)
            {
                cachedUserId = platformUserInfo.UserId;
                var socialPlatform = platformUserInfo.SocialPlatform;
            }

            isLoginProcessComplete = true;
        }

        #endregion

        #region Google

        public async UniTask<bool> LoginGoogle()
        {
            if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Google))
                return false;

            isLoginProcessComplete = false;
            CookAppsPlatformAuth.GoogleSignIn(SignInResult);
            while (!isLoginProcessComplete)
                await UniTask.Yield();

            if (cachedUserId == null)
            {
                isLoginProcessComplete = false;
                return false;
            }

            var isSuccess = await GrpcManager.Instance.Auth.CreateAsync(AuthPlatform.Google, cachedUserId);
            if (isSuccess)
            {
                isLoginProcessComplete = false;
                cachedUserId = null;
                return false;
            }

            // var popupData = new PopupAlertData("Popup_Social_Login_Check_Title", "Popup_Social_Login_Check_Desc", false, "Popup_Social_Login_Check_Button_Yes", "Common_Cancel", true);
            // (_, var res) = await SceneUIManager.Instance.RequestPushUIAsync("Popup_Alert", popupData);
            // if (res is PopupAlertResult.Ok)
            // {
            //     LogOut();
            //     await UniversalGrpcManager.Instance.AddAuthInfoAsync(AuthPlatform.Google, cachedUserId);
            //     isLoginProcessComplete = false;
            //     cachedUserId = null;
            //     return true;
            // }

            return false;
        }

        private void SignInResult(EnumSignResult result, PlatformUserInfo platformUserInfo)
        {
            // 로그인 성공
            if (result == EnumSignResult.SUCCESS)
            {
                cachedUserId = platformUserInfo.UserId;
                var socialPlatform = platformUserInfo.SocialPlatform;
            }

            isLoginProcessComplete = true;
        }

        #endregion


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