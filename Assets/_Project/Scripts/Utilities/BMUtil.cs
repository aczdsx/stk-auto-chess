using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public static class BMUtil
{
    // 해당 트랜스폼의 하위 개체 삭제
    public static void RemoveChildObjects(Transform targetTransform)
    {
        for (int i = 0; i < targetTransform.childCount; i++)
        {
            UnityEngine.Object.Destroy(targetTransform.GetChild(i).gameObject);
        }
    }

    public static Color ChangeColorAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
    
    public static string ConvertToJsonSerialize<T>(T data)
    {
        return JsonConvert.SerializeObject(data);
    }
    
    public static T ConvertFromJsonDeserialize<T>(string data)
    {
        return JsonConvert.DeserializeObject<T>(data);
    }
    
    public static string CompressStringToGzip(string data)
    {
        byte[] rawData = Encoding.UTF8.GetBytes(data);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(rawData, 0, rawData.Length);
            }
            // Base64로 인코딩하여 문자열로 반환
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }

    public static string DecompressGzipToString(string compressedString)
    {
        // Base64 문자열을 바이트 배열로 변환
        byte[] gzipData = Convert.FromBase64String(compressedString);
    
        using (var inputStream = new MemoryStream(gzipData))
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
            gzipStream.CopyTo(outputStream);
            byte[] decompressedData = outputStream.ToArray();
        
            // 바이트 배열을 문자열로 변환
            string resultString = Encoding.UTF8.GetString(decompressedData);
        
            // 역슬래시 제거
            resultString = resultString.Replace("\\", "");
            
            // 따옴표 제거
            resultString = resultString.Trim('"');
        
            return resultString;
        }
    }

    public static T DecompressGzipToDataClass<T>(string compressedString)
    {
        // Base64 문자열을 바이트 배열로 변환
        byte[] gzipData = Convert.FromBase64String(compressedString);
    
        using (var inputStream = new MemoryStream(gzipData))
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
            gzipStream.CopyTo(outputStream);
            byte[] decompressedData = outputStream.ToArray();
        
            // 바이트 배열을 문자열로 변환
            string resultString = Encoding.UTF8.GetString(decompressedData);
        
            // 역슬래시 제거
            resultString = resultString.Replace("\\", "");
            
            // 따옴표 제거
            resultString = resultString.Trim('"');
        
            return ConvertFromJsonDeserialize<T>(resultString);
        }
    }
    
    public static string GenerateRandomId(int length)
    {
        string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] outputChars = new char[length];

        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        int minIndex = 0;
        int maxIndexExclusive = charset.Length;
        int diff = maxIndexExclusive - minIndex;

        long upperBound = uint.MaxValue / diff * diff;

        byte[] randomBuffer = new byte[sizeof(int)];

        for (int i = 0; i < outputChars.Length; i++)
        {
            // Generate a fair, random number between minIndex and maxIndex
            uint randomUInt;
            do
            {
                rng.GetBytes(randomBuffer);
                randomUInt = BitConverter.ToUInt32(randomBuffer, 0);
            }
            while (randomUInt >= upperBound);
            int charIndex = (int)(randomUInt % diff);

            // Set output character based on random index
            outputChars[i] = charset[charIndex];
        }

        return new string(outputChars);
    }
}
