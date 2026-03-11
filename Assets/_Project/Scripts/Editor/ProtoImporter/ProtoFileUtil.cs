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
