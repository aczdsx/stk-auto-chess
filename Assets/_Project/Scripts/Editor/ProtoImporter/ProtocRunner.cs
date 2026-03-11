#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtocRunner
    {
        private static string BinDir => Path.GetFullPath("Assets/_Project/Scripts/Editor/ProtoImporter/bin~");

        private static string PathProtoc => GetPlatformExecutePath(Path.Combine(BinDir, "protoc"));

        private static string PathPlugin => GetPlatformExecutePath(Path.Combine(BinDir, "grpc_csharp_plugin"));

        internal static string ShellScriptsDir => BinDir;

        private static string GetPlatformExecutePath(string executePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return executePath + ".exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return executePath + "_linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return executePath + "_osx";
            throw new NotSupportedException("Unsupported OS platform");
        }

        public static bool GenerateCSharp(string protoRootDir, string outputDir)
        {
            try
            {
                ProtoFileUtil.ClearDirectoryContents(outputDir);
                ProtoFileUtil.EnsureDirectoryExists(outputDir);

                var protoFiles = Directory.GetFiles(protoRootDir, "*.proto", SearchOption.AllDirectories);
                var protoFileArgs = string.Join(" ", protoFiles.Select(f => $"\"{f}\""));

                var command = new List<string>
                {
                    $"--csharp_out={outputDir}",
                    $"--grpc_out={outputDir}",
                    $"--plugin=protoc-gen-grpc=\"{PathPlugin}\"",
                    $"--csharp_opt=serializable",
                };

                var protoFolders = ProtoFileUtil.GetAllDirectories(protoRootDir);
                protoFolders.Insert(0, protoRootDir);
                foreach (string path in protoFolders)
                {
                    if (!string.IsNullOrEmpty(path))
                        command.Add($"-I {path}");
                }

                command.Add(protoFileArgs);

                string arguments = string.Join(" ", command);
                var (result, error) = InvokeProcess(PathProtoc, arguments);
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError(error);
                }

                return string.IsNullOrEmpty(error);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        internal static (string result, string error) InvokeProcess(string fileName, string arguments)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var psi = new ProcessStartInfo
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
                return (string.Empty, "Failed to start process");

            p.WaitForExit();
            string error = p.StandardError.ReadToEnd();
            string result = p.StandardOutput.ReadToEnd();
            p.Close();
            return (result, error);
        }
    }
}
#endif
