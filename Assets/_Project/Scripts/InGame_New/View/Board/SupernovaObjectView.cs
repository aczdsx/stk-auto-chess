using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 슈퍼노바 오브젝트 보드 비주얼.
    /// 보드 위에 표시되며, 유저가 드래그하여 유닛에 부여할 수 있음.
    /// </summary>
    public class SupernovaObjectView : MonoBehaviour, IBoardDraggableObject
    {
        private const string NOT_SUPERNOVA_TYPE_TOKEN = "NOT_SUPERNOVA_TYPE";

        public int TraitId { get; private set; }
        public int Col { get; private set; }
        public int Row { get; private set; }

        public Vector3 WorldPosition => transform.position;

        // 드롭 로직용 콜백
        private System.Func<GameWorld> _getWorld;
        private System.Action<GameCommand> _sendCommand;

        public void Setup(int traitId, byte col, byte row, byte tier,
                          System.Func<GameWorld> getWorld, System.Action<GameCommand> sendCommand)
        {
            TraitId = traitId;
            Col = col;
            Row = row;
            _getWorld = getWorld;
            _sendCommand = sendCommand;

            UpdatePosition(col, row);
        }

        public void UpdatePosition(int col, int row)
        {
            Col = col;
            Row = row;
            var worldPos = BoardWorldHelper.BoardGridToWorld(0, col, row);
            transform.position = worldPos;
        }

        /// <summary>드래그 중 월드 좌표로 직접 위치 설정</summary>
        public void SetWorldPosition(Vector3 pos)
        {
            transform.position = pos;
        }

        public bool TryHandleDrop(int col, int row)
        {
            var world = _getWorld?.Invoke();
            if (world == null) return false;

            int slot = BoardHelper.ToIndex(col, row);
            int occupant = world.BoardSlots[0][slot];

            if (occupant != UnitData.InvalidId)
            {
                // 유닛 위 드롭 → trait 체크 + SetSynergyPrepTarget 커맨드
                int traitBit = 1 << TraitId;
                int unitIdx = world.FindUnitIndex(occupant);
                if (unitIdx >= 0 && (world.Units[unitIdx].TraitFlags & traitBit) != 0)
                {
                    _sendCommand(GameCommand.SetSynergyPrepTarget(0, TraitId, occupant));
                    return true;
                }
                ToastManager.Instance.ShowToastByTokenKey(NOT_SUPERNOVA_TYPE_TOKEN);
                return false;
            }

            // 빈 타일 드롭 → 위치 이동
            if (col != Col || row != Row)
            {
                _sendCommand(GameCommand.SetSynergyPrepTarget(0, TraitId, -1, col, row));
                return true;
            }
            return false;
        }
    }
}
