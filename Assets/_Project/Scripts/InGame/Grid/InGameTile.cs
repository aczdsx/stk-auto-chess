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
        public InGameTile cameFrom { set; get; } = null;


        public InGameTileView View { get; private set; }

        // 점유 여부
        public CharacterController OccupiedCharacter { get; private set; }

        public InGameTile(int x, int y, InGameTileView view)
        {
            X = x;
            Y = y;
            View = view;
        }

        public bool IsOccupied()
        {
            return OccupiedCharacter != null;
        }

        public void SetOccupied(CharacterController character)
        {
            OccupiedCharacter = character;
            var effectCodeGames = InGameManager.Instance.EffectCodeContainer.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(effectCodeGames, EffectCodeGameLambda.OnTileCharacterEnterLambda, this, OccupiedCharacter);
        }

        public void SetUnoccupied()
        {
            var effectCodeGames = InGameManager.Instance.EffectCodeContainer.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(effectCodeGames, EffectCodeGameLambda.OnTileCharacterExitLambda, this, OccupiedCharacter);
            OccupiedCharacter = null;
        }
    }
}
