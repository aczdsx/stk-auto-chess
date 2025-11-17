using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    public class InGameTile
    {
        public EffectCodeContainer EffectCodeContainer => _ecc;
        private EffectCodeContainer _ecc;
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
            _ecc = new EffectCodeContainer(this);
        }

        ~InGameTile()
        {
            _ecc.Clear();
            _ecc = null;
        }

        public bool CheckValidTile(AllianceType allianceType, bool isCheckSameAllianceType)
        {
            if (OccupiedCharacter == null)
                return false;

            if (OccupiedCharacter.AllianceType == AllianceType.Wall)
                return false;

            if (OccupiedCharacter.AllianceType == AllianceType.Neutral)
            {
                return true;
            }
            else
            {
                if (isCheckSameAllianceType && OccupiedCharacter.AllianceType == allianceType)
                {
                    return true;
                }
                else if (!isCheckSameAllianceType && OccupiedCharacter.AllianceType != allianceType)
                {
                    return true;
                }
            }
            
            return false;
        }

        public bool IsOccupied()
        {
            return OccupiedCharacter != null;
        }

        public void SetOccupied(CharacterController character)
        {
            OccupiedCharacter = character;
            var effectCodes = _ecc.GetEffectCodesByType(EffectCodeType.Tile);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeTileLambda.OnTileCharacterEnterLambda, this, OccupiedCharacter);

            var effectCodesByGame = _ecc.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(effectCodesByGame, EffectCodeGameLambda.OnTileCharacterEnterLambda, this, OccupiedCharacter);

            var inGameEffectCodes = InGameManager.Instance.EffectCodeContainer.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(inGameEffectCodes, EffectCodeGameLambda.OnTileCharacterEnterLambda, this, OccupiedCharacter);
        }

        public void SetUnoccupied()
        {
            var effectCodes = _ecc.GetEffectCodesByType(EffectCodeType.Tile);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeTileLambda.OnTileCharacterExitLambda, this, OccupiedCharacter);

            var effectCodesByGame = _ecc.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(effectCodesByGame, EffectCodeGameLambda.OnTileCharacterExitLambda, this, OccupiedCharacter);

            var inGameEffectCodes = InGameManager.Instance.EffectCodeContainer.GetEffectCodesByType(EffectCodeType.Game);
            EffectCodeForLoopHelper.CallWithArgs(inGameEffectCodes, EffectCodeGameLambda.OnTileCharacterExitLambda, this, OccupiedCharacter);

            OccupiedCharacter = null;
        }
    }
}
