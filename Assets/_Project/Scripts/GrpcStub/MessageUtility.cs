using System;
using Google.Protobuf;

namespace CookApps.AutoBattler
{
    public static class MessageUtility
    {
        public static T FromBase64String<T>(string base64) where T : IMessage<T>, new()
        {
            if (string.IsNullOrEmpty(base64)) return new T();

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var parser = new MessageParser<T>(() => new T());
                return parser.ParseFrom(bytes);
            }
            catch
            {
                // 파싱 실패 시 기본값 반환
                return new T();
            }
        }

        public static string ToBase64String(IMessage message)
        {
            if (message == null) return string.Empty;
            var bytes = message.ToByteArray();
            return Convert.ToBase64String(bytes);
        }
    }
}
