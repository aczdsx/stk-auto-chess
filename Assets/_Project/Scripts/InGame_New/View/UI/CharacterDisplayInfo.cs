namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 벤치 UI에 표시할 캐릭터 정보. 시뮬레이션 UnitData와 서버 데이터를 연결하는 경량 구조체.
    /// </summary>
    public struct CharacterDisplayInfo
    {
        public int ChampionSpecId;
        public byte StarLevel;
        public int Level;
        public uint ServerCharacterId;
    }
}
