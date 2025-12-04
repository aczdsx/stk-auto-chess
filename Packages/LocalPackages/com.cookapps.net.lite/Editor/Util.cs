/*
* Copyright (c) CookApps.
*/

using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;

namespace CookApps.NetLite.Editor
{
    internal static class Util
    {
        internal const string PackageName = "com.cookapps.net.lite";
        internal const string TechPackageName = "Packages/" + PackageName;
        internal const string TechPackageRuntimeProtoRoot = TechPackageName + "/Runtime/ProtoRoot";


        /// <summary>
        /// 패키지의 FullPath를 얻는다. (패키지 개발, 배포 공통)
        /// </summary>
        /// <param name="subPath">패키지 내부의 경로 (없으면 root)</param>
        /// <returns></returns>
        public static string GetGrpcPackageFullPath(string subPath = null)
        {
            PackageInfo info = PackageInfo.FindForAssetPath(TechPackageName);
            string path = info == null ? Path.GetFullPath("Assets/Package") : info.resolvedPath;
            return string.IsNullOrEmpty(subPath) ? path : Path.Combine(path, subPath);
        }
    }
}
