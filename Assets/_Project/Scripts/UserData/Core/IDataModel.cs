using System;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 모든 데이터 모델이 구현해야 하는 인터페이스
    /// Reflection 없이 타입 안전성을 보장
    /// </summary>
    public interface IDataModel
    {
        /// <summary>
        /// 데이터 카테고리 키 (고유 식별자)
        /// </summary>
        string CategoryKey { get; }

        /// <summary>
        /// 데이터 버전 (델타 업데이트용)
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 델타 업데이트 적용
        /// </summary>
        void ApplyDelta(IDataModel delta);

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        void Reset();

        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        bool Validate();
    }

    /// <summary>
    /// 델타 업데이트를 위한 변경 플래그
    /// </summary>
    [Flags]
    public enum DataChangeFlags
    {
        None = 0,
        Created = 1 << 0,
        Updated = 1 << 1,
        Deleted = 1 << 2,
        All = Created | Updated | Deleted
    }

    /// <summary>
    /// 데이터 변경 이벤트
    /// </summary>
    public readonly struct DataChangeEvent
    {
        public readonly string CategoryKey;
        public readonly DataChangeFlags ChangeType;
        public readonly IDataModel OldData;
        public readonly IDataModel NewData;
        public readonly long Timestamp;

        public DataChangeEvent(string categoryKey, DataChangeFlags changeType, IDataModel oldData, IDataModel newData)
        {
            CategoryKey = categoryKey;
            ChangeType = changeType;
            OldData = oldData;
            NewData = newData;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}