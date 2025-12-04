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
    /// 인게임 씬
    public class InGameScene : MonoBehaviour
    {
        [SerializeField] private Text _infoText;
        [SerializeField] private Text _infoUser;
        [SerializeField] private GameObject _goSetting;

        private UnityEngine.Coroutine _coroutineCheckVersion;

        private void OnEnable()
        {
            UpdateUserInfo();
            // 인게임중에는 버전 체크 코루틴을 실행합니다.
            _coroutineCheckVersion = StartCoroutine(InGameVersionChecker.CoroutineLoop());
        }

        private void OnDisable()
        {
            if (_coroutineCheckVersion == null)
                return;

            // 인게임이 아닌 상태에서는 버전 체크 코루틴을 중지합니다.
            StopCoroutine(_coroutineCheckVersion);
            _coroutineCheckVersion = null;
        }

        /// 레벨업
        public void OnClickLevelUp()
        {
            var userData  = UserData.Instance;
            userData.LevelData.Level += 1;
            UpdateUserInfo();
        }

        /// 경험치 10 증가
        public void OnClickExpUp()
        {
            var userData  = UserData.Instance;
            userData.ExpData.Exp += 10;
            UpdateUserInfo();
        }

        /// 서버에 유저 데이터 동기화
        public void OnClickSyncToServer()
        {
            SyncToServerUserData();
        }

        /// 설정창 열기
        public void OnClickSetting()
        {
            _goSetting.SetActive(true);
            gameObject.SetActive(false);
        }

        private async void SyncToServerUserData()
        {
            try
            {
                SetInfoText("Upload user data...");
                var userData  = UserData.Instance;
                var netManager = NetManager.Instance;
                IReadOnlyDictionary<string, string> syncData = userData.GetSyncData();
                PlayerDataSetResponse response = await netManager.PlayerData.SetAsync(syncData);
                response.ThrowIfError();
                SetInfoText("User data uploaded.");
                UpdateUserInfo();
            }
            catch (Exception e)
            {
                SetInfoText($"Error: {e.Message}");
            }
        }

        private void SetInfoText(string message)
        {
            if (_infoText != null)
            {
                _infoText.text = message;
            }
        }

        private void UpdateUserInfo()
        {
            var userData = UserData.Instance;
            var sb = new StringBuilder();
            sb.AppendLine($"UserId: {userData.UserId}");
            sb.AppendLine($"PlayerId: {userData.PlayerId}");
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine($"Level: {userData.LevelData.Level}");
            sb.AppendLine($"Exp: {userData.ExpData.Exp}");
            _infoUser.text = sb.ToString();
        }
    }
}
