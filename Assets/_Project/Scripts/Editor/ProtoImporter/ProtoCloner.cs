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
                UnityEngine.Debug.LogError("Failed to get git remote origin URL: " + e.Message);
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
            UnityEngine.Debug.Log(projInfo.IsSsh ? "Use SSH" : "Use HTTPS");

            const string protoCloneFolder = "Generated/Proto";
            string targetFolder = Path.Combine(rootFolder, protoCloneFolder, projName);
            ProtoFileUtil.EnsureDirectoryExists(targetFolder);
            ProtoFileUtil.ClearDirectoryContents(targetFolder);

            var protoProjDirInfo = new DirectoryInfo(targetFolder);
            if (projName != protoProjDirInfo.Name)
            {
                UnityEngine.Debug.LogError($"IDL project name mismatch: {projName} != {protoProjDirInfo.Name}");
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
                    UnityEngine.Debug.LogError($"Failed to write ssh config file: {e.Message}");
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
                        UnityEngine.Debug.LogError($"Failed to execute ssh command: {e.Message}");
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
                    UnityEngine.Debug.LogError($"Error: {e}, {e.Message}");
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
                            UnityEngine.Debug.LogError(e.Data);
                            output.Add(e.Data);
                        }
                        else
                        {
                            UnityEngine.Debug.Log(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            UnityEngine.Debug.LogWarning(e.Data);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    return process.ExitCode;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to execute git command: {e.Message}");
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
                    UnityEngine.Debug.LogWarning("[ProtoCloner] Task has timed out.");
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
                UnityEngine.Debug.LogError($"Error: {e}, {e.Message}");
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
                    UnityEngine.Debug.LogError("Error: Shell window was closed by user.");
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "셸 윈도우가 강제 종료되었습니다.\n\n셸 창을 강제 종료하면 API 업데이트가 실행되지 않습니다.", "확인");
                    break;
                case -2:
                    EditorUtility.DisplayDialog("API 업데이트 실패", "작업이 취소되었습니다.", "확인");
                    break;
                default:
                    UnityEngine.Debug.LogError($"Failed to clone proto files: {exitCode}");
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
                    UnityEngine.Debug.LogError($"FilterService: failed to delete '{subdir}': {ex.Message}");
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
                        UnityEngine.Debug.Log($"ExcludeServices: deleted '{subdir}'");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"ExcludeServices: failed to delete '{subdir}': {ex.Message}");
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
