using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 그리드 타일 관리.
    /// 스테이지 프리팹에 이미 배치된 타일을 참조하여, 페이즈에 따라 Preparation / Combat 전환.
    /// Initialize() 시 타일 배열에서 그리드 크기를 역산하여 BoardHelper/BoardWorldHelper를 세팅.
    /// </summary>
    public class BoardGridView : MonoBehaviour
    {
        [Header("Tile References")]
        [Tooltip("스테이지 프리팹에 배치된 타일들. 순서: board0 row0 col0..6, row1 col0..6, ... board3 row7 col0..6")]
        [SerializeField] private BoardTileView[] _tiles;

        [Header("Board Settings")]
        [SerializeField] private int _boardWidth = 7;
        [SerializeField] private int _boardHeight = 4;
        [SerializeField] private int _boardCount = 1;

        private bool _isCombatMode;
        private int _combatHeight;

        // ── 초기화 ──

        public void Initialize()
        {
            _combatHeight = _boardHeight * 2;

            // 시뮬레이션 레이어에 보드 크기 세팅
            BoardHelper.Setup(_boardWidth, _boardHeight);

            // 타일 월드 좌표를 BoardWorldHelper에 등록
            var transforms = new Transform[_tiles.Length];
            for (int i = 0; i < _tiles.Length; i++)
                transforms[i] = _tiles[i] != null ? _tiles[i].transform : null;
            BoardWorldHelper.Initialize(transforms, _boardCount);

            // 초기 상태: Preparation
            _isCombatMode = false;
            ShowPreparationGrid();
        }

        // ── 페이즈 전환 ──

        /// <summary>Preparation 모드: 플레이어 보드만 표시</summary>
        public void OnPreparation()
        {
            if (!_isCombatMode) return;
            _isCombatMode = false;
            ShowPreparationGrid();
        }

        /// <summary>Combat 모드: 전체 전투 그리드 표시</summary>
        public void OnCombatStart()
        {
            if (_isCombatMode) return;
            _isCombatMode = true;
            ShowCombatGrid();
        }

        // ── 내부 ──

        private void ShowPreparationGrid()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] == null) continue;
                int row = (i / _boardWidth) % _combatHeight;
                _tiles[i].SetVisible(row < _boardHeight);
            }
        }

        private void ShowCombatGrid()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] != null)
                    _tiles[i].SetVisible(true);
            }
        }
    }
}
