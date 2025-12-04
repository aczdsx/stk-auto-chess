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

namespace CookApps.NetLite.Editor
{
    internal enum ProtoCompareVersionResult
    {
        BranchMismatch,
        NeedUpdate,
        UpToDate,
        RemoteNotFound,
    }

    internal static class ProtoScriptRunner
    {
        private const string ShellScriptFolder = "ShellScripts";
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        private const string protoCloneSshScript = "protos_clone_ssh.sh";
        private const string protoCloneHttpsScript = "protos_clone_https.sh";
#else
        private const string protoCloneSshScript = "protos_clone_ssh.ps1";
        private const string protoCloneHttpsScript = "protos_clone_https.ps1";
#endif
        private const string VersionFileName = "version.txt";

        struct GitProjectInfo
        {
            public bool IsSsh;
            public string RepositoryName;

            public GitProjectInfo(bool isSsh, string repoName)
            {
                IsSsh = isSsh;
                RepositoryName = repoName;
            }


        }

        private static GitProjectInfo GetGitProjectInfo()
        {
            string url = null;
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "config --get remote.origin.url",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Application.dataPath // Unity 프로젝트의 Assets 폴더 경로
                };

                using (Process process = Process.Start(startInfo))
                {
                    using (StreamReader reader = process?.StandardOutput)
                    {
                        url = reader?.ReadToEnd().Trim();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to get git remote origin URL: " + e.Message);
            }

            // 레포 이름 알아내기
            if (string.IsNullOrEmpty(url))
                return new (false, string.Empty);

            var segments = url.Split('/');
            if (segments.Length == 0)
                return new (false, string.Empty);

            var repoName = segments[^1];
            return new (url.Contains("git@"), repoName.EndsWith(".git") ? repoName[..^4] : repoName);
        }

        /// hive IDL 저장소에서 proto 파일을 클론하는 스크립트를 실행합니다.
        /// 이후 서비스별 필터링, 폴더 평탄화, import 경로 수정 작업을 수행합니다.
        public static async Task<(bool Result, string ProtoDir)> RunCloneScript(string projName, string rootFolder, IEnumerable<string> services, CancellationTokenSource cts)
        {
            string branchName = "main";
            // var scriptFolder = GetScriptFolder();
            ProcessHelper.TryGetProtocRootDir(out string scriptFolder);

            string scriptPath;
            var projInfo = GetGitProjectInfo();
            var isSsh = projInfo.IsSsh;
            if (isSsh)
            {
                scriptPath = Path.Combine(scriptFolder, "ProtoSync", ShellScriptFolder, protoCloneSshScript);
                Debug.Log("Use SSH");
            }
            else
            {
                scriptPath = Path.Combine(scriptFolder, "ProtoSync", ShellScriptFolder, protoCloneHttpsScript);
                Debug.Log("Use HTTPS");
            }

            var protoCloneFolder = "Generated/Proto";
            var targetFolder = Path.Combine(rootFolder, protoCloneFolder, projName);
            FileUtil.EnsureDirectoryExists(targetFolder);
            FileUtil.ClearDirectoryContents(targetFolder);
            var protoProjDirInfo = new DirectoryInfo(targetFolder);
            if (projName != protoProjDirInfo.Name)
            {
                Debug.LogError($"IDL project name mismatch: {projName} != {protoProjDirInfo.Name}");
                return (false, string.Empty);
            }
            var directoryPath = new DirectoryInfo(rootFolder).FullName;  // ProtoRoot의 절대 경로


#if UNITY_EDITOR_WIN
            if (isSsh)
            {
                // Windows에서 SSH를 사용할 때, SSH 키를 등록하고 ssh-agent를 사용하도록 설정 (관리자 권한으로 PowerShell 실행)
                string sshConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".ssh", "config");
                string sshConfigContent = "Host *\n  AddKeysToAgent yes\n  IdentitiesOnly yes";

                // sshConfigPath에 파일이 있으면 넘어가고, 없으면 sshConfigContent 내용을 작성하여 파일 생성
                EditorUtility.DisplayProgressBar("Activate ssh-agent", "Activating ssh-agent...", 1f);
                try
                {
                    if (!File.Exists(sshConfigPath))
                    {
                        await File.WriteAllTextAsync(sshConfigPath, sshConfigContent);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write ssh config file: {e.Message}");
                }

                string scriptForSettingSshPath = Path.Combine(scriptFolder, ShellScriptFolder, "ssh_setting.ps1");

                var taskForSettingSsh = Task.Run(() =>
                {
                    cts.Token.ThrowIfCancellationRequested();
                    try
                    {
                        var fileName = "powershell.exe";
                        var args = $"-NoExit -ExecutionPolicy Bypass -File \"{scriptForSettingSshPath}\"";
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = fileName,
                            Arguments = args,
                            RedirectStandardOutput = false,
                            RedirectStandardError = false,
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = false,
                        };

                        using var process = new Process();
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();

                        return process.ExitCode;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to execute ssh command: {e.Message}");
                        return 1;
                    }
                    finally
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                        }
                    }
                }, cts.Token);

                int sshSuccess;
                try
                {
                    sshSuccess = await taskForSettingSsh;
                    EditorUtility.ClearProgressBar();
                }
                catch (OperationCanceledException e)
                {
                    Debug.Log($"Task has cancelled: {e.Message}");
                    EditorUtility.ClearProgressBar();
                    sshSuccess = -2;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error: {e}, {e.Message}");
                    EditorUtility.ClearProgressBar();
                    sshSuccess = -1;
                }

                if (sshSuccess == 1)
                {
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "Windows PowerShell을 관리자 권한으로 실행해야 합니다.\n\n안전한 SSH 접속을 위한 과정입니다. 다시 시도할 때에는 \"예\" 버튼을 눌러주세요.",
                        "확인");
                    return (false, string.Empty);
                }
                if (sshSuccess == -2)
                {
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "작업이 취소되었습니다.",
                        "확인");
                    return (false, string.Empty);
                }
                if (sshSuccess == -1)
                {
                    EditorUtility.DisplayDialog("API 업데이트 실패",
                        "알 수 없는 오류가 발생했습니다.",
                        "확인");
                    return (false, string.Empty);
                }
            }
#endif

            Debug.Log($"-NoExit -ExecutionPolicy Bypass -File \"{scriptPath}\" -BRANCH \"{branchName}\" -PROJ_NAME \"{projName}\" -TARGET_DIR \"{directoryPath}\" -CLONE_DIR \"{protoCloneFolder}\"");

            EditorUtility.DisplayProgressBar("Cloning proto files", "Cloning proto files from remote repository... (Wait up to 30 seconds)", 1f);
            List<string> output = new List<string>();
            var task = Task.Run(() =>
            {
                try
                {
                    cts.Token.ThrowIfCancellationRequested();

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                    var fileName = "bash";
                    var args =
 $"\"{scriptPath}\" {branchName} {projName} {directoryPath} {protoCloneFolder}";
#else
                    var fileName = "powershell.exe";
                    var args =
                        $"-NoExit -ExecutionPolicy Bypass -File \"{scriptPath}\" -BRANCH \"{branchName}\" -PROJ_NAME \"{projName}\" -TARGET_DIR \"{directoryPath}\" -CLONE_DIR \"{protoCloneFolder}\"";
#endif
                    ProcessStartInfo startInfo = new ProcessStartInfo
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

                    using var process = new Process();
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.Contains("Error"))
                            {
                                Debug.LogError(e.Data);
                                output.Add(e.Data);
                            }
                            else
                            {
                                Debug.Log(e.Data);
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.LogWarning(e.Data);
                        }
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
                finally
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                    }
                }
            }, cts.Token);

            int success;
            try
            {
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
                Task completedTask = await Task.WhenAny(task, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    Debug.LogWarning("[Editor] Task has timed out.");
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
            catch (OperationCanceledException e)
            {
                Debug.Log($"Task has cancelled: {e.Message}");
                EditorUtility.ClearProgressBar();
                success = -2;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}, {e.Message}");
                EditorUtility.ClearProgressBar();
                success = -1;
            }

            switch (success)
            {
                case 0:
                    foreach (var outputMessage in output)
                    {
                        switch (outputMessage)
                        {
                            case "Error: The target directory does not exist.":
                                EditorUtility.DisplayDialog("API 업데이트 실패", "Root Path를 제대로 입력해주세요.\n\n존재하지 않는 경로입니다.", "확인");
                                success = 2;
                                break;
                            case "Error: There is already a git repository in the target or clone directory.":
                                EditorUtility.DisplayDialog("API 업데이트 실패", "Root Path를 제대로 입력해주세요.\n\nRoot Path에 .git 폴더가 있으면 파일 보호를 위해 API 업데이트가 실행되지 않습니다.", "확인");
                                success = 3;
                                break;
                            case "Error: There is a .keep file in the target or clone directory.":
                                EditorUtility.DisplayDialog("API 업데이트 실패", "Root Path를 제대로 입력해주세요.\n\n.keep 파일이 있는 ClientSideProto 내에서는 API 업데이트가 실행되지 않습니다.", "확인");
                                success = 4;
                                break;
                            case "Error: Failed to clone the repository.":
                                EditorUtility.DisplayDialog("API 업데이트 실패",
                                    "원격 저장소 접근에 실패했습니다.\n\n- `git config user.email` 명령어를 입력하여 회사 계정인지 확인하세요.\n\n- SSH를 사용하는 경우 `ssh-add -l` 명령어를 입력하여 회사 계정이 표시되는지 확인하세요.\n\n- 표시되지 않는다면 `ssh-add {회사 계정 private key 파일 경로}`를 입력한 후 API 업데이트를 다시 수행하세요.", "확인");
                                success = 6;
                                break;
                            case "Error: Project directory not found in the repository.":
                                EditorUtility.DisplayDialog("API 업데이트 실패", "IDL Project Name을 제대로 입력해주세요.\n\n원격 IDL 저장소의 main 브랜치의 루트에 해당 폴더가 존재하지 않습니다.", "확인");
                                success = 7;
                                break;
                            case "Error: Failed to check the identities in the ssh-agent.":
                                EditorUtility.DisplayDialog("API 업데이트 실패", "ssh-agent가 제대로 실행되지 않았습니다.\n\n잠시 후 다시 API 업데이트를 실행해 보세요.\n그래도 같은 오류가 반복되면 매뉴얼을 참고하세요.", "확인");
                                success = 8;
                                break;
                        }
                        if (outputMessage.Contains("Error: No SSH key (id_ed25519 or id_rsa) found in the"))
                        {
                            EditorUtility.DisplayDialog("API 업데이트 실패", "SSH 인증에 실패하였습니다.\n\n- ~/.ssh 폴더에 id_ed25519 또는 id_rsa 가 있어야 합니다.\n\n- 없다면 https://github.com/cookapps-devops/tech-grpc/blob/0048470fe18d6edb41fef35e93611587c530f84d/Assets/Package/Documentation~/manual/ssh_keygen.md 을 읽어보세요.\n\n- 있다면 `ssh-add {회사 계정 private key 파일 경로}`를 입력한 후 API 업데이트를 다시 수행하세요.", "확인");
                            success = 5;
                        }
                        else if (outputMessage.Contains("Error: IOException occurred while removing directory:"))
                        {
                            EditorUtility.DisplayDialog("API 업데이트 실패", "I/O 오류가 발생했습니다.\n\n열려 있는 프로그램 및 파일들을 닫고 다시 시도하세요.", "확인");
                            success = 9;
                        }
                    }
                    break;
                case -1073741510:
                    Debug.LogError("Error: Shell window was closed by user.");
                    EditorUtility.DisplayDialog("API 업데이트 실패", "셸 윈도우가 강제 종료되었습니다.\n\n셸 창을 강제 종료하면 API 업데이트가 실행되지 않습니다.", "확인");
                    break;
                case -2:
                    EditorUtility.DisplayDialog("API 업데이트 실패", "작업이 취소되었습니다.", "확인");
                    break;
                default:
                    Debug.LogError($"Failed to clone proto files: {success}");
                    break;
            }

            if (success == 0)
            {
                var pathMapping = new Dictionary<string, string>();
                FilterService(targetFolder, services);
                ExcludeServices(targetFolder, Defines.ExcludeDefaultServices);
                FlattenFoldersRecursively(targetFolder, targetFolder, string.Empty, pathMapping);
                UpdateProtoImportPaths(targetFolder, pathMapping);
                DeleteMetaFiles(targetFolder);

                AssetDatabase.ImportAsset(targetFolder, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }


            return (success == 0, targetFolder);
        }

        public static (ProtoCompareVersionResult result, string msg) CompareVersion(string localGeneratedFolder, string remoteClonedFolder)
        {
            string localVersionFile = Path.Combine(localGeneratedFolder, VersionFileName);
            string remoteVersionFile = Path.Combine(remoteClonedFolder, VersionFileName);

            if (!File.Exists(remoteVersionFile))
            {
                return (ProtoCompareVersionResult.RemoteNotFound, "Remote version file not found.");
            }

            if (!File.Exists(localVersionFile))
            {
                return (ProtoCompareVersionResult.NeedUpdate, "Local version file not found.");
            }

            var localVersionContent = File.ReadAllLines(localVersionFile);
            var remoteVersionContent = File.ReadAllLines(remoteVersionFile);
            if (remoteVersionContent.Length < 2)
            {
                return (ProtoCompareVersionResult.RemoteNotFound, "Remote version file is invalid.");
            }
            if (localVersionContent.Length < 2)
            {
                return (ProtoCompareVersionResult.NeedUpdate, "Local version file is invalid.");
            }

            if (localVersionContent[0] != remoteVersionContent[0])
            {
                return (ProtoCompareVersionResult.BranchMismatch, $"Branch mismatch. {localVersionContent[0]} {remoteVersionContent[0]}");
            }

            if (!int.TryParse(localVersionContent[1], out var localVersion))
            {
                return (ProtoCompareVersionResult.NeedUpdate, "Local version is invalid.");
            }

            if (!int.TryParse(remoteVersionContent[1], out var remoteVersion))
            {
                return (ProtoCompareVersionResult.RemoteNotFound, "Remote version is invalid.");
            }

            if (localVersion < remoteVersion)
            {
                return (ProtoCompareVersionResult.NeedUpdate, $"Local version {localVersion} is older than remote version {remoteVersion}.");
            }

            return (ProtoCompareVersionResult.UpToDate, "Local version is up-to-date.");
        }

        /// <summary>
        /// 타겟 폴더의 직계 하위 폴더들을 services 목록에 따라 필터링하여 삭제한다.
        /// services가 null이거나 비어있으면 필터를 수행하지 않는다 (아무 것도 삭제하지 않음).
        /// </summary>
        /// <param name="targetFolder">필터 대상이 되는 최상위 폴더 경로</param>
        /// <param name="services">유지할 서비스 이름 목록</param>
        private static void FilterService(string targetFolder, IEnumerable<string> services)
        {
            // targetFolder가 유효하지 않으면 종료
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
            {
                Debug.LogWarning($"FilterService: targetFolder is null or does not exist: {targetFolder}");
                return;
            }

            // services가 null이거나 비어있으면 필터링을 수행하지 않음
            if (services == null)
            {
                Debug.Log($"FilterService: services is null - skipping filtering for '{targetFolder}'");
                return;
            }

            // 여러 번 열거하지 않도록 HashSet으로 변환 (대소문자 무시)
            var serviceSet = new HashSet<string>(services.Where(s => !string.IsNullOrEmpty(s)), StringComparer.OrdinalIgnoreCase);
            if (serviceSet.Count == 0)
            {
                Debug.Log($"FilterService: services is empty - skipping filtering for '{targetFolder}'");
                return;
            }

            try
            {
                // targetFolder의 직계 하위 폴더들만 가져와서 services 목록에 없는 폴더만 삭제
                var subdirs = Directory.GetDirectories(targetFolder);
                foreach (string subdir in subdirs)
                {
                    try
                    {
                        string service = Path.GetFileName(subdir);
                        // 유지할 서비스 목록에 포함된 폴더는 건너뜀
                        if (serviceSet.Contains(service))
                            continue;

                        // 하위 폴더 및 내용 전체 삭제 (보호 파일 무시)
                        Directory.Delete(subdir, true);
                    }
                    catch (Exception ex)
                    {
                        // 삭제 실패 시 에러 로그
                        Debug.LogError($"FilterService: failed to delete '{subdir}': {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"FilterService: error while filtering services in '{targetFolder}': {e.Message}");
            }
        }

        /// <summary>
        /// 타겟 폴더의 직계 하위 폴더들을 services 목록에 따라 제외하여 삭제한다.
        /// services가 null이거나 비어있으면 제외를 수행하지 않는다 (아무 것도 삭제하지 않음).
        /// </summary>
        /// <param name="targetFolder">제외 대상이 되는 최상위 폴더 경로</param>
        /// <param name="services">제외할 서비스 이름 목록</param>
        private static void ExcludeServices(string targetFolder, IEnumerable<string> services)
        {
            // targetFolder가 유효하지 않으면 종료
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
            {
                Debug.LogWarning($"ExcludeServices: targetFolder is null or does not exist: {targetFolder}");
                return;
            }

            // services가 null이거나 비어있으면 제외를 수행하지 않음
            if (services == null)
            {
                Debug.Log($"ExcludeServices: services is null - skipping exclusion for '{targetFolder}'");
                return;
            }

            // 여러 번 열거하지 않도록 HashSet으로 변환 (대소문자 무시)
            var serviceSet = new HashSet<string>(services.Where(s => !string.IsNullOrEmpty(s)), StringComparer.OrdinalIgnoreCase);
            if (serviceSet.Count == 0)
            {
                Debug.Log($"ExcludeServices: services is empty - skipping exclusion for '{targetFolder}'");
                return;
            }

            try
            {
                // targetFolder의 직계 하위 폴더들만 가져와서 services 목록에 있는 폴더만 삭제
                var subdirs = Directory.GetDirectories(targetFolder);
                foreach (string subdir in subdirs)
                {
                    try
                    {
                        string service = Path.GetFileName(subdir);
                        // 제외할 서비스 목록에 포함된 폴더만 삭제
                        if (!serviceSet.Contains(service))
                            continue;

                        // 하위 폴더 및 내용 전체 삭제 (보호 파일 무시)
                        Directory.Delete(subdir, true);
                        Debug.Log($"ExcludeServices: deleted '{subdir}'");
                    }
                    catch (Exception ex)
                    {
                        // 삭제 실패 시 에러 로그
                        Debug.LogError($"ExcludeServices: failed to delete '{subdir}': {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ExcludeServices: error while excluding services in '{targetFolder}': {e.Message}");
            }
        }

        private static void FlattenFoldersRecursively(string targetFolder, string currentFolder, string folderPrefix, Dictionary<string, string> pathMapping)
        {
            var subdirectories = Directory.GetDirectories(currentFolder);

            foreach (var subdirectory in subdirectories)
            {
                var folderName = new DirectoryInfo(subdirectory).Name;
                var newPrefix = string.IsNullOrEmpty(folderPrefix) ? folderName : $"{folderPrefix}_{folderName}";
                FlattenFoldersRecursively(targetFolder, subdirectory, newPrefix, pathMapping);
            }

            if (targetFolder == currentFolder)
                return;

            var files = Directory.GetFiles(currentFolder);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                var folderName = new DirectoryInfo(Path.GetDirectoryName(file) ?? throw new InvalidOperationException()).Name;
                var destinationPath = Path.Combine(targetFolder, fileName);
                if (fileNameWithoutExt != folderName || File.Exists(destinationPath))
                {
                    fileName = $"{folderPrefix}+{fileName}";
                    destinationPath = Path.Combine(targetFolder, fileName);
                }

                File.Move(file, destinationPath);
                var targetLength = targetFolder.Length + 1;
                pathMapping.Add(file.Substring(targetLength), destinationPath.Substring(targetLength));
            }

            Directory.Delete(currentFolder, false);
        }

        private static void DeleteMetaFiles(string targetFolder)
        {
            var files = Directory.GetFiles(targetFolder, "*.meta");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        static void UpdateProtoImportPaths(string targetFolder, Dictionary<string, string> pathMapping)
        {
            var protoFiles = Directory.GetFiles(targetFolder, "*.proto");

            foreach (var protoFile in protoFiles)
            {
                var lines = File.ReadAllLines(protoFile);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("import"))
                    {
                        var match = Regex.Match(lines[i], @"import\s+""(.+?)"";");
                        if (match.Success)
                        {
                            var oldImportPath = match.Groups[1].Value;
                            var key = oldImportPath;
                            if (Path.DirectorySeparatorChar != '/' && key.Contains('/'))
                            {
                                key = key.Replace('/', Path.DirectorySeparatorChar);
                            }

                            if (pathMapping.TryGetValue(key, out var newImportPath))
                            {
                                lines[i] = lines[i].Replace(oldImportPath, newImportPath);
                            }
                        }
                    }
                }

                File.WriteAllLines(protoFile, lines);
            }
        }
    }
}
