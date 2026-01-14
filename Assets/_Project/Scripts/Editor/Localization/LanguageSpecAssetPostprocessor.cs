#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// OriginalSpecLanguage.json 파일이 변경될 때 자동으로 Localization 임포트 실행
    /// </summary>
    public class LanguageSpecAssetPostprocessor : AssetPostprocessor
    {
        private const string SpecLanguagePath = "Assets/OriginalSpecLanguage.json";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath == SpecLanguagePath)
                {
                    ImportLocalization();
                    return;
                }
            }
        }

        private static void ImportLocalization()
        {
            Debug.Log($"[LanguageSpecAssetPostprocessor] {SpecLanguagePath} 변경 감지, Localization 임포트 시작...");

            var result = LocalizationImporter.ImportAllFromJsonFile(SpecLanguagePath);
            if (result.Success)
            {
                Debug.Log($"[LanguageSpecAssetPostprocessor] Localization 임포트 완료\n{result.GetSummary()}");
            }
            else
            {
                Debug.LogError($"[LanguageSpecAssetPostprocessor] Localization 임포트 실패: {result.ErrorMessage}");
            }
        }
    }
}
#endif
