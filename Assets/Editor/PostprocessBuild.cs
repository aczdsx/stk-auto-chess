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
