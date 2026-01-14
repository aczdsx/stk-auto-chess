#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// CookAppsLocalData 관련 에디터 유틸리티
    /// </summary>
    public static class LocalDataEditorTools
    {
        private static string GetLocalDataPath()
        {
            var folderName = $"localdata_{PlayerSettings.applicationIdentifier.ToLower().Replace(".", "_")}";
            return Path.Combine(Application.persistentDataPath, folderName);
        }

        [MenuItem("Tools/Local Data/Delete All Local Data")]
        public static void DeleteAllLocalData()
        {
            string folderPath = GetLocalDataPath();

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Debug.Log($"[LocalDataEditorTools] 모든 로컬 데이터가 삭제되었습니다: {folderPath}");
            }
            else
            {
                Debug.Log($"[LocalDataEditorTools] 삭제할 로컬 데이터가 없습니다: {folderPath}");
            }
        }

        [MenuItem("Tools/Local Data/Delete All PlayerPrefs")]
        public static void DeleteAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[LocalDataEditorTools] 모든 PlayerPrefs가 삭제되었습니다.");
        }

        [MenuItem("Tools/Local Data/Delete All (Local Data + PlayerPrefs)")]
        public static void DeleteAll()
        {
            DeleteAllLocalData();
            DeleteAllPlayerPrefs();
            Debug.Log("[LocalDataEditorTools] 모든 로컬 데이터와 PlayerPrefs가 삭제되었습니다.");
        }

        [MenuItem("Tools/Local Data/Open Persistent Data Path")]
        public static void OpenPersistentDataPath()
        {
            string productName = PlayerSettings.productName;
            if (productName.Contains(":"))
            {
                productName = productName.Replace(":", "_");
            }

#if UNITY_EDITOR_WIN
            string appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            System.Diagnostics.Process.Start("explorer.exe",
                $"{appdata}\\..\\LocalLow\\{PlayerSettings.companyName}\\{productName}");
#elif UNITY_EDITOR_OSX
            var path = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", PlayerSettings.companyName, productName);
            System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
#endif
        }
    }
}
#endif
