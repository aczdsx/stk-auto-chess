using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 위에서 드래그 가능한 오브젝트 인터페이스.
    /// BoardInputHandler는 이 인터페이스를 통해 드래그/드롭을 처리하며,
    /// 구체적인 드롭 로직은 각 구현체가 담당.
    /// </summary>
    public interface IBoardDraggableObject
    {
        int Col { get; }
        int Row { get; }
        Vector3 WorldPosition { get; }
        void SetWorldPosition(Vector3 pos);

        /// <summary>
        /// 드롭 시 호출. 오브젝트 자체가 드롭 로직 실행.
        /// true=처리 완료(커맨드 발행 등), false=실패(원위치 복귀).
        /// </summary>
        bool TryHandleDrop(int col, int row);
    }
}
