/*
* Copyright (c) CookApps.
*/

using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace CookApps.NetLite.Utils
{
    public static class GZipUtil
    {
        /// gzip 압축 데이터를 문자열로 변환
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string DecompressToUtf8String(byte[] data)
        {
            return DecompressToUtf8String(new ReadOnlyMemory<byte>(data));
        }

        /// gzip 압축 데이터를 문자열로 변환
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string DecompressToUtf8String(byte[] data, int start, int length)
        {
            return DecompressToUtf8String(new ReadOnlyMemory<byte>(data, start, length));
        }

        /// gzip 압축 데이터를 문자열로 변환
        public static unsafe string DecompressToUtf8String(ReadOnlyMemory<byte> data)
        {
            // GZip 매직 넘버 체크 (0x1F 0x8B)
            if (data.Length < 2 || data.Span[0] != 0x1F || data.Span[1] != 0x8B)
            {
                // GZip이 아닌 경우 빈 문자열
                return string.Empty;
            }

            // 원본 크기
            int decompressedSize = GetDecompressedSize(data);
            if (decompressedSize == 0)
            {
                return string.Empty;
            }

            using MemoryHandle handle = data.Pin();
            using var compressedStream = new UnmanagedMemoryStream((byte*) handle.Pointer, data.Length);
            using var decompressedStream = new MemoryStream(decompressedSize);
            using var gZipStream = new GZipStream(compressedStream, CompressionMode.Decompress);

            try
            {
                gZipStream.CopyTo(decompressedStream, 4096);
                string json =
                    Encoding.UTF8.GetString(decompressedStream.GetBuffer(), 0, (int) decompressedStream.Length);
                return json;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// 문자열을 utf8 변환이후 gzip 압축
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] CompressFromUtf8String(string data)
        {
            return CompressFromUtf8String(data.AsSpan());
        }

        /// 문자열을 utf8 인코딩 후 gzip 압축하고, Base64 문자열로 반환
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CompressToBase64String(string data)
        {
            byte[] compressedBytes = CompressFromUtf8String(data);
            return Convert.ToBase64String(compressedBytes);
        }

        /// 문자열을 utf8 변환이후 gzip 압축
        public static byte[] CompressFromUtf8String(ReadOnlySpan<char> data)
        {
            // UTF8 인코딩 시 필요한 최대 바이트 수 계산
            int maxByteCount = Encoding.UTF8.GetMaxByteCount(data.Length);
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
            byte[] result;

            try
            {
                // 임대한 배열을 Span으로 사용
                Span<byte> buffer = rentedBuffer.AsSpan(0, maxByteCount);
                int actualByteCount = Encoding.UTF8.GetBytes(data, buffer);

                // MemoryStream의 초기 용량은 최소 1로 지정하여 빈 문자열일 경우에도 내부 버퍼가 생성되도록 함
                using (var outputStream = new MemoryStream(Math.Max(actualByteCount, 1)))
                {
                    using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        // 빈 문자열이면 actualByteCount가 0이지만, GZipStream은 헤더와 푸터를 기록함
                        gzipStream.Write(buffer.Slice(0, actualByteCount));
                    }
                    result = outputStream.ToArray();
                }
            }
            finally
            {
                // 사용한 버퍼 반환
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            return result;
        }

        // 압축 파일의 풀었을때 크기
        private static int GetDecompressedSize(ReadOnlyMemory<byte> compressedData)
        {
            // GZIP 포맷의 마지막 4바이트가 원본 크기 (최대 4GB)
            if (compressedData.Length < 4)
            {
                return 0;
            }

            return BitConverter.ToInt32(compressedData[^4..].Span);
        }
    }
}
