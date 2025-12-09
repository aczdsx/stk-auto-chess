using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace CookApps.AutoBattler
{
    public static class MessageUtility
    {
        private static readonly IDictionary<Type, object> _messageParserPool = new Dictionary<Type, object>();
        private static MessageParser<T> GetOrCreate<T>() where T : IMessage<T>, new()
        {
            var type = typeof(T);
            if (_messageParserPool.TryGetValue(type, out var parser))
                return (MessageParser<T>)parser;

            parser = new MessageParser<T>(() => new T());
            _messageParserPool[type] = parser;

            return (MessageParser<T>)parser;
        }

        public static T FromBase64String<T>(string base64) where T : IMessage<T>, new()
        {
            if (string.IsNullOrEmpty(base64))
                return new T();

            var bytes = Convert.FromBase64String(base64);
            var parser = GetOrCreate<T>();
            return parser.ParseFrom(bytes);
        }

        public static T FromBytes<T>(ReadOnlySpan<byte> data) where T : IMessage<T>, new()
        {
            var parser = GetOrCreate<T>();
            return parser.ParseFrom(data);
        }

        public static string ToBase64String(IMessage message)
        {
            if (message == null)
                return string.Empty;

            var bytes = message.ToByteArray();
            return Convert.ToBase64String(bytes);
        }

        public static void ToBytes(IMessage message, Span<byte> buffer)
        {
            message.WriteTo(buffer);
        }
    }
}
