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
        where TModel : class, IDataModel, new()
    {
        /// <summary>
        /// 데이터 모델
        /// </summary>
        protected TModel Model { get; private set; }

        /// <summary>
        /// 서버 데이터 매니저
        /// </summary>
        protected ServerDataManager DataManager { get; private set; }

        /// <summary>
        /// 이벤트 버스
        /// </summary>
        protected DataEventBus EventBus { get; private set; }

        /// <summary>
        /// 카테고리 키
        /// </summary>
        protected string CategoryKey { get; private set; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="categoryKey">데이터 카테고리 키</param>
        protected DataBridgeBase(string categoryKey)
        {
            CategoryKey = categoryKey;
            DataManager = ServerDataManager.Instance;
            EventBus = DataEventBus.Instance;

            // 데이터 모델 가져오기 또는 생성
            InitializeModel();

            // 이벤트 구독
            SubscribeEvents();
        }

        /// <summary>
        /// 모델 초기화
        /// </summary>
        private void InitializeModel()
        {
            Model = DataManager.GetData<TModel>(CategoryKey);
            if (Model == null)
            {
                Model = new TModel();
                DataManager.RegisterFactory(CategoryKey, () => new TModel());
                DataManager.SetData(CategoryKey, Model);
            }
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            // 데이터 변경 감지 (EventBus)
            EventBus.Subscribe(CategoryKey, OnDataChanged);

            // 모델별 이벤트 구독 (파생 클래스에서 구현)
            SubscribeModelEvents();
        }

        /// <summary>
        /// 모델별 이벤트 구독 (파생 클래스에서 구현)
        /// </summary>
        protected abstract void SubscribeModelEvents();

        /// <summary>
        /// 모델별 이벤트 구독 해제 (파생 클래스에서 구현)
        /// </summary>
        protected abstract void UnsubscribeModelEvents();

        /// <summary>
        /// 데이터 변경 콜백 (파생 클래스에서 오버라이드 가능)
        /// </summary>
        protected virtual void OnDataChanged(DataChangeEvent changeEvent)
        {
            // 기본 구현 없음 - 파생 클래스에서 필요시 오버라이드
        }

        /// <summary>
        /// 리소스 정리 (IDisposable)
        /// </summary>
        public void Dispose()
        {
            // EventBus 구독 해제
            EventBus.Unsubscribe(CategoryKey, OnDataChanged);

            // 모델 이벤트 구독 해제
            if (Model != null)
            {
                UnsubscribeModelEvents();
            }
        }
    }
}
