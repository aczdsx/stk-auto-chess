namespace CookApps.AutoBattler
{
    public partial class StageInfo
    {
        private int _cachedBoardWidth = -1;
        private int _cachedBoardHeight = -1;

        /// <summary>
        /// map_size 문자열("W,H")에서 보드 크기를 파싱합니다.
        /// 결과를 캐싱하여 반복 파싱을 방지합니다.
        /// 실패 시 기본값(7, 8)을 반환합니다.
        /// ClassicBattle 모드 기준: height는 전체 전투 그리드 높이입니다.
        /// </summary>
        public void GetBoardSize(out int boardWidth, out int boardHeight)
        {
            if (_cachedBoardWidth > 0)
            {
                boardWidth = _cachedBoardWidth;
                boardHeight = _cachedBoardHeight;
                return;
            }

            boardWidth = 7;
            boardHeight = 8;

            if (!string.IsNullOrEmpty(map_size))
            {
                var parts = map_size.Split(',');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out boardWidth);
                    int.TryParse(parts[1], out boardHeight);
                }
            }

            _cachedBoardWidth = boardWidth;
            _cachedBoardHeight = boardHeight;
        }
    }
}
