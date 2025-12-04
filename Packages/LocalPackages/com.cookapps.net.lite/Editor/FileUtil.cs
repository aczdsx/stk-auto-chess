/*
* Copyright (c) CookApps.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CookApps.NetLite.Editor
{
    internal static class FileUtil
    {
        /// <summary>
        /// sourceDir의 모든 파일과 폴더를 targetDir로 복사합니다.
        /// targetDir이 이미 존재하면 전체를 삭제 후 복사합니다.
        /// </summary>
        /// <param name="sourceDir">복사할 소스 디렉토리 경로</param>
        /// <param name="targetDir">복사 대상 타겟 디렉토리 경로</param>
        public static void OverwriteCopyDirectory(string sourceDir, string targetDir)
        {
            // 타겟 폴더가 이미 있으면 통째로 삭제
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true); // true = 하위 포함 전체 삭제

            // 타겟 폴더 생성
            Directory.CreateDirectory(targetDir);

            // 파일 복사
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            // 서브 폴더 복사 (재귀)
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destDir = Path.Combine(targetDir, dirName);
                OverwriteCopyDirectory(dir, destDir);
            }
        }

        /// <summary>
        /// 지정한 경로에 폴더가 없으면 하위 폴더까지 모두 생성합니다.
        /// </summary>
        /// <param name="directoryPath">생성할 폴더 경로</param>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            // 폴더가 존재하지 않으면 하위 폴더까지 모두 생성
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// 지정한 root 경로 하위의 모든 디렉토리 경로를 재귀적으로 리스트로 반환합니다.
        /// 예외 발생 시 빈 리스트를 반환합니다.
        /// </summary>
        /// <param name="root">탐색을 시작할 루트 디렉토리 경로</param>
        /// <returns>모든 하위 디렉토리 경로 리스트</returns>
        public static List<string> GetAllDirectories(string root)
        {
            var result = new List<string>();

            try
            {
                foreach (var dir in Directory.GetDirectories(root))
                {
                    result.Add(dir);
                    result.AddRange(GetAllDirectories(dir));   // 재귀 호출
                }
            }
            catch
            {
                // ignored
            }
            return result;
        }

        /// <summary>
        /// 지정한 directoryPath가 존재하면 그 폴더 내부의 모든 파일과 하위 디렉토리를 삭제한다.
        /// 이 메서드는 대상 폴더 자체는 삭제하지 않는다.
        /// </summary>
        /// <param name="directoryPath">정리할 디렉토리 경로</param>
        public static void ClearDirectoryContents(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;

            if (!Directory.Exists(directoryPath))
                return;

            try
            {
                // 최상위 파일 삭제
                foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // 삭제 실패는 무시하고 다음 파일로 진행
                    }
                }

                // 하위 디렉토리 삭제 (재귀 포함 전체 삭제)
                foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                        // 삭제 실패는 무시
                    }
                }
            }
            catch
            {
                // 최상위 예외는 무시
            }
        }

        /// <summary>
        /// 지정한 directoryPath 하위에서 fileNames에 해당하는 파일만 삭제합니다.
        /// 존재하지 않는 파일은 무시합니다.
        /// </summary>
        /// <param name="directoryPath">파일을 삭제할 디렉토리 경로</param>
        /// <param name="fileNames">삭제할 파일명 목록 (IEnumerable<string>)</param>
        public static void DeleteFilesInDirectory(string directoryPath, IEnumerable<string> fileNames)
        {
            // directoryPath가 null 또는 빈 문자열이면 바로 반환
            if (string.IsNullOrEmpty(directoryPath))
                return;

            // 폴더가 존재하지 않으면 바로 반환
            if (!Directory.Exists(directoryPath))
                return;

            // fileNames가 null이면 바로 반환
            if (fileNames == null)
                return;

            foreach (var fileName in fileNames)
            {
                // 파일명 null 또는 빈 문자열은 무시
                if (string.IsNullOrEmpty(fileName))
                    continue;

                var filePath = Path.Combine(directoryPath, fileName);
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath); // 파일 삭제
                    }
                }
                catch
                {
                    // 삭제 실패는 무시
                }
            }
        }

        /// <summary>
        /// orgDir의 모든 파일을 targetDir에 덮어쓰기 복사합니다.
        /// 하위 폴더의 파일도 모두 targetDir에 복사되며, 폴더 구조는 유지됩니다.
        /// .meta 확장자 파일은 제외됩니다.
        /// </summary>
        /// <param name="orgDir">복사할 원본 디렉토리 경로</param>
        /// <param name="targetDir">복사 대상 디렉토리 경로</param>
        public static void CopyAllFilesToDirectory(string orgDir, string targetDir)
        {
            // orgDir가 null 또는 빈 문자열이면 바로 반환
            if (string.IsNullOrEmpty(orgDir))
                return;

            // orgDir가 존재하지 않으면 바로 반환
            if (!Directory.Exists(orgDir))
                return;

            // targetDir가 null 또는 빈 문자열이면 바로 반환
            if (string.IsNullOrEmpty(targetDir))
                return;

            try
            {
                // 재귀적으로 폴더 구조를 유지하며 파일 복사
                CopyAllFilesRecursively(orgDir, targetDir, "");
            }
            catch
            {
                // 복사 실패는 무시
            }
        }

        private static void CopyAllFilesRecursively(string sourceDir, string targetDir, string relativePath)
        {
            var currentSource = Path.Combine(sourceDir, relativePath);
            var currentTarget = Path.Combine(targetDir, relativePath);

            // 대상 폴더가 존재하지 않으면 생성
            if (!Directory.Exists(currentTarget))
            {
                Directory.CreateDirectory(currentTarget);
            }

            // 현재 폴더의 파일 복사 (.meta 제외)
            var files = Directory.GetFiles(currentSource)
                .Where(f => Path.GetExtension(f).ToLower() != ".meta");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(currentTarget, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            // 하위 폴더 재귀 호출
            var subDirs = Directory.GetDirectories(currentSource);
            foreach (var subDir in subDirs)
            {
                var subDirName = Path.GetFileName(subDir);
                CopyAllFilesRecursively(sourceDir, targetDir, Path.Combine(relativePath, subDirName));
            }
        }

        /// <summary>
        /// orgDir의 모든 파일명을 기준으로 targetDir에서 해당 파일명을 가진 파일을 제거합니다.
        /// 하위 폴더의 파일도 포함되며, .meta 확장자 파일은 제외됩니다.
        /// </summary>
        public static void RemoveFilesFromTargetBasedOnOrgDir(string orgDir, string targetDir)
        {
            // orgDir가 null 또는 빈 문자열이면 바로 반환
            if (string.IsNullOrEmpty(orgDir))
                return;

            // orgDir가 존재하지 않으면 바로 반환
            if (!Directory.Exists(orgDir))
                return;

            // targetDir가 null 또는 빈 문자열이면 바로 반환
            if (string.IsNullOrEmpty(targetDir))
                return;

            // targetDir가 존재하지 않으면 바로 반환
            if (!Directory.Exists(targetDir))
                return;

            try
            {
                // orgDir의 모든 파일명을 수집 (.meta 제외, 대소문자 구분 없이)
                var orgFileNames = Directory.GetFiles(orgDir, "*", SearchOption.AllDirectories)
                    .Where(f => Path.GetExtension(f).ToLower() != ".meta")
                    .Select(f => Path.GetFileName(f).ToLower())
                    .ToHashSet();

                // targetDir의 모든 파일을 순회하며 orgFileNames에 있는 파일명과 일치하면 삭제
                var targetFiles = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories);
                foreach (var file in targetFiles)
                {
                    var fileName = Path.GetFileName(file).ToLower();
                    if (orgFileNames.Contains(fileName))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // 삭제 실패는 무시
                        }
                    }
                }
            }
            catch
            {
                // 전체 예외는 무시
            }
        }
    }
}
