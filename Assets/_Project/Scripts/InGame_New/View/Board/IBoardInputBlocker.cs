namespace CookApps.AutoChess.View
{
    public enum BoardInputAction
    {
        Select,     // 보드 유닛 탭 선택
        Place,      // 벤치 → 보드 배치
        Move,       // 보드 내 재배치
        Withdraw,   // 보드 → 벤치 회수
    }

    /// <summary>
    /// 보드 입력 차단 인터페이스.
    /// SelectableBlockerManager 패턴과 동일 — 우선순위 기반, 하나라도 false면 차단.
    /// 튜토리얼 등에서 특정 액션/타일만 허용하는 용도.
    /// </summary>
    public interface IBoardInputBlocker
    {
        /// <summary>
        /// 특정 액션+타일에 대한 입력 허용 여부.
        /// col/row가 -1이면 타일 무관 체크 (드래그 시작 시점 등).
        /// </summary>
        bool IsAllowInput(BoardInputAction action, int col, int row);

        int GetPriority();
    }
}
