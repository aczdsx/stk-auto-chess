using MemoryPack;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 클라이언트 데이터 베이스 클래스
    /// 변경 시 자동 저장 지원
    /// </summary>
    public abstract class ClientDataBase
    {
        /// <summary>
        /// 데이터 카테고리 (저장 키)
        /// </summary>
        [MemoryPackIgnore] public abstract string Category { get; } 

        /// <summary>
        /// 데이터 변경 시 호출 (자동 저장 트리거)
        /// </summary>
        protected void SetDirty()
        {
            ClientDataManager.Instance.MarkDirty(this);
        }
    }
}
