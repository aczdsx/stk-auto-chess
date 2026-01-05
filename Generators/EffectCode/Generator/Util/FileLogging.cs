using System.IO;

namespace Generator.Util
{
    public static class FileLogging
    {
        private static string logPath = "/Users/twhan/logs/sourcegenerator.log";
        private static StreamWriter sw;
        static FileLogging()
        {
            // if (File.Exists(logPath))
            // {
            //     sw = File.AppendText(logPath);
            // }
            // else
            // {
            //     sw = File.CreateText(logPath);
            // }
        }
        
        public static void WriteLog(string log)
        {
            // sw.WriteLine(log);
        }
        
        public static void CloseFile()
        {
            // sw.Close();
        }
    }
}
