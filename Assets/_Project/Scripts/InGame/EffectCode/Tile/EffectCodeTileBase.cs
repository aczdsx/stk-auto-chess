
using System;

namespace CookApps.BattleSystem
{
    public abstract class EffectCodeTileBase : EffectCodeBase
    {
        public override EffectCodeType Type => EffectCodeType.Tile;

        public virtual void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
        }

        public virtual void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
        }
    }

    public static class EffectCodeTileLambda
    {
        public static Action<EffectCodeBase, InGameTile, CharacterController> OnTileCharacterEnterLambda = (effectCode, tile, character) =>
        {
            if (effectCode is EffectCodeTileBase effectCodeTile)
            {
                effectCodeTile.OnTileCharacterEnter(tile, character);
            }
        };
        public static Action<EffectCodeBase, InGameTile, CharacterController> OnTileCharacterExitLambda = (effectCode, tile, character) =>
        {
            if (effectCode is EffectCodeTileBase effectCodeTile)
            {
                effectCodeTile.OnTileCharacterExit(tile, character);
            }
        };
    }
}
