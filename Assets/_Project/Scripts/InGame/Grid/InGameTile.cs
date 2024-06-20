using CookApps.AutoBattler;
using Unity.Mathematics;

namespace CookApps.BattleSystem
{
    public class InGameTile
    {
        private EffectCodeContainer ecc;
        public int X { get; }
        public int Y { get; }
        public int G { set; get; } = -1;
        public int H { set; get; } = -1;
        public InGameTile cameFrom { set; get; } = null;


        public InGameTileView View { get; private set; }

        // 점유 여부
        public CharacterController OccupiedCharacter { get; private set; }

        public InGameTile(int x, int y, InGameTileView view)
        {
            X = x;
            Y = y;
            View = view;
            ecc = new EffectCodeContainer(this);
        }

        ~InGameTile()
        {
            ecc.Clear();
            ecc = null;
        }

        public bool IsOccupied()
        {
            return OccupiedCharacter != null;
        }

        public void SetOccupied(CharacterController character)
        {
            OccupiedCharacter = character;
            var effectCodes = ecc.GetEffectCodesByType(EffectCodeType.Tile);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeTileLambda.OnTileCharacterEnterLambda, this, OccupiedCharacter);
        }

        public void SetUnoccupied()
        {
            var effectCodes = ecc.GetEffectCodesByType(EffectCodeType.Tile);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeTileLambda.OnTileCharacterExitLambda, this, OccupiedCharacter);
            OccupiedCharacter = null;
        }
    }
}
