/*
* Copyright (c) CookApps.
*/

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public partial class GrpcSpecService
    {
        public enum DataLoadingMethod
        {
            /// <summary>
            /// gRPC 방식으로 서버를 통해 로드합니다.
            /// </summary>
            Grpc,

            /// <summary>
            /// REST Api방식으로 CDN을 통해 로드합니다.
            /// </summary>
            Rest,
        }

        private const uint DefaultVersion = 0;

        //------------------- private / protected ------------------//
        private delegate Task<SpecDataResponse> DelegateServiceAsync(SpecDataRequest request);

        //--------------------------------------------------------------------------------//
        //------------------------------------METHOD--------------------------------------//
        //--------------------------------------------------------------------------------//
        //───────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Spec Data를 반환합니다.
        /// </summary>
        /// <param name="version">version</param>
        /// <param name="dataLoadingMethod">데이터 로드하는 방식을 선택합니다.</param>
        /// <returns>T형태의 클래스로 반환합니다.</returns>
        public async Task<T> GetSpecDataAsync<T>(uint version, DataLoadingMethod dataLoadingMethod = DataLoadingMethod.Grpc) where T : class
        {
            string jsonString = await GetSpecDataAsync(version, dataLoadingMethod);
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            return JsonUtility.FromJson<T>(jsonString);
        }

        /// <summary>
        /// Spec Data를 반환합니다.
        /// </summary>
        /// <param name="version">version</param>
        /// <param name="dataLoadingMethod">데이터 로드하는 방식을 선택합니다.</param>
        /// <returns>json string으로 반환합니다</returns>
        public async Task<string> GetSpecDataAsync(uint version, DataLoadingMethod dataLoadingMethod = DataLoadingMethod.Grpc)
        {
            string jsonString = dataLoadingMethod == DataLoadingMethod.Grpc
                ? await GetDataAsync(GetSpecDataAsync, version)
                : await GetSpecLinkAsync(SpecType.Game, version);

            return jsonString;
        }

        //─────────────────────────────────────────────────────────────────────────────────────── Localization
        /// <summary>
        /// Localization를 반환합니다.
        /// </summary>
        /// <param name="version">version</param>
        /// <param name="dataLoadingMethod">데이터 로드하는 방식을 선택합니다.</param>
        public async Task<T> GetLanguageDataAsync<T>(uint version, DataLoadingMethod dataLoadingMethod = DataLoadingMethod.Grpc) where T : class
        {
            string jsonString = await GetLanguageDataAsync(version, dataLoadingMethod);
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            return JsonUtility.FromJson<T>(jsonString);
        }

        /// <summary>
        /// Localization를 반환합니다.
        /// </summary>
        /// <param name="version">version</param>
        /// <param name="dataLoadingMethod">데이터 로드하는 방식을 선택합니다.</param>
        /// <returns>json string으로 반환합니다</returns>
        public async Task<string> GetLanguageDataAsync(uint version, DataLoadingMethod dataLoadingMethod = DataLoadingMethod.Grpc)
        {
            if (version <= DefaultVersion)
            {
                return string.Empty;
            }

            string jsonString = dataLoadingMethod == DataLoadingMethod.Grpc
                ? await GetDataAsync(GetLanguageDataAsync, version)
                : await GetSpecLinkAsync(SpecType.Language, version);

            return jsonString;
        }

        //─────────────────────────────────────────────────────────────────────────────────────── Post Localization
        /// <summary>
        /// 우편함 관련 Localization를 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="version">version</param>
        /// <param name="dataLoadingMethod">데이터 로드하는 방식을 선택합니다.</param>
        /// <returns>Type형태로 반환합니다</returns>
        public async Task<T> GetPostLanguageDataAsync<T>(uint version, DataLoadingMethod dataLoadingMethod = DataLoadingMethod.Grpc) where T : class
        {
            string jsonString = await GetPostLanguageDataAsync(version, dataLoadingMethod);
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            return JsonUtility.FromJson<T>(jsonString);
        }

        /// <summary>
        /// 우편함 관련 Localization을 반환합니다.
        /// </summary>
        /// <param name="version">version</param>
        /// <param name="dataLoadingMethod">데이터 로드하는 방식을 선택합니다.</param>
        /// <returns>json string으로 반환합니다</returns>
        public async Task<string> GetPostLanguageDataAsync(uint version, DataLoadingMethod dataLoadingMethod = DataLoadingMethod.Grpc)
        {
            if (version <= DefaultVersion)
            {
                return string.Empty;
            }

            string jsonString = dataLoadingMethod == DataLoadingMethod.Grpc
                    ? await GetDataAsync(GetPostLanguageDataAsync, version)
                    : await GetSpecLinkAsync(SpecType.PostLanguage, version);

            return jsonString;
        }

        private async Task<string> GetDataAsync(DelegateServiceAsync serviceAsync, uint? version)
        {
            if (version == null)
            {
                throw new Exception("_specVersion이나 _localizationVersion이 null입니다. CheckVersionAsync를 먼저 실행해주세요.");
            }

            using PooledObject<SpecDataRequest> _ = GenericPool<SpecDataRequest>.Get(out SpecDataRequest request);
            request.Version = version.Value;

            SpecDataResponse response = await serviceAsync(request);

            if (response.IsError)
            {
                return null;
            }
            byte[] bytesGzip = Convert.FromBase64String(response.Data.Spec);
            return DecompressToJsonString(bytesGzip);
        }

        //─────────────────────────────────────────────────────────────────────────────────────── Get Spec Link
        private async Task<string> GetSpecLinkAsync(SpecType specType, uint version)
        {
            if (specType == SpecType.Unspecified)
            {
                return null;
            }

            using PooledObject<SpecLinkRequest> _ = GenericPool<SpecLinkRequest>.Get(out SpecLinkRequest request);
            request.SpecType = specType;
            request.Version = version;
            SpecLinkResponse response = await GetSpecLinkAsync(request);
            if (response.IsError)
            {
                return null;
            }

            UnityWebRequest req = UnityWebRequest.Get(response.Data.Url);
            req.SetRequestHeader("Content-Type", "text/plain");
            /*
             // TODO-tech : 나중에 주석 해제하여 복원 부탁드립니다.
            while (req.isDone)
            {
                await CookAppsTask.Yield();
            }
            */

            string cipherText = req.downloadHandler.text;
            req.Dispose();
            return DecryptAES256FromBase64(cipherText, response.Data.Key);
        }

        //jsonstring을 반환
        private static string DecryptAES256FromBase64(string encData, string key)
        {
            if (encData == null)
            {
                throw new ArgumentNullException(nameof(encData), "Data is null in Decrypt");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Key is null or empty in Decrypt");
            }

            string iv = key.Substring(0, 16);

            // Convert base64 string to byte array
            byte[] encDataBytes = Convert.FromBase64String(encData);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (MemoryStream msDecrypt = new MemoryStream(encDataBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (MemoryStream resultStream = new MemoryStream())
                            {
                                csDecrypt.CopyTo(resultStream);
                                byte[] decryptedBytes = resultStream.ToArray();
                                return DecompressToJsonString(decryptedBytes);
                            }
                        }
                    }
                }
            }
        }

        private static string DecompressToJsonString(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        gZipStream.CopyTo(decompressedStream);
                    }
                    return Encoding.UTF8.GetString(decompressedStream.ToArray());
                }
            }
        }
    }
}