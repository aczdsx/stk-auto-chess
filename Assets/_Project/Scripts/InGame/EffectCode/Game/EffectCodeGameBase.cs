
using System;

namespace CookApps.BattleSystem
{
    public abstract class EffectCodeGameBase : EffectCodeBase
    {
        public override EffectCodeType Type => EffectCodeType.Game;

        public virtual void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
        }

        public virtual void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
        }

        public virtual void OnUpdate(float dt)
        {
        }
    }

    public static class EffectCodeGameLambda
    {
        public static Action<EffectCodeBase, InGameTile, CharacterController> OnTileCharacterEnterLambda = (effectCode, tile, character) =>
        {
            if (effectCode is EffectCodeGameBase effectCodeGame)
            {
                effectCodeGame.OnTileCharacterEnter(tile, character);
            }
        };
        public static Action<EffectCodeBase, InGameTile, CharacterController> OnTileCharacterExitLambda = (effectCode, tile, character) =>
        {
            if (effectCode is EffectCodeGameBase effectCodeGame)
            {
                effectCodeGame.OnTileCharacterExit(tile, character);
            }
        };

        public static Action<EffectCodeBase, float> CallOnUpdateLambda = (effectCode, dt) =>
        {
            if (effectCode is EffectCodeGameBase effectCodeGame)
            {
                effectCodeGame.OnUpdate(dt);
            }
        };
    }
}
