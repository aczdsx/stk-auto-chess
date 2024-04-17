namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameTile
    {
        public int X { get; }
        public int Y { get; }

        // 점유 여부
        public CharacterController OccupiedCharacter { get; set; }

        public InGameTile(int x, int y)
        {
            X = x;
            Y = y;
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
