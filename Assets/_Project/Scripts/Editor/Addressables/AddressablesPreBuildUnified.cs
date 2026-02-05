/*
* Copyright (c) CookApps.
*/

using System;
using CookApps.Build;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// 빌드 시 Addressables 관련 전처리 및 후처리를 담당하는 클래스
    /// </summary>
    internal class AddressablesPreBuildUnified : IPreprocessBuild, IPostprocessBuild
    {
        private const string ProfileDefault = "Default";
        private const string ContentStateBinFileName = "addressables_content_state.bin";
        private const string DevDefineSymbol = "__DEV";

        private string _previousProfileId;

        public int callbackOrder => -100;

        /// <summary>
        /// 빌드 전처리: 프로필 백업 및 빌드 설정 적용
        /// </summary>
        public void OnPreprocessBuild(IPreBuildReport report)
        {
            AddressableAssetSettings settings = GetSettings();

            AddressableSpriteAtlasGuard.PreAddressableBuild(report.Target);

            // 로컬 환경에서만 이전 프로필 백업
            BackupCurrentProfile(settings);

            // 빌드 설정 강제 적용
            ConfigureRemoteCatalog(settings);

            // 타겟 프로필 결정 및 변경
            string targetProfileName = AddressableProfileHelper.TargetProfile;
            string buildTarget = AddressableProfileHelper.BuildTarget;

            ChangeActiveProfile(settings, targetProfileName);

            Debug.Log($"[Addressables] Target Profile: {targetProfileName}, Build Target: {buildTarget}");

            // Addressables 빌드 실행
            PerformAddressablesBuild(settings, targetProfileName);
        }

        /// <summary>
        /// 빌드 후처리: 로컬 환경에서 프로필 복구
        /// </summary>
        public void OnPostprocessBuild(IPostBuildReport report)
        {
            // 배치모드(CI/빌드머신)가 아닐 때만 프로필 복구 로직을 실행
            if (Application.isBatchMode)
            {
                Debug.Log("[Addressables] 배치모드(CI/빌드머신) 감지: 프로필 복귀를 스킵");
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                return;
            }

            RestoreProfile(settings);
        }

        /// <summary>
        /// AddressableAssetSettings를 안전하게 가져옴
        /// </summary>
        private static AddressableAssetSettings GetSettings()
        {
            return AddressableAssetSettingsDefaultObject.Settings
                   ?? throw new Exception("[Addressables] Settings null");
        }

        /// <summary>
        /// 현재 활성 프로필 백업 (로컬 환경만)
        /// </summary>
        private void BackupCurrentProfile(AddressableAssetSettings settings)
        {
            if (!Application.isBatchMode)
            {
                _previousProfileId = settings.activeProfileId;
                string profileName = settings.profileSettings.GetProfileName(_previousProfileId);
                Debug.Log($"[Addressables] 현재 프로필 백업: {profileName}");
            }
        }

        /// <summary>
        /// Remote Catalog 설정 강제 적용
        /// </summary>
        private static void ConfigureRemoteCatalog(AddressableAssetSettings settings)
        {
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            Debug.Log("[Addressables] Remote Catalog 설정 적용 완료");
        }

        /// <summary>
        /// 활성 프로필 변경
        /// </summary>
        private static void ChangeActiveProfile(AddressableAssetSettings settings, string targetProfileName)
        {
            string profileId = settings.profileSettings.GetProfileId(targetProfileName);
            if (string.IsNullOrEmpty(profileId))
            {
                throw new Exception($"[Addressables] Profile '{targetProfileName}' not found");
            }

            settings.activeProfileId = profileId;
            Debug.Log($"[Addressables] Active Profile 변경: {targetProfileName}");
        }

        /// <summary>
        /// Addressables 빌드 실행 (증분 빌드 또는 새 빌드)
        /// </summary>
        private static void PerformAddressablesBuild(AddressableAssetSettings settings, string targetProfileName)
        {
            string contentStateBinFile = GetContentStateBinFilePath(settings);
            Debug.Log($"[Addressables] Content state 파일 경로: {contentStateBinFile}");

            if (System.IO.File.Exists(contentStateBinFile))
            {
                PerformContentUpdateBuild(settings, contentStateBinFile);
            }
            else
            {
                PerformNewBuild();
            }

            Debug.Log($"[Addressables] 빌드 완료 (Profile={targetProfileName})");
        }

        /// <summary>
        /// Content state bin 파일 경로 가져오기
        /// </summary>
        private static string GetContentStateBinFilePath(AddressableAssetSettings settings)
        {
            string contentStatePath = settings.ContentStateBuildPath;
            string evaluatedPath = settings.profileSettings.EvaluateString(
                settings.activeProfileId, contentStatePath);
            return System.IO.Path.Combine(evaluatedPath, ContentStateBinFileName);
        }

        /// <summary>
        /// 증분 빌드 (Update a Previous Build)
        /// </summary>
        private static void PerformContentUpdateBuild(AddressableAssetSettings settings, string contentStateBinFile)
        {
            Debug.Log("[Addressables] 이전 빌드 상태 발견 → 증분 빌드 시작");

            AddressableAssetBuildResult result = ContentUpdateScript.BuildContentUpdate(settings, contentStateBinFile);
            if (result != null && !string.IsNullOrEmpty(result.Error))
            {
                throw new Exception($"[Addressables] Content Update Build 실패: {result.Error}");
            }
        }

        /// <summary>
        /// 새 빌드 (New Build)
        /// </summary>
        private static void PerformNewBuild()
        {
            Debug.Log("[Addressables] 이전 빌드 상태 없음 → 새 빌드 시작");

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception($"[Addressables] Build Player Content 실패: {result.Error}");
            }
        }

        /// <summary>
        /// 프로필 복구 (로컬 환경만)
        /// </summary>
        private void RestoreProfile(AddressableAssetSettings settings)
        {
            string restoreId = GetRestoreProfileId(settings);
            if (string.IsNullOrEmpty(restoreId))
            {
                Debug.LogWarning("[Addressables] 복구할 프로필을 찾을 수 없음");
                return;
            }

            settings.activeProfileId = restoreId;
            string profileName = settings.profileSettings.GetProfileName(restoreId);
            Debug.Log($"[Addressables] 프로필 복구 완료: {profileName}");
        }

        /// <summary>
        /// 복구할 프로필 ID 가져오기
        /// </summary>
        private string GetRestoreProfileId(AddressableAssetSettings settings)
        {
            if (!string.IsNullOrEmpty(_previousProfileId))
            {
                return _previousProfileId;
            }

            return settings.profileSettings.GetProfileId(ProfileDefault);
        }
    }
}