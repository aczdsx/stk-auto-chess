#if UNITY_EDITOR // 유니티 에디터에서만 작동
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using UnityEngine;

public class NaniNovelToolkit : MonoBehaviour
{
    [Header("엑셀시트 경로")]
    public string excelFilePath;

    [Header("나니노벨 스크립트 폴더 경로")]
    public string naniScriptFolderPath;

    [Header("타겟 시트 이름")]
    public string sheetName;

    [ContextMenu("타겟시트 변환")]
    public void ConvertTargetSheet()
    {
        FileInfo existingFile = new FileInfo(excelFilePath);
        using (ExcelPackage package = new ExcelPackage(existingFile))
        {
            var worksheet = package.Workbook.Worksheets.Where(i => i.Name.Equals(sheetName)).Select(i => i).First();

            if (worksheet != null)
            {
                List<string> inputTextList = new List<string>();
                int rowCount = worksheet.Dimension.End.Row; //get row count

                for (int row = 1; row < rowCount; row++)
                {
                    var text_1 = worksheet.Cells[row, 1].Value?.ToString();
                    var text_2 = worksheet.Cells[row, 2].Value?.ToString();
                    inputTextList.Add($"{text_1}{text_2}");
                }

                WriteTxt(
                    Application.dataPath.Replace("Assets", "") + naniScriptFolderPath + "/" + worksheet.Name + ".nani",
                    inputTextList);
            }
        }
    }

    [ContextMenu("전체시트 변환")]
    public void Run()
    {
        FileInfo existingFile = new FileInfo(excelFilePath);
        using (ExcelPackage package = new ExcelPackage(existingFile))
        {
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                if (worksheet != null)
                {
                    List<string> inputTextList = new List<string>();
                    int rowCount = worksheet.Dimension.End.Row; //get row count

                    for (int row = 1; row < rowCount; row++)
                    {
                        var text_1 = worksheet.Cells[row, 1].Value?.ToString();
                        var text_2 = worksheet.Cells[row, 2].Value?.ToString();
                        inputTextList.Add($"{text_1}{text_2}");
                    }

                    WriteTxt(
                        Application.dataPath.Replace("Assets", "") + naniScriptFolderPath + "/" + worksheet.Name +
                        ".nani",
                        inputTextList);
                }
            }
        }
    }

    private void WriteTxt(string filePath, List<string> messageList)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }

        FileStream fileStream
            = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);

        StreamWriter writer = new StreamWriter(fileStream, System.Text.Encoding.Unicode);

        foreach (var message in messageList)
        {
            writer.WriteLine(message);
        }

        writer.Close();
    }
}
#endif