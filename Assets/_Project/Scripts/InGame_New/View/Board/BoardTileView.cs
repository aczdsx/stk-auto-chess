using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 개별 보드 타일 비주얼.
    /// 기존 InGameTileView의 기능을 포함: 보드 스프라이트, 선택 하이라이트, 공격 범위, 스킬 범위 표시.
    /// </summary>
    public class BoardTileView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _boardSprite;
        [SerializeField] private GameObject _activeObj;
        [SerializeField] private GameObject _attackActiveObj;
        [SerializeField] private GameObject _skillNavigateObj;

        public int Col { get; private set; }
        public int Row { get; private set; }
        public Vector3 Position => transform.position;
        public bool IsAlphaBoard => _boardSprite != null && _boardSprite.color.a == 0;

        /// <summary>그리드 좌표 설정</summary>
        public void Setup(int col, int row)
        {
            Col = col;
            Row = row;
        }

        // ── 색상 ──

        /// <summary>타일 색상 설정</summary>
        public void SetColor(Color color)
        {
            if (_boardSprite != null)
                _boardSprite.color = color;
        }

        /// <summary>타일 색상 반환</summary>
        public Color GetColor()
        {
            return _boardSprite != null ? _boardSprite.color : Color.clear;
        }

        // ── 표시/숨김 ──

        /// <summary>타일 표시/숨김</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ── 하이라이트 ──

        /// <summary>선택 하이라이트 (배치 가능 타일 표시)</summary>
        public void SetActiveHighlight(bool isActive)
        {
            if (_activeObj != null)
                _activeObj.SetActive(isActive);
        }

        /// <summary>공격 사거리 표시</summary>
        public void SetAttackRangeHighlight(bool isActive)
        {
            if (_attackActiveObj != null)
                _attackActiveObj.SetActive(isActive);
        }

        /// <summary>스킬 범위 표시</summary>
        public void SetSkillNavigateHighlight(bool isActive)
        {
            if (_skillNavigateObj != null)
                _skillNavigateObj.SetActive(isActive);
        }
    }
}
