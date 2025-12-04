/*
* Copyright (c) CookApps.
* 이진호(jhlee8@cookapps.com)
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
namespace CookApps.NetLite.Editor
{
    internal static class ProcessHelper
    {
        private static string PathProtoc
        {
            get
            {
                TryGetProtocRootDir(out string protocRootDir);
                Assert.IsNotNull(protocRootDir);
                string executePath = Path.Combine(protocRootDir, "bin/protoc");
                return GetExecutePath(executePath);
            }
        }

        private static string PathPlugin
        {
            get
            {
                TryGetProtocRootDir(out string protocRootDir);
                Assert.IsNotNull(protocRootDir);
                string executePath = Path.Combine(protocRootDir, "bin/grpc_csharp_plugin");
                return GetExecutePath(executePath);
            }
        }

        // internal static string UniversalProtoPath
        // {
        //     get
        //     {
        //         TryGetProtocRootDir(out string protocRootDir);
        //         Assert.IsNotNull(protocRootDir);
        //         string executePath = Path.Combine(protocRootDir, "UniversalProto");
        //         return executePath;
        //     }
        // }

        private static string GetExecutePath(string executePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return executePath + ".exe";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return executePath + "_linux";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return executePath + "_osx";
            }

            throw new NotSupportedException();
        }

        internal static bool TryGetProtocRootDir(out string path)
        {
            // 패키지 형식으로 존재하면
            string dirPackage = Path.GetFullPath($"{Util.TechPackageName}/Editor");
            if (Directory.Exists(dirPackage))
            {
                path = dirPackage;
                return false;
            }

            // asmdef 형식이면
            string assemblyDefinitionFilePath =
                CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("CookApps.NetLite.Editor");
            string dir = Path.GetDirectoryName(assemblyDefinitionFilePath);
            Assert.IsNotNull(dir);
            dir = Path.GetFullPath(dir);
            path = dir;
            return true;
        }

        public static (bool found, string version) ProtocVersion()
        {
            try
            {
                (string result, string error) pair = InvokeProcessStart(PathProtoc, "--version");
                return (true, pair.result);
            }
            catch
            {
                return (false, null);
            }
        }

        public static bool ProtocGenerationCSharp(string protoPath, string outputDir, IReadOnlyList<string> protoFolders)
        {
            try
            {
                var command = new List<string>
                {
                    $"--csharp_out={outputDir}",
                    $"--grpc_out={outputDir}",
                    $"{protoPath}", // 컴파일할 proto 파일
                    $"--plugin=protoc-gen-grpc=\"{PathPlugin}\"",
                    $"--csharp_opt=serializable",
                    // $"--proto_path={Path.GetDirectoryName(protoPath)}",
                };

                foreach (string path in protoFolders)
                {
                    var convPath = path;
                    if (!string.IsNullOrEmpty(convPath))
                    {
                        command.Add($"-I {convPath}"); // import 경로 추가
                    }
                }

                string arguments = string.Join(" ", command);
                (string result, string error) pair = InvokeProcessStart(PathProtoc, arguments);
                if (!string.IsNullOrEmpty(pair.error))
                {
                    Debug.LogError(pair.error);
                }

                return string.IsNullOrEmpty(pair.error);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
        }

        public static bool ProtocGenerationCSharp(string protoRootDir, string outputDir)
        {
            try
            {
                // 출력 디렉토리 초기화
                FileUtil.ClearDirectoryContents(outputDir);
                FileUtil.EnsureDirectoryExists(outputDir);

                var protoFiles = Directory.GetFiles(protoRootDir, "*.proto", SearchOption.AllDirectories);
                var protoFileArgs = string.Join(" ", protoFiles.Select(f => $"\"{f}\""));

                var command = new List<string>
                {
                    $"--csharp_out={outputDir}",
                    $"--grpc_out={outputDir}",
                    $"--plugin=protoc-gen-grpc=\"{PathPlugin}\"",
                    $"--csharp_opt=serializable",
                };

                // protoRootDir 하위의 모든 폴더를 protoFolders로 수집
                var protoFolders = FileUtil.GetAllDirectories(protoRootDir);
                protoFolders.Insert(0, protoRootDir); // 루트 폴더도 포함
                foreach (string path in protoFolders)
                {
                    var convPath = path;
                    if (!string.IsNullOrEmpty(convPath))
                    {
                        command.Add($"-I {convPath}"); // import 경로 추가
                    }
                }
                // 모든 proto 파일 추가
                command.Add(protoFileArgs);

                string arguments = string.Join(" ", command);
                (string result, string error) pair = InvokeProcessStart(PathProtoc, arguments);
                if (!string.IsNullOrEmpty(pair.error))
                {
                    Debug.LogError(pair.error);
                }

                return string.IsNullOrEmpty(pair.error);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
        }



        private static (string result, string error) InvokeProcessStart(string fileName, string arguments)
        {
            string projectRoot = Application.dataPath; // Assets 폴더의 경로
            projectRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
            var psi = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = projectRoot,
            };

            Process p = Process.Start(psi);
            if (p == null)
            {
                return (string.Empty, "error");
            }

            p.WaitForExit();
            string error = p.StandardError.ReadToEnd();
            string result = p.StandardOutput.ReadToEnd();
            p.Close();
            return (result, error);
        }
    }
}
