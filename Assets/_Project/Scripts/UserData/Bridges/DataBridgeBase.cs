using System;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// DataBridge의 base class
    /// ServerDataManager와 UI 사이의 중간 레이어 공통 로직
    /// </summary>
    /// <typeparam name="TModel">데이터 모델 타입 (IDataModel 구현 필요)</typeparam>
    public abstract class DataBridgeBase<TModel> : IDisposable
        where TModel : class, IDataModel
    {
        /// <summary>
        /// 데이터 모델
        /// </summary>
        protected TModel Model { get; private set; }

        /// <summary>
        /// 카테고리 키
        /// </summary>
        protected string CategoryKey { get; private set; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="model">데이터 모델 (직접 전달)</param>
        /// <param name="categoryKey">데이터 카테고리 키</param>
        protected DataBridgeBase(TModel model, string categoryKey)
        {
            Model = model;
            CategoryKey = categoryKey;

            // 이벤트 구독
            SubscribeEvents();
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            // 모델 변경 감지 (OnChanged 이벤트)
            if (Model != null)
            {
                Model.OnChanged += OnModelChanged;
            }

            // 모델별 이벤트 구독 (파생 클래스에서 구현)
            SubscribeModelEvents();
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            // 이벤트 구독 해제
            if (Model != null)
            {
                Model.OnChanged -= OnModelChanged;
            }

            UnsubscribeModelEvents();
        }

        /// <summary>
        /// 데이터가 전체적으로 변경되었을 때 호출됨 (Model.OnChanged 처리)
        /// 파생 클래스에서 오버라이드하여 UI 갱신 이벤트 발생
        /// </summary>
        protected abstract void OnModelChanged();

        /// <summary>
        /// 모델별 이벤트 구독 (구현 필요)
        /// </summary>
        protected abstract void SubscribeModelEvents();

        /// <summary>
        /// 모델별 이벤트 구독 해제 (구현 필요)
        /// </summary>
        protected abstract void UnsubscribeModelEvents();
        UnsubscribeModelEvents();
    }
}
    }
}
