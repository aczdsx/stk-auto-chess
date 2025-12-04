/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CookApps.NetLite.Editor.Asset;
using UnityEditor;

namespace CookApps.NetLite.Editor
{
    internal static class ProtoCSharpGenerator
    {
        /// 내부에서 사용하는 'hive-grpc repo'의 proto 파일로부터 C# 코드를 생성합니다.
        public static async void InternalGenerateCSharpFromProtoFiles()
        {
            try
            {
                await Task.Yield();
                const string protoDirRoot = "Assets/Package/Runtime/ProtoRoot";
                string protoDir = Path.GetFullPath(protoDirRoot);
                string csDir = Path.GetFullPath(protoDirRoot + "/Generated/CSharp");
                var result = await ProtoScriptRunner.RunCloneScript("hive-grpc", protoDir, Defines.IncludeDefaultServices, new CancellationTokenSource());
                if (!result.Result)
                {
                    UnityEngine.Debug.LogError("ProtoCSharpGenerator InternalGenerateCSharpFromProtoFiles에서 proto 파일 복제에 실패했습니다.");
                    return;
                }
                ProcessHelper.ProtocGenerationCSharp(result.ProtoDir, csDir);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        public static void UserGenerateCSharpFromProtoFilesFromAsset()
        {
            var asset = NetLiteAsset.GetOrCreateAsset();
            if(!asset.IsValid())
            {
                return;
            }
            UserGenerateCSharpFromProtoFiles(asset.IdlProjectName, asset.TargetDir);
        }

        public static async void UserGenerateCSharpFromProtoFiles(string repoDir, string targetDir, IEnumerable<string> serviceNames = null)
        {
            try
            {
                await Task.Yield();
                // Library에 임시로 폴더 생성 및 초기화
                string tempDir = Path.GetFullPath("Library/TempProto");
                // 폴더가 없으면 생성
                FileUtil.EnsureDirectoryExists(tempDir);
                FileUtil.ClearDirectoryContents(tempDir);

                var result = await ProtoScriptRunner.RunCloneScript(repoDir, tempDir, serviceNames, new CancellationTokenSource());
                if (!result.Result)
                {
                    UnityEngine.Debug.LogError($"git에서 {repoDir} proto 파일 복제에 실패했습니다.");
                    return;
                }

                string protoDir = result.ProtoDir;

                string pkgProto = Util.GetGrpcPackageFullPath("Runtime/ProtoRoot/Generated/Proto/hive-grpc");
                string pkgCs = Util.GetGrpcPackageFullPath("Runtime/ProtoRoot/Generated/CSharp");

                // pkg 내부의 proto 파일들을 result.ProtoDir에 복사
                FileUtil.CopyAllFilesToDirectory(pkgProto, result.ProtoDir);

                // C# 코드 생성
                string csDir = Path.GetFullPath(tempDir + "/Generated/CSharp");
                ProcessHelper.ProtocGenerationCSharp(protoDir, csDir);

                // pkg 내부의 cs 파일들을 csDir에서 제거
                FileUtil.RemoveFilesFromTargetBasedOnOrgDir(pkgCs, csDir);

                string targetFullDir = Path.GetFullPath(targetDir);
                // Generated 생성 폴더 내부 정리
                FileUtil.ClearDirectoryContents(Path.Combine(targetFullDir, "Generated/CSharp"));
                FileUtil.ClearDirectoryContents(Path.Combine(targetFullDir, "Generated/Proto"));
                // 생성된 파일들을 targetDir로 복사
                FileUtil.CopyAllFilesToDirectory(tempDir, targetFullDir);

                // 변경된 사항 에셋 데이터베이스에 반영
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
