using System.IO;
using CookApps.Build;
using UnityEditor;
using UnityEditor.iOS.Xcode;

public class PostprocessBuild : IPostprocessBuild
{
    public int callbackOrder => 1;
    public void OnPostprocessBuild(IPostBuildReport report)
    {
        if (report.Target == BuildTarget.iOS) // Check if the build is for iOS
        {
            string xcodeProjectPath =  report.OutputPath + "/Unity-iPhone.xcodeproj/project.pbxproj";

            PBXProject xcodeProject = new PBXProject();
            xcodeProject.ReadFromFile(xcodeProjectPath);

            string unityMainTargetGuid  = xcodeProject.GetUnityMainTargetGuid();
            string unityFrameworkTargetGuid = xcodeProject.GetUnityFrameworkTargetGuid();

            // IOS 릴리즈 시 FrameWork 오류로 인한 세팅
            xcodeProject.SetBuildProperty(unityMainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            xcodeProject.SetBuildProperty(unityFrameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            xcodeProject.SetBuildProperty(unityMainTargetGuid, "ENABLE_MODULE_VERIFIER" , "NO");
            xcodeProject.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_MODULE_VERIFIER" , "NO");

            // IOS 빌드 시 오류로 인한 세팅
            xcodeProject.SetBuildProperty(unityMainTargetGuid, "ENABLE_BITCODE", "NO");
            xcodeProject.SetBuildProperty(unityFrameworkTargetGuid , "ENABLE_BITCODE", "NO");

            xcodeProject.WriteToFile(xcodeProjectPath);

            string plistPath = report.OutputPath + "/Info.plist";

            PlistDocument plist = new PlistDocument(); // Read Info.plist file into memory
            plist.ReadFromString(File.ReadAllText(plistPath));

            PlistElementDict rootDict = plist.root;
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            // Edit plist list
            // ...

            File.WriteAllText(plistPath, plist.WriteToString()); // Override Info.plist
        }
    }
}
