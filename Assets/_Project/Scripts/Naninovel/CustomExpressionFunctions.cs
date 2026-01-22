using Naninovel;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Naninovel 커스텀 Expression Functions
    ///
    /// 사용법:
    /// - {Player()} : 플레이어 닉네임 반환
    /// - {player} : CustomVariable로 저장된 닉네임 (NicknamePopupNaninovel에서 설정)
    ///
    /// .nani 스크립트 예시:
    /// Narrator: 흥미로운듯 {Player()} 를 쳐다보는 재클린.
    ///
    /// 참고: Naninovel이 ExpressionFunctionAttribute가 붙은 메서드를 자동 수집합니다.
    /// </summary>
    public static class CustomExpressionFunctions
    {
        /// <summary>
        /// 플레이어 닉네임 반환
        /// 사용: {Player()}
        /// </summary>
        [ExpressionFunction("Player")]
        [Doc("Returns the player's nickname.", examples: "Player()")]
        public static string Player()
        {
            // ServerDataManager에서 닉네임 가져오기
            var nickname = ServerDataManager.Instance?.PlayerData?.Nickname;

            if (string.IsNullOrEmpty(nickname))
            {
                // CustomVariable에서 가져오기 (fallback)
                var variableManager = Engine.GetService<ICustomVariableManager>();
                if (variableManager != null && variableManager.VariableExists("player"))
                {
                    nickname = variableManager.GetVariableValue("player").String;
                }
            }

            return nickname ?? "관측자"; // 기본값
        }
    }
}
