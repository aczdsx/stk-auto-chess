using System.Collections.Generic;
using MemoryPack;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 인게임 프리셋 로컬 저장 데이터.
    /// 보드 위 유닛 배치를 최대 <see cref="MaxPresetCount"/>개 슬롯에 저장/불러오기/삭제할 수 있다.
    /// </summary>
    [MemoryPackable]
    public partial class ClientPresetData : ClientDataBase
    {
        public const string CategoryName = "client_preset";

        /// <summary>프리셋 슬롯 최대 개수.</summary>
        public const int MaxPresetCount = 5;

        public override string Category => CategoryName;

        /// <summary>싱글턴 접근. <see cref="ClientDataManager"/>에서 카테고리로 조회한다.</summary>
        public static ClientPresetData Get() =>
            ClientDataManager.Instance.GetData<ClientPresetData>(CategoryName);

        [MemoryPackOrder(0)] private MemoryPackList<PresetSlotData> _presets = new();

        /// <summary>저장된 프리셋 슬롯 목록 (읽기 전용).</summary>
        [MemoryPackIgnore]
        public IReadOnlyList<PresetSlotData> Presets => _presets;

        /// <summary>
        /// 지정 슬롯에 유닛 배치를 저장한다.
        /// </summary>
        /// <param name="index">슬롯 인덱스 (0 ~ <see cref="MaxPresetCount"/>-1).</param>
        /// <param name="units">저장할 유닛 배치 목록.</param>
        public void SavePreset(int index, List<PresetUnitPlacement> units)
        {
            if (index < 0 || index >= MaxPresetCount) return;

            while (_presets.Count <= index)
                _presets.Add(null);

            var packList = new MemoryPackList<PresetUnitPlacement>();
            packList.AddRange(units);
            _presets[index] = new PresetSlotData { Units = packList };
            Debug.Log("PresetSave?????");
            SetDirty();
        }

        /// <summary>
        /// 지정 슬롯의 프리셋을 삭제한다.
        /// </summary>
        /// <param name="index">슬롯 인덱스.</param>
        public void DeletePreset(int index)
        {
            if (index < 0 || index >= _presets.Count) return;
            _presets[index] = null;
            SetDirty();
        }

        /// <summary>
        /// 지정 슬롯의 프리셋 이름을 변경한다.
        /// </summary>
        public void RenamePreset(int index, string name)
        {
            if (index < 0 || index >= _presets.Count) return;
            if (_presets[index] == null) return;
            _presets[index].Name = name ?? "";
            SetDirty();
        }

        /// <summary>
        /// 프리셋을 from 위치에서 빼내어 to 위치에 삽입한다. 나머지 슬롯은 밀린다.
        /// 이름은 슬롯 위치에 고정된다 (데이터만 이동).
        /// </summary>
        public void MovePreset(int from, int to)
        {
            if (from < 0 || from >= MaxPresetCount) return;
            if (to < 0 || to >= MaxPresetCount) return;
            if (from == to) return;

            int max = from > to ? from : to;
            while (_presets.Count <= max)
                _presets.Add(null);

            // from 위치의 프리셋을 빼서 to 위치에 삽입 (이름도 함께 이동)
            var item = _presets[from];
            _presets.RemoveAt(from);
            _presets.Insert(to, item);

            SetDirty();
        }

        /// <summary>
        /// 지정 슬롯에 유효한 프리셋이 저장되어 있는지 확인한다.
        /// </summary>
        public bool HasPreset(int index)
        {
            return index >= 0 && index < _presets.Count
                && _presets[index]?.Units is { Count: > 0 };
        }

        [MemoryPackOnDeserialized]
        private void OnDeserialized()
        {
            _presets ??= new();
        }
    }

    /// <summary>
    /// 프리셋 한 슬롯에 저장되는 데이터. 유닛 배치 목록을 담는다.
    /// </summary>
    [MemoryPackable]
    public partial class PresetSlotData
    {
        [MemoryPackOrder(0)] public MemoryPackList<PresetUnitPlacement> Units = new();
        [MemoryPackOrder(1)] public string Name = "";
    }

    /// <summary>
    /// 프리셋에 저장되는 개별 유닛 배치 정보.
    /// </summary>
    [MemoryPackable]
    public partial struct PresetUnitPlacement
    {
        /// <summary>캐릭터 스펙 ID.</summary>
        [MemoryPackOrder(0)] public int ChampionSpecId;

        /// <summary>보드 열 위치.</summary>
        [MemoryPackOrder(1)] public byte Col;

        /// <summary>보드 행 위치.</summary>
        [MemoryPackOrder(2)] public byte Row;
    }
}
