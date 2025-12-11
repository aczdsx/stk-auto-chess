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
        /// 데이터가 크게 변경되었을 때 발생하는 이벤트
        /// (예: 전체 교체, 델타 적용 등)
        /// </summary>
        event Action OnChanged;

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
}
