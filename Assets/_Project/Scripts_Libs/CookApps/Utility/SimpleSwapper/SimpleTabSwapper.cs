using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    /// <summary>
    /// 버튼 기반 탭 전환 컴포넌트.
    ///
    /// [사용법]
    /// 1. 오브젝트에 SimpleTabSwapper 컴포넌트를 추가한다.
    /// 2. Inspector의 tabs에 탭 개수만큼 항목을 추가하고, Key에 SimpleSwapType을 지정한다.
    ///    - button: 탭 버튼 (클릭 시 해당 탭으로 전환)
    ///    - swappers: 탭 선택/미선택 시 외형이 바뀔 SimpleSwapper 배열 (버튼 색상, 텍스트 등)
    ///    - content: 탭 선택 시 활성화할 컨텐츠 GameObject (SetActive로 제어됨)
    /// 3. selectedChildType / unselectedChildType 으로 선택·미선택 상태의 자식 swapper 타입을 지정한다.
    ///    (기본값: Normal / Disabled)
    /// 4. OnTabChanged 이벤트를 구독하면 탭 전환 시 콜백을 받을 수 있다.
    /// 5. 코드에서 Swap(SimpleSwapType)을 호출해 프로그래밍 방식으로 탭을 전환할 수도 있다.
    ///
    /// [예시 - 코드에서 탭 전환]
    /// <code>
    /// tabSwapper.Swap(SimpleSwapType.Custom_0);
    /// tabSwapper.OnTabChanged += type => Debug.Log($"탭 전환: {type}");
    /// </code>
    /// </summary>
    public class SimpleTabSwapper : SimpleSwapper
    {
        [Serializable]
        public class Tab
        {
            public Button button;
            public SimpleSwapper[] swappers;
            [Tooltip("탭 선택 시 활성화할 컨텐츠 (없으면 무시)")]
            public GameObject content;
        }

        [SerializeField] private SerializableDictionary<SimpleSwapType, Tab> tabs;
        [SerializeField] private SimpleSwapType selectedChildType = SimpleSwapType.Normal;
        [SerializeField] private SimpleSwapType unselectedChildType = SimpleSwapType.Disabled;

        public event Action<SimpleSwapType> OnTabChanged;

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return tabs.Keys;
        }

        protected override void Awake()
        {
            base.Awake();

            foreach (var pair in tabs)
            {
                var type = pair.Key;
                pair.Value.button.onClick.AddListener(() => Swap(type));
            }

            Refresh();
        }

        public override void Swap(SimpleSwapType swapType)
        {
            if (currentType == swapType) return;
            if (!tabs.ContainsKey(swapType)) return;

            currentType = swapType;
            Refresh();
            OnTabChanged?.Invoke(swapType);
        }

        private void Refresh()
        {
            foreach (var pair in tabs)
            {
                bool isSelected = pair.Key == currentType;

                if (pair.Value.swappers != null)
                {
                    pair.Value.swappers.Swap(isSelected ? selectedChildType : unselectedChildType);
                }

                if (pair.Value.content != null)
                {
                    pair.Value.content.SetActive(isSelected);
                }
            }
        }
    }
}
