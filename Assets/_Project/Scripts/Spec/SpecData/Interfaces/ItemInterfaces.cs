using System;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 아이템 ID를 나타내는 구조체
    /// int 대신 사용하여 타입 안정성을 제공하고, 확장 메서드를 통해 아이템 종류를 판별할 수 있습니다.
    /// </summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public int Value { get; }

        public ItemId(int value)
        {
            Value = value;
        }

        // int와의 암시적 변환
        public static implicit operator int(ItemId itemId) => itemId.Value;
        public static implicit operator uint(ItemId itemId) => (uint)itemId.Value;
        public static implicit operator ItemId(int value) => new ItemId(value);

        // 동등성 비교
        public bool Equals(ItemId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ItemId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();

        // 비교 연산자
        public static bool operator ==(ItemId left, ItemId right) => left.Equals(right);
        public static bool operator !=(ItemId left, ItemId right) => !left.Equals(right);
    }

    /// <summary>
    /// ItemCurrencyTable, ItemConsumableTable, ItemMaterialTable이 공통으로 구현하는 인터페이스
    /// 세 클래스의 모든 공통 필드를 프로퍼티로 제공하여, 동일한 방식으로 접근할 수 있게 합니다.
    /// </summary>
    public interface ISpecItemInfo
    {
        /// <summary>
        /// 아이템 ID를 반환합니다.
        /// ItemCurrencyTable은 currency_id, ItemConsumableTable과 ItemMaterialTable은 item_id를 반환합니다.
        /// </summary>
        ItemId GetItemId();

        /// <summary>
        /// #SheetIndex
        /// </summary>
        int id { get; }

        /// <summary>
        /// 아이템의 그레이드
        /// </summary>
        GradeType item_grade { get; }

        /// <summary>
        /// 아이템 이름 토큰
        /// </summary>
        string name_token { get; }

        /// <summary>
        /// 아이템 설명 토큰
        /// </summary>
        string desc_token { get; }

        /// <summary>
        /// 아이템 아이콘
        /// </summary>
        string icon { get; }

        /// <summary>
        /// 아이템 백그라운드
        /// </summary>
        string bg { get; }

        /// <summary>
        /// 최소 보유량
        /// </summary>
        int min_stock { get; }

        /// <summary>
        /// 최대 보유량
        /// </summary>
        int max_stock { get; }

        /// <summary>
        /// 인벤토리 표시 유무
        /// </summary>
        bool info_show { get; }

        /// <summary>
        /// 사용 가능 유무
        /// </summary>
        bool is_consumable { get; }

        /// <summary>
        /// 합성 요구량
        /// </summary>
        int merge_count { get; }

        /// <summary>
        /// 세부 ID 특정 가능 여부
        /// </summary>
        bool is_common { get; }

        /// <summary>
        /// 유료 재화 여부 (ItemCurrencyTable, ItemMaterialTable 전용, ItemConsumableTable은 false 반환)
        /// </summary>
        bool is_premium { get; }
    }

    /// <summary>
    /// ItemCurrencyTable의 ISpecItemInfo 인터페이스 구현
    /// </summary>
    public partial class ItemCurrencyTable : ISpecItemInfo
    {
        public ItemId GetItemId() => new ItemId(currency_id);

        int ISpecItemInfo.id => id;
        GradeType ISpecItemInfo.item_grade => item_grade;
        string ISpecItemInfo.name_token => currency_name_token;
        string ISpecItemInfo.desc_token => currency_desc_token;
        string ISpecItemInfo.icon => item_icon;
        string ISpecItemInfo.bg => item_bg;
        int ISpecItemInfo.min_stock => min_stock;
        int ISpecItemInfo.max_stock => max_stock;
        bool ISpecItemInfo.info_show => info_show;
        bool ISpecItemInfo.is_consumable => is_consumable;
        int ISpecItemInfo.merge_count => merge_count;
        bool ISpecItemInfo.is_common => is_common;
        bool ISpecItemInfo.is_premium => is_premium;
    }

    /// <summary>
    /// ItemConsumableTable의 ISpecItemInfo 인터페이스 구현
    /// </summary>
    public partial class ItemConsumableTable : ISpecItemInfo
    {
        public ItemId GetItemId() => new ItemId(item_id);

        int ISpecItemInfo.id => id;
        GradeType ISpecItemInfo.item_grade => item_grade;
        string ISpecItemInfo.name_token => item_name_token;
        string ISpecItemInfo.desc_token => item_desc_token;
        string ISpecItemInfo.icon => item_icon;
        string ISpecItemInfo.bg => item_bg;
        int ISpecItemInfo.min_stock => min_stock;
        int ISpecItemInfo.max_stock => max_stock;
        bool ISpecItemInfo.info_show => info_show;
        bool ISpecItemInfo.is_consumable => is_consumable;
        int ISpecItemInfo.merge_count => merge_count;
        bool ISpecItemInfo.is_common => is_common;
        bool ISpecItemInfo.is_premium => false; // ItemConsumableTable에는 is_premium 필드가 없음
    }

    /// <summary>
    /// ItemMaterialTable의 ISpecItemInfo 인터페이스 구현
    /// </summary>
    public partial class ItemMaterialTable : ISpecItemInfo
    {
        public ItemId GetItemId() => new ItemId(item_id);

        int ISpecItemInfo.id => id;
        GradeType ISpecItemInfo.item_grade => item_grade;
        string ISpecItemInfo.name_token => currency_name_token;
        string ISpecItemInfo.desc_token => consumable_desc_token;
        string ISpecItemInfo.icon => currency_icon;
        string ISpecItemInfo.bg => currency_bg;
        int ISpecItemInfo.min_stock => min_stock;
        int ISpecItemInfo.max_stock => max_stock;
        bool ISpecItemInfo.info_show => info_show;
        bool ISpecItemInfo.is_consumable => is_consumable;
        int ISpecItemInfo.merge_count => merge_count;
        bool ISpecItemInfo.is_common => is_common;
        bool ISpecItemInfo.is_premium => is_premium;
    }
}
