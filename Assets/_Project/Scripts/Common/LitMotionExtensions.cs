using System;
using LitMotion;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 게임 전역 스케줄러 관리.
    /// 인게임에서 커스텀 타임스케일이 필요한 경우 설정.
    /// </summary>
    public static class InGameLitMotionScheduler
    {
        /// <summary>
        /// 현재 활성 스케줄러. null이면 LitMotion 기본 스케줄러(MotionScheduler.Update) 사용.
        /// ManualMotionDispatcher 등을 할당하면 WithInGameScheduler()가 자동 적용.
        /// </summary>
        public static IMotionScheduler Current { get; set; }
    }

    /// <summary>
    /// LitMotion 편의 확장 메서드.
    /// </summary>
    public static class LitMotionExtensions
    {
        // === 스케줄러 자동 연결 ===

        /// <summary>
        /// InGameLitMotionScheduler.Current가 설정되어 있으면 자동으로 WithScheduler 적용.
        /// 설정되지 않았으면 아무 동작 없이 builder를 그대로 반환.
        /// </summary>
        public static MotionBuilder<TValue, TOptions, TAdapter> WithInGameScheduler<TValue, TOptions, TAdapter>(
            this MotionBuilder<TValue, TOptions, TAdapter> builder)
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
            where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
        {
            var scheduler = InGameLitMotionScheduler.Current;
            return scheduler != null ? builder.WithScheduler(scheduler) : builder;
        }

        // === Sequence 헬퍼 ===
        // LitMotion Insert()는 lastTail을 업데이트하지 않음.
        // Insert() 후 Join() → Join은 lastTail(0)에서 시작.
        // DOTween Insert+Join 동작이 필요하면 AppendInterval().Append().Join() 사용.

        /// <summary>
        /// 시퀀스에 콜백 추가. DOTween AppendCallback() 대체.
        /// </summary>
        public static MotionSequenceBuilder AppendCallback(
            this MotionSequenceBuilder builder,
            Action callback)
        {
            var handle = LMotion.Create(0f, 0f, 0f)
                .WithOnComplete(callback)
                .RunWithoutBinding();
            return builder.Append(handle);
        }

        /// <summary>
        /// 특정 시간에 콜백 삽입. DOTween InsertCallback() 대체.
        /// </summary>
        public static MotionSequenceBuilder InsertCallback(
            this MotionSequenceBuilder builder,
            float position,
            Action callback)
        {
            var handle = LMotion.Create(0f, 0f, 0f)
                .WithOnComplete(callback)
                .RunWithoutBinding();
            return builder.Insert(position, handle);
        }
    }
}
