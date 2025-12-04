/*
* Copyright (c) CookApps.
*/

using CookApps.NetLite.Constants;

namespace CookApps.NetLite.Initialize
{
    /// <summary>
    /// NetLite 초기화 파라미터입니다.
    /// </summary>
    public class NetLiteInitializeParam
    {
        /// <summary>
        /// 접속 주소 (예: "https://example.com:50051")
        /// </summary>
        public string Address { get; init; }
        /// <summary>
        /// 로그를 출력할 것인지. 이 값과 상관없이 Development Build, _DEV가 아니면 출력되지 않습니다.
        /// </summary>
        public bool EnabledLog { get; init; }
        /// <summary>
        /// Store 정보
        /// </summary>
        public StoreMap Store { get; init; }
    }
}
