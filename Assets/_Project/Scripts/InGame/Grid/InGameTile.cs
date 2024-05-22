using CookApps.AutoBattler;
using Unity.Mathematics;

namespace CookApps.BattleSystem
{
    public class InGameTile
    {
        public int X { get; }
        public int Y { get; }
        public int G { set; get; } = -1;
        public int H { set; get; } = -1;
        public int F => G + H;
        public InGameTile cameFrom { set; get; } = null;


        public InGameTileView View { get; private set; }

        // 점유 여부
        public CharacterController OccupiedCharacter { get; set; }

        public InGameTile(int x, int y, InGameTileView view)
        {
            X = x;
            Y = y;
            View = view;
        }

        public int CompareTo(InGameTile other)
        {
            return F == other.F ? 0 : F - other.F;
        }

        public bool IsOccupied()
        {
            return OccupiedCharacter != null;
        }

        public void SetOccupied(CharacterController character)
        {
            OccupiedCharacter = character;
        }

        public void SetUnoccupied()
        {
            OccupiedCharacter = null;
        }
    }
}
