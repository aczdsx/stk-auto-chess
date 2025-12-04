/*
* Copyright (c) CookApps.
*/

namespace CookApps.NetLite
{
    /// <summary>
    /// 서버 응답의 상태 코드(StatusCode)가 성공(200)이 아닌 경우 발생하는 예외입니다.
    /// 서버 응답에서 오류 상태를 나타내며, <see cref="StatusCode"/> 속성을 통해 구체적인 상태 코드를 확인할 수 있습니다.
    /// </summary>
    public class ResponseStatusCodeException : System.Exception
    {
        /// <summary>
        /// 서버로부터 받은 응답 상태 코드를 가져옵니다.
        /// 200이 아닌 값은 오류 상태를 나타냅니다.
        /// </summary>
        public uint StatusCode { get; }

        /// <summary>
        /// <see cref="ResponseStatusCodeException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="statusCode">서버 응답 상태 코드</param>
        /// <param name="message">예외를 설명하는 오류 메시지</param>
        public ResponseStatusCodeException(uint statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
