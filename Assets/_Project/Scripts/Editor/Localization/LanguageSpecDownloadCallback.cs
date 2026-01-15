#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CookApps.SpecData.Hive;
using CookApps.SpecData.Hive.Editor;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// 스펙 다운로드 완료 시 Language 스펙을 다운로드하여 파일로 저장합니다.
    /// 파일 저장 후 LanguageSpecAssetPostprocessor가 자동으로 Localization 임포트를 실행합니다.
    /// </summary>
    public class LanguageSpecDownloadCallback : ISpecDownloadCallback
    {
        private const string SpecLanguagePath = "Assets/OriginalSpecLanguage.json";

        public async void OnSpecDownloadCompleted()
        {
            try
            {
                await DownloadLanguageSpecAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static async Task DownloadLanguageSpecAsync()
        {
            // 1. SpecDataAsset에서 AppId 가져오기 (internal 클래스이므로 리플렉션)
            uint appId = GetAppIdFromSpecDataAsset();
            if (appId == 0)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] AppId를 가져올 수 없습니다.");
                return;
            }

            // 2. 버전 정보 가져오기 (Dev 환경 기준)
            var versionInfo = await GetSpecVersionInfosAsync(appId, SpecEnvironment.Dev);
            if (versionInfo == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] 버전 정보를 가져올 수 없습니다.");
                return;
            }

            // 3. Language 스펙 버전 가져오기
            uint languageVersion = GetPublishVersion(versionInfo, SpecType.Language);
            if (languageVersion == 0)
            {
                Debug.LogWarning("[LanguageSpecDownloadCallback] Language 스펙 버전이 0입니다.");
                return;
            }

            // 4. Language 스펙 데이터 다운로드
            string languageData = await GetSpecDataAsync(appId, languageVersion, SpecType.Language, SpecEnvironment.Dev);
            if (string.IsNullOrEmpty(languageData))
            {
                Debug.LogError("[LanguageSpecDownloadCallback] Language 스펙 데이터를 다운로드할 수 없습니다.");
                return;
            }

            // 5. 기존 파일과 비교하여 변경된 경우에만 저장
            string existingData = File.Exists(SpecLanguagePath) ? File.ReadAllText(SpecLanguagePath) : null;
            if (existingData == languageData)
            {
                Debug.Log("[LanguageSpecDownloadCallback] Language 스펙이 변경되지 않아 임포트를 스킵합니다.");
                return;
            }

            // 6. 파일로 저장 (AssetPostprocessor가 변경 감지 후 임포트 실행)
            File.WriteAllText(SpecLanguagePath, languageData);
            AssetDatabase.ImportAsset(SpecLanguagePath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[LanguageSpecDownloadCallback] Language 스펙 다운로드 완료: {SpecLanguagePath}");
        }

        /// <summary>
        /// SpecDataAsset에서 AppId를 리플렉션으로 가져옵니다.
        /// </summary>
        private static uint GetAppIdFromSpecDataAsset()
        {
            var specDataAssetType = GetSpecDataAssetType();
            if (specDataAssetType == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] SpecDataAsset 타입을 찾을 수 없습니다.");
                return 0;
            }

            var getAssetsMethod = specDataAssetType.GetMethod("GetAssets", BindingFlags.Public | BindingFlags.Static);
            if (getAssetsMethod == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] GetAssets 메서드를 찾을 수 없습니다.");
                return 0;
            }

            var asset = getAssetsMethod.Invoke(null, null);
            if (asset == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] SpecDataAsset 인스턴스를 가져올 수 없습니다.");
                return 0;
            }

            var appIdField = specDataAssetType.GetField("AppId", BindingFlags.Public | BindingFlags.Instance);
            if (appIdField == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] AppId 필드를 찾을 수 없습니다.");
                return 0;
            }

            return (uint)appIdField.GetValue(asset);
        }

        /// <summary>
        /// HiveAdminSpecVersionInfo에서 GetPublishVersion을 리플렉션으로 호출합니다.
        /// </summary>
        private static uint GetPublishVersion(object versionInfo, SpecType specType)
        {
            if (versionInfo == null) return 0;

            var method = versionInfo.GetType().GetMethod("GetPublishVersion", BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] GetPublishVersion 메서드를 찾을 수 없습니다.");
                return 0;
            }

            return (uint)method.Invoke(versionInfo, new object[] { specType });
        }

        /// <summary>
        /// HiveAdminSpec 내부 클래스의 메서드를 리플렉션으로 호출하여 스펙 버전 정보를 가져옵니다.
        /// </summary>
        public static async Task<object> GetSpecVersionInfosAsync(uint appId, SpecEnvironment environment)
        {
            var hiveAdminSpecType = GetHiveAdminSpecType();
            if (hiveAdminSpecType == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] HiveAdminSpec 타입을 찾을 수 없습니다.");
                return null;
            }

            var method = hiveAdminSpecType.GetMethod("GetSpecVersionInfos", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] GetSpecVersionInfos 메서드를 찾을 수 없습니다.");
                return null;
            }

            var task = (Task)method.Invoke(null, new object[] { appId, environment });
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        /// <summary>
        /// HiveAdminSpec 내부 클래스의 메서드를 리플렉션으로 호출하여 스펙 데이터를 가져옵니다.
        /// </summary>
        public static async Task<string> GetSpecDataAsync(uint appId, uint version, SpecType specType, SpecEnvironment environment)
        {
            var hiveAdminSpecType = GetHiveAdminSpecType();
            if (hiveAdminSpecType == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] HiveAdminSpec 타입을 찾을 수 없습니다.");
                return null;
            }

            var method = hiveAdminSpecType.GetMethod("GetSpecData", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Debug.LogError("[LanguageSpecDownloadCallback] GetSpecData 메서드를 찾을 수 없습니다.");
                return null;
            }

            var task = (Task<string>)method.Invoke(null, new object[] { appId, version, specType, environment });
            return await task.ConfigureAwait(false);
        }

        private static Type GetHiveAdminSpecType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("CookApps.SpecData.Hive.Editor.HiveAPI.HiveAdminSpec");
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static Type GetSpecDataAssetType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("CookApps.SpecData.Hive.Editor.Asset.SpecDataAsset");
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}
#endif
