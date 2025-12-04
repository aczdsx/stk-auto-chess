/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SettingScene : MonoBehaviour
    {
        [SerializeField] private Text _infoText;
        [SerializeField] private Text _infoUser;
        [SerializeField] private GameObject _goInGameScene;
        [SerializeField] private GameObject _goTitleScene;

        private void OnEnable()
        {
            OnClickPlatformList();
        }

        /// 현재 계정의 연동된 플랫폼 목록 조회
        public async void OnClickPlatformList()
        {
            try
            {
                var netManager = NetManager.Instance;
                ListResponse listResponse = await netManager.Auth.ListAsync();
                listResponse.ThrowIfError();
                UpdateUserInfo(listResponse.ItemList);
                SetInfoText($"플랫폼 목록 조회 성공 : {listResponse.ItemList.Count}개");
            }
            catch (Exception e)
            {
                SetInfoText($"Error: {e.Message}");
            }
        }

        /// 특정 플랫폼으로 인증 및 계정 연동(묶기)
        private async void OnClickSignPlatform(AuthPlatform platform)
        {
            try
            {
                var netManager = NetManager.Instance;
                string authId = await AuthPlatformUtil.GetAuthId(platform);
                CreateResponse createResponse = await netManager.Auth.CreateAsync(platform, authId);
                createResponse.ThrowIfError();
                SetInfoText($"계정 연동 성공 {platform}");
                OnClickPlatformList(); // 연동된 플랫폼 목록 갱신
            }
            catch (Exception e)
            {
                SetInfoText($"Error: {e.Message}");
            }
        }

        public void OnClickGpgs()
        {
            OnClickSignPlatform(AuthPlatform.Gpgs);
        }
        public void OnClickFacebook()
        {
            OnClickSignPlatform(AuthPlatform.Facebook);
        }

        public void OnClickGoogle()
        {
            OnClickSignPlatform(AuthPlatform.Google);
        }

        // 계정 즉시 삭제
        public async void OnClickDelete()
        {
            try
            {
                var netManager = NetManager.Instance;
                AuthDeleteResponse authDeleteResponse = await netManager.Auth.DeleteAsync();
                authDeleteResponse.ThrowIfError();
                SetInfoText("계정 삭제 성공");
                _goTitleScene.SetActive(true);
                gameObject.SetActive(false);
                // 계정 삭제 이후 타이틀 씬으로 이동 시 네트워크 매니저 종료
                // NetManager.Instance.Shutdown();
            }
            catch (Exception e)
            {
                SetInfoText($"Error: {e.Message}");
            }
        }

        public void OnClickInGameScene()
        {
            _goInGameScene.SetActive(true);
            gameObject.SetActive(false);
        }

        private void UpdateUserInfo(IEnumerable<AuthData> authDataList)
        {
            var sb = new StringBuilder();
            foreach (var authData in authDataList)
            {
                sb.AppendLine($"Platform: {authData.Platform}, AuthId: {authData.AuthId}");
            }
            if (_infoUser != null)
            {
                _infoUser.text = sb.ToString();
            }
        }

        private void SetInfoText(string message)
        {
            if (_infoText != null)
            {
                _infoText.text = message;
            }
        }
    }
}
