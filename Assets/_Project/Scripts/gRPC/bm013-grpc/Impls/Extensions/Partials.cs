namespace Tech.Hive.V1
{
    public partial class CharacterData
    {
        /// <summary>
        /// 스펙 캐릭터 인덱스 가져오기
        /// CharacterId = ItemId = SpecCharacterIndex (동일)
        /// </summary>
        public int GetSpecCharacterIndex()
        {
            return (int)CharacterId;
        }
    }
}
