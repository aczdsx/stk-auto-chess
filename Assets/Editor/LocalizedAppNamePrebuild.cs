#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class LocalizedAppNamePrebuild : IPreprocessBuildWithReport
{
    private const string EN_APP_NAME = "Stella Knights";
    private const string KO_APP_NAME = "스텔라나이츠";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // iOS 빌드가 아닐 때는 무시
        if (report.summary.platform != BuildTarget.iOS)
            return;

        CreateInfoPlistStrings("en", EN_APP_NAME);
        CreateInfoPlistStrings("ko", KO_APP_NAME);

        AssetDatabase.Refresh();
    }

    private static void CreateInfoPlistStrings(string locale, string displayName)
    {
        // Unity는 Assets/Plugins/iOS/ 하위 구조를 Xcode로 그대로 복사하며,
        // <locale>.lproj/InfoPlist.strings 는 로컬라이즈된 앱 표시명으로 인식됩니다.
        string dir = $"Assets/Plugins/iOS/{locale}.lproj";
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, "InfoPlist.strings");

        // CFBundleDisplayName 키를 반드시 사용해야 홈 화면 표시명이 바뀜
        // (CFBundleName 아님!)
        string content = "\"CFBundleDisplayName\" = \"" + EscapeForStrings(displayName) + "\";\n";

        // BOM 없는 UTF-8 권장
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeForStrings(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif