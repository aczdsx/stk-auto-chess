# Proto Importer 내재화 Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** NetLite 패키지의 "API 업데이트" 기능을 프로젝트에 내재화하고, 기존 ProtoBufCleaner와 통합하여 CookApps Window에서 관리 가능한 Proto Importer를 만든다.

**Architecture:** `Assets/_Project/Scripts/Editor/ProtoImporter/`에 에디터 코드, `Tools/protoc/`에 바이너리와 셸 스크립트를 배치한다. 설정 에셋(ScriptableObject)으로 import 설정과 cleanup 설정을 통합 관리하며, CookAppsPackageWindow에 탭으로 등록한다. 워크플로우: IDL clone → 서비스 필터링 → protoc C# 생성 → cleanup → AssetDatabase 갱신.

**Tech Stack:** Unity 6 Editor, C# (System.Diagnostics.Process), bash/PowerShell 셸 스크립트, protoc + grpc_csharp_plugin

---

## File Structure

### 생성할 파일

| 파일 | 역할 |
|------|------|
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSetting.cs` | 통합 설정 SO (import + cleanup) |
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSettingEditor.cs` | 커스텀 인스펙터 (API 업데이트 버튼, 저장소 리스트 버튼) |
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterWindow.cs` | CookApps Window 탭 등록 |
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporter.cs` | 메인 오케스트레이터 (clone → generate → clean) |
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoCloner.cs` | IDL clone + 서비스 필터링 + 폴더 평탄화 + import 경로 수정 |
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtocRunner.cs` | protoc 실행 래퍼 |
| `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoFileUtil.cs` | 파일 유틸리티 |
| `Tools/protoc/bin/` | protoc 바이너리 복사 (protoc_osx, protoc_linux, protoc.exe, grpc_csharp_plugin_*, include/) |
| `Tools/protoc/ShellScripts/` | 셸 스크립트 복사 (protos_clone_ssh.sh/.ps1, protos_clone_https.sh/.ps1, ssh_setting.ps1) |

### 삭제할 파일

| 파일 | 이유 |
|------|------|
| `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleaner.cs` | ProtoImporter에 통합 |
| `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.cs` | ProtoImporterSetting에 통합 |
| `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.asset` | 새 설정 에셋으로 대체 |
| `Assets/CookApps/Editor/NetLiteAsset.asset` | 새 설정 에셋으로 대체 (패키지 탭에서도 제거됨) |

### 원본 → 내재화 매핑

| 패키지 원본 (Library/PackageCache/com.cookapps.net.lite@.../Editor/) | 내재화 대상 |
|------|------|
| `ProtoCSharpGenerator.cs` | `ProtoImporter.cs` |
| `ProtoSync/ProtoScriptRunner.cs` | `ProtoCloner.cs` |
| `ProcessHelper.cs` | `ProtocRunner.cs` |
| `FileUtil.cs` | `ProtoFileUtil.cs` |
| `Asset/NetLiteAsset.cs` + `ProtoBufCleanupSetting.cs` | `ProtoImporterSetting.cs` |
| `Asset/NetLiteAssetEditor.cs` | `ProtoImporterSettingEditor.cs` |
| `Window/PackageWindow.cs` | `ProtoImporterWindow.cs` |
| `Defines.cs` | `ProtoImporterSetting` 필드로 통합 |

---

## Chunk 1: 기반 구조

### Task 1: Tools/protoc/ 바이너리 및 스크립트 복사

**Files:**
- Create: `Tools/protoc/bin/` (바이너리 + include 전체)
- Create: `Tools/protoc/ShellScripts/` (5개 스크립트)

- [ ] **Step 1: Tools/protoc/bin/ 디렉토리 생성 및 바이너리 복사**

```bash
mkdir -p Tools/protoc/bin
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/protoc_osx Tools/protoc/bin/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/protoc_linux Tools/protoc/bin/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/protoc.exe Tools/protoc/bin/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/grpc_csharp_plugin_osx Tools/protoc/bin/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/grpc_csharp_plugin_linux Tools/protoc/bin/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/grpc_csharp_plugin.exe Tools/protoc/bin/
cp -r Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/bin/include Tools/protoc/bin/
chmod +x Tools/protoc/bin/protoc_osx Tools/protoc/bin/protoc_linux
chmod +x Tools/protoc/bin/grpc_csharp_plugin_osx Tools/protoc/bin/grpc_csharp_plugin_linux
```

- [ ] **Step 2: ShellScripts 복사**

```bash
mkdir -p Tools/protoc/ShellScripts
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoSync/ShellScripts/protos_clone_ssh.sh Tools/protoc/ShellScripts/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoSync/ShellScripts/protos_clone_ssh.ps1 Tools/protoc/ShellScripts/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoSync/ShellScripts/protos_clone_https.sh Tools/protoc/ShellScripts/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoSync/ShellScripts/protos_clone_https.ps1 Tools/protoc/ShellScripts/
cp Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoSync/ShellScripts/ssh_setting.ps1 Tools/protoc/ShellScripts/
chmod +x Tools/protoc/ShellScripts/*.sh
```

- [ ] **Step 3: .gitignore 확인**

`Tools/protoc/bin/`에 바이너리가 포함되므로 `.gitignore`에서 제외되지 않는지 확인. 만약 `*.exe` 등이 무시되고 있으면 `!Tools/protoc/bin/` 예외를 추가.

- [ ] **Step 4: Commit**

```bash
git add Tools/protoc/
git commit -m "chore: Tools/protoc에 protoc 바이너리 및 셸 스크립트 배치"
```

---

### Task 2: ProtoFileUtil.cs — 파일 유틸리티

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoFileUtil.cs`

원본: `Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/FileUtil.cs`

- [ ] **Step 1: ProtoFileUtil.cs 작성**

패키지의 `FileUtil.cs`를 포팅. 네임스페이스 변경, `#if UNITY_EDITOR` 가드 추가.

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtoFileUtil
    {
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static List<string> GetAllDirectories(string root)
        {
            var result = new List<string>();
            try
            {
                foreach (var dir in Directory.GetDirectories(root))
                {
                    result.Add(dir);
                    result.AddRange(GetAllDirectories(dir));
                }
            }
            catch
            {
                // ignored
            }
            return result;
        }

        public static void ClearDirectoryContents(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return;

            try
            {
                foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try { File.Delete(file); }
                    catch { /* ignored */ }
                }
                foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try { Directory.Delete(dir, true); }
                    catch { /* ignored */ }
                }
            }
            catch { /* ignored */ }
        }

        public static void CopyAllFilesToDirectory(string orgDir, string targetDir)
        {
            if (string.IsNullOrEmpty(orgDir) || !Directory.Exists(orgDir))
                return;
            if (string.IsNullOrEmpty(targetDir))
                return;

            try
            {
                CopyAllFilesRecursively(orgDir, targetDir, "");
            }
            catch { /* ignored */ }
        }

        private static void CopyAllFilesRecursively(string sourceDir, string targetDir, string relativePath)
        {
            var currentSource = Path.Combine(sourceDir, relativePath);
            var currentTarget = Path.Combine(targetDir, relativePath);

            if (!Directory.Exists(currentTarget))
                Directory.CreateDirectory(currentTarget);

            var files = Directory.GetFiles(currentSource)
                .Where(f => Path.GetExtension(f).ToLower() != ".meta");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(currentTarget, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var subDir in Directory.GetDirectories(currentSource))
            {
                var subDirName = Path.GetFileName(subDir);
                CopyAllFilesRecursively(sourceDir, targetDir, Path.Combine(relativePath, subDirName));
            }
        }

        public static void RemoveFilesFromTargetBasedOnOrgDir(string orgDir, string targetDir)
        {
            if (string.IsNullOrEmpty(orgDir) || !Directory.Exists(orgDir))
                return;
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
                return;

            try
            {
                var orgFileNames = Directory.GetFiles(orgDir, "*", SearchOption.AllDirectories)
                    .Where(f => Path.GetExtension(f).ToLower() != ".meta")
                    .Select(f => Path.GetFileName(f).ToLower())
                    .ToHashSet();

                foreach (var file in Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories))
                {
                    if (orgFileNames.Contains(Path.GetFileName(file).ToLower()))
                    {
                        try { File.Delete(file); }
                        catch { /* ignored */ }
                    }
                }
            }
            catch { /* ignored */ }
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtoFileUtil.cs
git commit -m "feat: ProtoFileUtil 추가 — 파일 유틸리티"
```

---

### Task 3: ProtocRunner.cs — protoc 실행 래퍼

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtocRunner.cs`

원본: `Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProcessHelper.cs`

- [ ] **Step 1: ProtocRunner.cs 작성**

패키지의 `ProcessHelper.cs`를 포팅. 경로를 `Tools/protoc/bin/`으로 변경하고 패키지 경로 탐색(`TryGetProtocRootDir`) 제거.

```csharp
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtocRunner
    {
        private static string ToolsRootDir => Path.GetFullPath("Tools/protoc");

        private static string PathProtoc
        {
            get
            {
                string executePath = Path.Combine(ToolsRootDir, "bin/protoc");
                return GetPlatformExecutePath(executePath);
            }
        }

        private static string PathPlugin
        {
            get
            {
                string executePath = Path.Combine(ToolsRootDir, "bin/grpc_csharp_plugin");
                return GetPlatformExecutePath(executePath);
            }
        }

        internal static string ShellScriptsDir => Path.Combine(ToolsRootDir, "ShellScripts");

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
                    Debug.LogError(error);
                }

                return string.IsNullOrEmpty(error);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
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
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtocRunner.cs
git commit -m "feat: ProtocRunner 추가 — protoc 실행 래퍼"
```

---

## Chunk 2: Clone 및 메인 로직

### Task 4: ProtoCloner.cs — IDL clone + 필터링 + 평탄화

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoCloner.cs`

원본: `Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoSync/ProtoScriptRunner.cs`

- [ ] **Step 1: ProtoCloner.cs 작성**

패키지의 `ProtoScriptRunner.cs`를 포팅. 셸 스크립트 경로를 `Tools/protoc/ShellScripts/`로 변경. `Util.GetGrpcPackageFullPath()` 참조 제거.

```csharp
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtoCloner
    {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        private const string ProtoCloneSshScript = "protos_clone_ssh.sh";
        private const string ProtoCloneHttpsScript = "protos_clone_https.sh";
#else
        private const string ProtoCloneSshScript = "protos_clone_ssh.ps1";
        private const string ProtoCloneHttpsScript = "protos_clone_https.ps1";
#endif

        private struct GitProjectInfo
        {
            public bool IsSsh;
            public string RepositoryName;
        }

        private static GitProjectInfo GetGitProjectInfo()
        {
            string url = null;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "config --get remote.origin.url",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Application.dataPath
                };

                using var process = Process.Start(startInfo);
                using var reader = process?.StandardOutput;
                url = reader?.ReadToEnd().Trim();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to get git remote origin URL: " + e.Message);
            }

            if (string.IsNullOrEmpty(url))
                return new GitProjectInfo { IsSsh = false, RepositoryName = string.Empty };

            var segments = url.Split('/');
            if (segments.Length == 0)
                return new GitProjectInfo { IsSsh = false, RepositoryName = string.Empty };

            var repoName = segments[^1];
            return new GitProjectInfo
            {
                IsSsh = url.Contains("git@"),
                RepositoryName = repoName.EndsWith(".git") ? repoName[..^4] : repoName
            };
        }

        /// <summary>
        /// IDL 저장소에서 proto 파일을 clone하고, 서비스 필터링/평탄화/import 경로 수정을 수행합니다.
        /// </summary>
        public static async Task<(bool Result, string ProtoDir)> RunCloneScript(
            string projName, string rootFolder,
            IEnumerable<string> includeServices, IEnumerable<string> excludeServices,
            CancellationTokenSource cts)
        {
            const string branchName = "main";
            string shellScriptsDir = ProtocRunner.ShellScriptsDir;

            var projInfo = GetGitProjectInfo();
            string scriptPath = projInfo.IsSsh
                ? Path.Combine(shellScriptsDir, ProtoCloneSshScript)
                : Path.Combine(shellScriptsDir, ProtoCloneHttpsScript);
            Debug.Log(projInfo.IsSsh ? "Use SSH" : "Use HTTPS");

            const string protoCloneFolder = "Generated/Proto";
            string targetFolder = Path.Combine(rootFolder, protoCloneFolder, projName);
            ProtoFileUtil.EnsureDirectoryExists(targetFolder);
            ProtoFileUtil.ClearDirectoryContents(targetFolder);

            var protoProjDirInfo = new DirectoryInfo(targetFolder);
            if (projName != protoProjDirInfo.Name)
            {
                Debug.LogError($"IDL project name mismatch: {projName} != {protoProjDirInfo.Name}");
                return (false, string.Empty);
            }

            string directoryPath = new DirectoryInfo(rootFolder).FullName;

#if UNITY_EDITOR_WIN
            if (projInfo.IsSsh)
            {
                string sshConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".ssh", "config");

                EditorUtility.DisplayProgressBar("Activate ssh-agent", "Activating ssh-agent...", 1f);
                try
                {
                    if (!File.Exists(sshConfigPath))
                    {
                        await File.WriteAllTextAsync(sshConfigPath,
                            "Host *\n  AddKeysToAgent yes\n  IdentitiesOnly yes");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write ssh config file: {e.Message}");
                }

                string sshSettingScript = Path.Combine(shellScriptsDir, "ssh_setting.ps1");
                var sshTask = Task.Run(() =>
                {
                    cts.Token.ThrowIfCancellationRequested();
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{sshSettingScript}\"",
                            RedirectStandardOutput = false,
                            RedirectStandardError = false,
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = false,
                        };
                        using var process = new Process { StartInfo = psi };
                        process.Start();
                        process.WaitForExit();
                        return process.ExitCode;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to execute ssh command: {e.Message}");
                        return 1;
                    }
                }, cts.Token);

                int sshSuccess;
                try
                {
                    sshSuccess = await sshTask;
                    EditorUtility.ClearProgressBar();
                }
                catch (OperationCanceledException)
                {
                    EditorUtility.ClearProgressBar();
                    sshSuccess = -2;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error: {e}, {e.Message}");
                    EditorUtility.ClearProgressBar();
                    sshSuccess = -1;
                }

                switch (sshSuccess)
                {
                    case 1:
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "Windows PowerShell을 관리자 권한으로 실행해야 합니다.\n\n안전한 SSH 접속을 위한 과정입니다. 다시 시도할 때에는 \"예\" 버튼을 눌러주세요.",
                            "확인");
                        return (false, string.Empty);
                    case -2:
                        EditorUtility.DisplayDialog("API 업데이트 실패", "작업이 취소되었습니다.", "확인");
                        return (false, string.Empty);
                    case -1:
                        EditorUtility.DisplayDialog("API 업데이트 실패", "알 수 없는 오류가 발생했습니다.", "확인");
                        return (false, string.Empty);
                }
            }
#endif

            EditorUtility.DisplayProgressBar("Cloning proto files",
                "Cloning proto files from remote repository... (Wait up to 30 seconds)", 1f);
            var output = new List<string>();
            var task = Task.Run(() =>
            {
                try
                {
                    cts.Token.ThrowIfCancellationRequested();
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                    var fileName = "bash";
                    var args = $"\"{scriptPath}\" {branchName} {projName} {directoryPath} {protoCloneFolder}";
#else
                    var fileName = "powershell.exe";
                    var args = $"-NoExit -ExecutionPolicy Bypass -File \"{scriptPath}\" -BRANCH \"{branchName}\" -PROJ_NAME \"{projName}\" -TARGET_DIR \"{directoryPath}\" -CLONE_DIR \"{protoCloneFolder}\"";
#endif
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = args,
                        StandardOutputEncoding = Encoding.GetEncoding("EUC-KR"),
                        StandardErrorEncoding = Encoding.GetEncoding("EUC-KR"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Normal,
                        CreateNoWindow = false
                    };

                    using var process = new Process { StartInfo = startInfo };
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;
                        if (e.Data.Contains("Error"))
                        {
                            Debug.LogError(e.Data);
                            output.Add(e.Data);
                        }
                        else
                        {
                            Debug.Log(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Debug.LogWarning(e.Data);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    return process.ExitCode;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to execute git command: {e.Message}");
                    return -1;
                }
            }, cts.Token);

            int success;
            try
            {
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
                Task completedTask = await Task.WhenAny(task, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    Debug.LogWarning("[ProtoCloner] Task has timed out.");
                    cts.Cancel();
                    EditorUtility.ClearProgressBar();
                    success = -2;
                }
                else
                {
                    success = task.Result;
                    EditorUtility.ClearProgressBar();
                }
            }
            catch (OperationCanceledException)
            {
                EditorUtility.ClearProgressBar();
                success = -2;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}, {e.Message}");
                EditorUtility.ClearProgressBar();
                success = -1;
            }

            // 에러 처리
            if (success == 0)
            {
                HandleCloneOutputErrors(output, ref success);
            }
            else
            {
                HandleCloneExitCode(success);
            }

            if (success == 0)
            {
                FilterService(targetFolder, includeServices);
                ExcludeServices(targetFolder, excludeServices);

                var pathMapping = new Dictionary<string, string>();
                FlattenFoldersRecursively(targetFolder, targetFolder, string.Empty, pathMapping);
                UpdateProtoImportPaths(targetFolder, pathMapping);
                DeleteMetaFiles(targetFolder);

                AssetDatabase.ImportAsset(targetFolder, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }

            return (success == 0, targetFolder);
        }

        private static void HandleCloneOutputErrors(List<string> output, ref int success)
        {
            foreach (var msg in output)
            {
                switch (msg)
                {
                    case "Error: The target directory does not exist.":
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "Root Path를 제대로 입력해주세요.\n\n존재하지 않는 경로입니다.", "확인");
                        success = 2;
                        break;
                    case "Error: There is already a git repository in the target or clone directory.":
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "Root Path를 제대로 입력해주세요.\n\nRoot Path에 .git 폴더가 있으면 파일 보호를 위해 API 업데이트가 실행되지 않습니다.", "확인");
                        success = 3;
                        break;
                    case "Error: There is a .keep file in the target or clone directory.":
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "Root Path를 제대로 입력해주세요.\n\n.keep 파일이 있는 ClientSideProto 내에서는 API 업데이트가 실행되지 않습니다.", "확인");
                        success = 4;
                        break;
                    case "Error: Failed to clone the repository.":
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "원격 저장소 접근에 실패했습니다.\n\n- `git config user.email` 명령어를 입력하여 회사 계정인지 확인하세요.\n\n- SSH를 사용하는 경우 `ssh-add -l` 명령어를 입력하여 회사 계정이 표시되는지 확인하세요.\n\n- 표시되지 않는다면 `ssh-add {회사 계정 private key 파일 경로}`를 입력한 후 API 업데이트를 다시 수행하세요.", "확인");
                        success = 6;
                        break;
                    case "Error: Project directory not found in the repository.":
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "IDL Project Name을 제대로 입력해주세요.\n\n원격 IDL 저장소의 main 브랜치의 루트에 해당 폴더가 존재하지 않습니다.", "확인");
                        success = 7;
                        break;
                    case "Error: Failed to check the identities in the ssh-agent.":
                        EditorUtility.DisplayDialog("API 업데이트 실패",
                            "ssh-agent가 제대로 실행되지 않았습니다.\n\n잠시 후 다시 API 업데이트를 실행해 보세요.", "확인");
                        success = 8;
                        break;
                }

                if (msg.Contains("Error: No SSH key (id_ed25519 or id_rsa) found in the"))
                {
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "SSH 인증에 실패하였습니다.\n\n- ~/.ssh 폴더에 id_ed25519 또는 id_rsa 가 있어야 합니다.", "확인");
                    success = 5;
                }
                else if (msg.Contains("Error: IOException occurred while removing directory:"))
                {
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "I/O 오류가 발생했습니다.\n\n열려 있는 프로그램 및 파일들을 닫고 다시 시도하세요.", "확인");
                    success = 9;
                }
            }
        }

        private static void HandleCloneExitCode(int exitCode)
        {
            switch (exitCode)
            {
                case -1073741510:
                    Debug.LogError("Error: Shell window was closed by user.");
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "셸 윈도우가 강제 종료되었습니다.\n\n셸 창을 강제 종료하면 API 업데이트가 실행되지 않습니다.", "확인");
                    break;
                case -2:
                    EditorUtility.DisplayDialog("API 업데이트 실패", "작업이 취소되었습니다.", "확인");
                    break;
                default:
                    Debug.LogError($"Failed to clone proto files: {exitCode}");
                    break;
            }
        }

        private static void FilterService(string targetFolder, IEnumerable<string> services)
        {
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
                return;
            if (services == null)
                return;

            var serviceSet = new HashSet<string>(
                services.Where(s => !string.IsNullOrEmpty(s)),
                StringComparer.OrdinalIgnoreCase);
            if (serviceSet.Count == 0)
                return;

            foreach (string subdir in Directory.GetDirectories(targetFolder))
            {
                try
                {
                    string service = Path.GetFileName(subdir);
                    if (!serviceSet.Contains(service))
                        Directory.Delete(subdir, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"FilterService: failed to delete '{subdir}': {ex.Message}");
                }
            }
        }

        private static void ExcludeServices(string targetFolder, IEnumerable<string> services)
        {
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
                return;
            if (services == null)
                return;

            var serviceSet = new HashSet<string>(
                services.Where(s => !string.IsNullOrEmpty(s)),
                StringComparer.OrdinalIgnoreCase);
            if (serviceSet.Count == 0)
                return;

            foreach (string subdir in Directory.GetDirectories(targetFolder))
            {
                try
                {
                    string service = Path.GetFileName(subdir);
                    if (serviceSet.Contains(service))
                    {
                        Directory.Delete(subdir, true);
                        Debug.Log($"ExcludeServices: deleted '{subdir}'");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ExcludeServices: failed to delete '{subdir}': {ex.Message}");
                }
            }
        }

        private static void FlattenFoldersRecursively(
            string targetFolder, string currentFolder, string folderPrefix,
            Dictionary<string, string> pathMapping)
        {
            foreach (var subdirectory in Directory.GetDirectories(currentFolder))
            {
                var folderName = new DirectoryInfo(subdirectory).Name;
                var newPrefix = string.IsNullOrEmpty(folderPrefix) ? folderName : $"{folderPrefix}_{folderName}";
                FlattenFoldersRecursively(targetFolder, subdirectory, newPrefix, pathMapping);
            }

            if (targetFolder == currentFolder)
                return;

            foreach (var file in Directory.GetFiles(currentFolder))
            {
                var fileName = Path.GetFileName(file);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                var folderName = new DirectoryInfo(
                    Path.GetDirectoryName(file) ?? throw new InvalidOperationException()).Name;
                var destinationPath = Path.Combine(targetFolder, fileName);
                if (fileNameWithoutExt != folderName || File.Exists(destinationPath))
                {
                    fileName = $"{folderPrefix}+{fileName}";
                    destinationPath = Path.Combine(targetFolder, fileName);
                }

                File.Move(file, destinationPath);
                var targetLength = targetFolder.Length + 1;
                pathMapping.Add(
                    file.Substring(targetLength),
                    destinationPath.Substring(targetLength));
            }

            Directory.Delete(currentFolder, false);
        }

        private static void UpdateProtoImportPaths(
            string targetFolder, Dictionary<string, string> pathMapping)
        {
            foreach (var protoFile in Directory.GetFiles(targetFolder, "*.proto"))
            {
                var lines = File.ReadAllLines(protoFile);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Trim().StartsWith("import"))
                        continue;

                    var match = Regex.Match(lines[i], @"import\s+""(.+?)"";");
                    if (!match.Success)
                        continue;

                    var oldImportPath = match.Groups[1].Value;
                    var key = oldImportPath;
                    if (Path.DirectorySeparatorChar != '/' && key.Contains('/'))
                        key = key.Replace('/', Path.DirectorySeparatorChar);

                    if (pathMapping.TryGetValue(key, out var newImportPath))
                        lines[i] = lines[i].Replace(oldImportPath, newImportPath);
                }
                File.WriteAllLines(protoFile, lines);
            }
        }

        private static void DeleteMetaFiles(string targetFolder)
        {
            foreach (var file in Directory.GetFiles(targetFolder, "*.meta"))
                File.Delete(file);
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtoCloner.cs
git commit -m "feat: ProtoCloner 추가 — IDL clone/필터링/평탄화"
```

---

### Task 5: ProtoImporter.cs — 메인 오케스트레이터

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporter.cs`

원본: `Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/ProtoCSharpGenerator.cs` + `ProtoBufCleaner.cs`

- [ ] **Step 1: ProtoImporter.cs 작성**

패키지의 `ProtoCSharpGenerator.UserGenerateCSharpFromProtoFiles`를 포팅하되:
- 패키지 내부 proto 참조(`pkgProto`, `pkgCs`) 제거 — 프로젝트 전용이므로 불필요
- Cleanup 로직(ProtoBufCleaner) 통합: 코드 생성 후 `removeFileNames` 기반 파일 삭제
- AssetPostprocessor를 통한 자동 정리 유지

```csharp
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtoImporter
    {
        /// <summary>
        /// 설정 에셋 기반으로 API 업데이트를 수행합니다.
        /// clone → C# 생성 → cleanup → AssetDatabase 갱신
        /// </summary>
        public static async void RunFromSetting()
        {
            var setting = ProtoImporterSetting.GetOrCreateAsset();
            if (!setting.IsValid())
                return;

            try
            {
                await System.Threading.Tasks.Task.Yield();

                // 1. Library에 임시 폴더 생성
                string tempDir = Path.GetFullPath("Library/TempProto");
                ProtoFileUtil.EnsureDirectoryExists(tempDir);
                ProtoFileUtil.ClearDirectoryContents(tempDir);

                // 2. IDL 저장소 clone + 필터링 + 평탄화
                var result = await ProtoCloner.RunCloneScript(
                    setting.IdlProjectName, tempDir,
                    setting.IncludeServices, setting.ExcludeServices,
                    new CancellationTokenSource());

                if (!result.Result)
                {
                    Debug.LogError($"[ProtoImporter] git에서 {setting.IdlProjectName} proto 파일 복제에 실패했습니다.");
                    return;
                }

                // 3. C# 코드 생성
                string csDir = Path.GetFullPath(Path.Combine(tempDir, "Generated/CSharp"));
                ProtocRunner.GenerateCSharp(result.ProtoDir, csDir);

                // 4. 생성된 파일을 타겟 디렉토리로 복사
                string targetFullDir = Path.GetFullPath(setting.TargetDir);
                ProtoFileUtil.ClearDirectoryContents(Path.Combine(targetFullDir, "Generated/CSharp"));
                ProtoFileUtil.ClearDirectoryContents(Path.Combine(targetFullDir, "Generated/Proto"));
                ProtoFileUtil.CopyAllFilesToDirectory(tempDir, targetFullDir);

                // 5. Cleanup — 불필요 파일 삭제
                CleanupGeneratedFiles(setting);

                // 6. AssetDatabase 갱신
                AssetDatabase.Refresh();

                Debug.Log("[ProtoImporter] API 업데이트가 완료되었습니다.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 설정의 removeFileNames 기준으로 생성 폴더에서 불필요한 파일을 삭제합니다.
        /// </summary>
        internal static void CleanupGeneratedFiles(ProtoImporterSetting setting)
        {
            if (setting == null || setting.RemoveFileNames == null || setting.RemoveFileNames.Count == 0)
                return;

            string targetAssetPath = (setting.CleanupTargetDir ?? string.Empty).Replace("\\", "/").TrimEnd('/');
            if (string.IsNullOrEmpty(targetAssetPath))
                return;

            string targetFullPath = Path.GetFullPath(targetAssetPath).Replace("\\", "/");
            if (!Directory.Exists(targetFullPath))
                return;

            var matchers = BuildRemoveMatchers(setting.RemoveFileNames);
            if (matchers.Count == 0)
                return;

            bool deleted = false;
            foreach (var file in Directory.GetFiles(targetFullPath, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (!ShouldRemove(fileName, matchers))
                    continue;

                var assetPath = ToAssetPath(file, targetFullPath, targetAssetPath);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                if (AssetDatabase.DeleteAsset(assetPath))
                    deleted = true;
            }

            if (deleted)
            {
                AssetDatabase.Refresh();
                Debug.Log("[ProtoImporter] 불필요한 생성 파일을 삭제했습니다.");
            }
        }

        private static bool ShouldRemove(string fileName, List<Regex> matchers)
        {
            if (string.IsNullOrEmpty(fileName) || matchers == null || matchers.Count == 0)
                return false;
            return matchers.Any(regex => regex.IsMatch(fileName));
        }

        private static List<Regex> BuildRemoveMatchers(IEnumerable<string> names)
        {
            var matchers = new List<Regex>();
            if (names == null)
                return matchers;

            foreach (var raw in names)
            {
                var trimmed = raw?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                bool hasWildcard = trimmed.IndexOfAny(new[] { '*', '?' }) >= 0;
                if (hasWildcard)
                {
                    AddWildcardMatcher(matchers, trimmed);
                    AddWildcardMatcher(matchers, $"{trimmed}.meta");
                }
                else
                {
                    var baseName = Path.GetFileNameWithoutExtension(trimmed);
                    if (string.IsNullOrWhiteSpace(baseName))
                        continue;
                    AddExactMatcher(matchers, $"{baseName}.cs");
                    AddExactMatcher(matchers, $"{baseName}.cs.meta");
                }
            }
            return matchers;
        }

        private static void AddExactMatcher(List<Regex> matchers, string fileName)
        {
            matchers.Add(new Regex($"^{Regex.Escape(fileName)}$", RegexOptions.IgnoreCase));
        }

        private static void AddWildcardMatcher(List<Regex> matchers, string wildcard)
        {
            string pattern = "^" + Regex.Escape(wildcard)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            matchers.Add(new Regex(pattern, RegexOptions.IgnoreCase));
        }

        private static string ToAssetPath(string absolutePath, string targetFullPath, string targetAssetPath)
        {
            var normalizedFile = Path.GetFullPath(absolutePath).Replace("\\", "/");
            if (!normalizedFile.StartsWith(targetFullPath, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            var relative = normalizedFile.Substring(targetFullPath.Length).TrimStart('/');
            return $"{targetAssetPath}/{relative}".Replace("\\", "/");
        }

        /// <summary>
        /// AssetPostprocessor — 생성 폴더에 새 파일이 들어오면 자동으로 cleanup 수행
        /// </summary>
        internal class ProtoImporterPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
            {
                var setting = AssetDatabase.LoadAssetAtPath<ProtoImporterSetting>(ProtoImporterSetting.AssetPath);
                if (setting == null)
                    return;

                string targetAssetPath = (setting.CleanupTargetDir ?? string.Empty).Replace("\\", "/").TrimEnd('/');
                if (string.IsNullOrEmpty(targetAssetPath))
                    return;

                bool touched = importedAssets.Any(a =>
                        a.StartsWith(targetAssetPath, StringComparison.OrdinalIgnoreCase))
                    || movedAssets.Any(a =>
                        a.StartsWith(targetAssetPath, StringComparison.OrdinalIgnoreCase));
                if (touched)
                    CleanupGeneratedFiles(setting);
            }
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporter.cs
git commit -m "feat: ProtoImporter 추가 — 메인 오케스트레이터 (clone → generate → clean)"
```

---

## Chunk 3: 설정/UI/통합

### Task 6: ProtoImporterSetting.cs — 통합 설정 에셋

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSetting.cs`

원본: `NetLiteAsset.cs` + `ProtoBufCleanupSetting.cs`

- [ ] **Step 1: ProtoImporterSetting.cs 작성**

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    internal class ProtoImporterSetting : ScriptableObject
    {
        internal const string AssetPath = "Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSetting.asset";

        [Header("Import 설정")]
        [Tooltip("IDL 저장소의 프로젝트 이름")]
        [SerializeField] private string _idlProjectName = "bm013-grpc";

        [Tooltip("생성된 코드가 저장될 경로 (Asset 경로)")]
        [SerializeField] private string _targetDir = "Assets/_Project/Scripts/gRPC/bm013-grpc";

        [Tooltip("포함할 서비스 목록 (비어있으면 전체 포함)")]
        [SerializeField] private string[] _includeServices = {
            "auth", "lobby", "spec", "player", "player_data", "inventory_flow", "shop"
        };

        [Tooltip("제외할 서비스 목록")]
        [SerializeField] private string[] _excludeServices = { "health" };

        [Header("Cleanup 설정")]
        [Tooltip("Cleanup 대상 폴더 (Asset 경로). 비어있으면 TargetDir/Generated/CSharp 사용")]
        [SerializeField] private string _cleanupTargetDir = "Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp";

        [Tooltip("삭제할 파일 이름 목록 (.cs 확장자 생략 가능, * 와일드카드 지원)")]
        [SerializeField] private List<string> _removeFileNames = new();

        public string IdlProjectName => _idlProjectName;
        public string TargetDir => _targetDir;
        public string[] IncludeServices => _includeServices;
        public string[] ExcludeServices => _excludeServices;
        public string CleanupTargetDir =>
            string.IsNullOrEmpty(_cleanupTargetDir) ? $"{_targetDir}/Generated/CSharp" : _cleanupTargetDir;
        public List<string> RemoveFileNames => _removeFileNames;

        public bool IsValid()
        {
            bool valid = true;
            if (string.IsNullOrEmpty(_idlProjectName))
            {
                Debug.LogError("[ProtoImporter] IDL Project Name이 비어있습니다.");
                valid = false;
            }
            if (string.IsNullOrEmpty(_targetDir))
            {
                Debug.LogError("[ProtoImporter] Target Dir이 비어있습니다.");
                valid = false;
            }
            return valid;
        }

        public static ProtoImporterSetting GetOrCreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ProtoImporterSetting>(AssetPath);
            if (asset != null)
                return asset;

            asset = CreateInstance<ProtoImporterSetting>();
            string dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir!);
                AssetDatabase.Refresh();
            }
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSetting.cs
git commit -m "feat: ProtoImporterSetting 추가 — 통합 설정 에셋"
```

---

### Task 7: ProtoImporterSettingEditor.cs — 커스텀 인스펙터

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSettingEditor.cs`

원본: `Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/Asset/NetLiteAssetEditor.cs`

- [ ] **Step 1: ProtoImporterSettingEditor.cs 작성**

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    [CustomEditor(typeof(ProtoImporterSetting))]
    internal class ProtoImporterSettingEditor : UnityEditor.Editor
    {
        private GUIStyle _buttonStyle;

        private GUIStyle ButtonStyle =>
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
            };

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("API 업데이트", ButtonStyle))
            {
                ProtoImporter.RunFromSetting();
            }

            if (GUILayout.Button("저장소 리스트", ButtonStyle))
            {
                Application.OpenURL("https://github.com/cookapps-devops/hive-grpc-IDL");
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Cleanup 수동 실행", GUILayout.Height(24)))
            {
                var setting = (ProtoImporterSetting)target;
                ProtoImporter.CleanupGeneratedFiles(setting);
            }

            if (!EditorGUI.EndChangeCheck())
                return;

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSettingEditor.cs
git commit -m "feat: ProtoImporterSettingEditor 추가 — 커스텀 인스펙터"
```

---

### Task 8: ProtoImporterWindow.cs — CookApps Window 탭 등록

**Files:**
- Create: `Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterWindow.cs`

원본: `Library/PackageCache/com.cookapps.net.lite@1d136ffc41d1/Editor/Window/PackageWindow.cs`

- [ ] **Step 1: ProtoImporterWindow.cs 작성**

`CookAppsPackageWindow.Add()`에 에셋 경로를 전달하면 탭 클릭 시 해당 SO의 커스텀 인스펙터가 자동으로 표시됩니다.

```csharp
#if UNITY_EDITOR
using CookApps.Package.Window.Editor;
using UnityEditor;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtoImporterWindow
    {
        [InitializeOnLoadMethod]
        private static void RegisterTab()
        {
            CookAppsPackageWindow.Add(
                "Proto Importer",
                ProtoImporterSetting.AssetPath,
                () => ProtoImporterSetting.GetOrCreateAsset());
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterWindow.cs
git commit -m "feat: ProtoImporterWindow 추가 — CookApps Window 탭 등록"
```

---

### Task 9: 기존 ProtoBufCleaner 파일 삭제 + 기존 설정 에셋 삭제

**Files:**
- Delete: `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleaner.cs`
- Delete: `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.cs`
- Delete: `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.asset`
- Delete: `Assets/CookApps/Editor/NetLiteAsset.asset` (새 설정으로 대체)
- Delete: 각 파일의 `.meta` 파일도 함께 삭제

- [ ] **Step 1: ProtoBufCleaner 관련 파일 삭제**

```bash
git rm Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleaner.cs
git rm Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleaner.cs.meta
git rm Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.cs
git rm Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.cs.meta
git rm Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.asset
git rm Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.asset.meta
```

- [ ] **Step 2: NetLiteAsset.asset 삭제**

NetLite 패키지의 `PackageWindow.cs`가 이 에셋을 참조하므로, 패키지가 존재하는 동안에는 패키지가 탭을 재생성할 수 있음. 하지만 내재화 후에는 프로젝트의 `ProtoImporterWindow`가 탭을 관리하므로 이 에셋은 불필요.

```bash
git rm Assets/CookApps/Editor/NetLiteAsset.asset
git rm Assets/CookApps/Editor/NetLiteAsset.asset.meta
```

> **주의:** NetLite 패키지의 `PackageWindow.cs`가 `[InitializeOnLoadMethod]`에서 `NetLiteAsset.GetOrCreateAsset()`을 호출하므로, 패키지가 설치된 상태에서는 에셋이 재생성될 수 있음. 이 경우:
> - 패키지에서 NetLite 탭이 별도로 뜨더라도 무해함 (설정만 비어있을 뿐)
> - 또는 패키지를 제거하면 완전히 정리됨

- [ ] **Step 3: Commit**

```bash
git commit -m "refactor: ProtoBufCleaner/NetLiteAsset 삭제 — ProtoImporter로 통합"
```

---

### Task 10: 최종 검증

- [ ] **Step 1: Unity Editor에서 컴파일 확인**

Unity Editor를 열어 컴파일 에러가 없는지 확인.

- [ ] **Step 2: CookApps Window에서 "Proto Importer" 탭 확인**

`CookApps > Show Window` (Ctrl+Shift+Alt+W) 메뉴로 CookApps Window를 열고:
- "Proto Importer" 탭이 표시되는지 확인
- 탭 클릭 시 `ProtoImporterSetting` 인스펙터가 올바르게 표시되는지 확인
- 기본값이 올바르게 설정되어 있는지 확인 (idlProjectName: `bm013-grpc`, targetDir: `Assets/_Project/Scripts/gRPC/bm013-grpc`)

- [ ] **Step 3: API 업데이트 버튼 동작 확인**

"API 업데이트" 버튼을 클릭하여:
- proto 파일 clone이 정상 수행되는지
- C# 코드가 `Generated/CSharp`에 생성되는지
- proto 파일이 `Generated/Proto`에 배치되는지

- [ ] **Step 4: Cleanup 동작 확인**

`removeFileNames`에 테스트 패턴(예: `*Reflection*`)을 추가하고 "Cleanup 수동 실행" 버튼으로 정상 동작 확인.

- [ ] **Step 5: 최종 Commit**

```bash
git add -A
git commit -m "feat: Proto Importer 내재화 완료 — NetLite API 업데이트 + ProtoBufCleaner 통합"
```
