using System;
using System.Collections.Generic;
using CookApps.AutoChess;
using CookApps.AutoChess.View;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class PresetInGamePopup : UILayerPopupBase
    {
        [SerializeField] private PresetInGameSlot _slotPrefab;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private CAButton _closeButton;

        private readonly List<PresetInGameSlot> _slotPool = new();
        private PresetPopupParam _param;

        public class PresetPopupParam
        {
            public AutoChessViewBridge ViewBridge;
            public Func<GameWorld> GetWorld;
            public byte PlayerIndex;
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            if (param is PresetPopupParam popupParam)
            {
                _param = popupParam;
            }
        }

        private void Start()
        {
            _closeButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => SceneUILayerManager.Instance.PopUILayer(self))
                .AddTo(this);
        }

        private void OnEnable()
        {
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            var presetData = ClientPresetData.Get();

            // 슬롯 풀 확보
            while (_slotPool.Count < ClientPresetData.MaxPresetCount)
            {
                var slot = Instantiate(_slotPrefab, _slotContainer);
                slot.gameObject.SetActive(true);
                _slotPool.Add(slot);
            }

            // 바인딩 (드래그로 sibling 순서가 바뀔 수 있으므로 리셋)
            for (int i = 0; i < ClientPresetData.MaxPresetCount; i++)
            {
                bool hasPreset = presetData.HasPreset(i);
                var preset = hasPreset ? presetData.Presets[i] : null;

                _slotPool[i].transform.SetSiblingIndex(i);
                _slotPool[i].Clear();
                _slotPool[i].Bind(new PresetInGameSlot.SlotData
                {
                    PresetIndex = i,
                    Preset = preset,
                    Synergies = hasPreset ? CollectSynergies(preset) : null,
                    Characters = hasPreset ? CollectCharacters(preset) : null,
                    TotalCP = hasPreset ? CalculateTotalCP(preset) : 0,
                    OnSave = OnSavePreset,
                    OnLoad = hasPreset ? OnLoadPreset : null,
                    OnDelete = hasPreset ? OnDeletePreset : null,
                    OnRename = hasPreset ? OnRenamePreset : null,
                    OnReorder = OnReorderPreset,
                });
                _slotPool[i].gameObject.SetActive(true);
            }

            // 남은 슬롯 비활성화
            for (int i = ClientPresetData.MaxPresetCount; i < _slotPool.Count; i++)
            {
                _slotPool[i].Clear();
                _slotPool[i].gameObject.SetActive(false);
            }
        }

        // ── 순서 변경: 드래그 앤 드롭으로 슬롯 삽입+밀림 ──

        private void OnReorderPreset(int fromIndex, int toIndex)
        {
            ClientPresetData.Get().MovePreset(fromIndex, toIndex);
            RefreshSlots();
        }

        // ── 저장: 현재 보드 유닛 수집 → 프리셋에 저장 ──

        private void OnSavePreset(int index)
        {
            if (_param == null) return;
            var world = _param.GetWorld();
            if (world == null) return;

            var boardSlots = world.BoardSlots[_param.PlayerIndex];
            var collected = new HashSet<int>();
            var units = new List<PresetUnitPlacement>();

            for (int i = 0; i < boardSlots.Length; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId || !collected.Add(entityId)) continue;

                ref var unit = ref world.GetUnit(entityId);
                units.Add(new PresetUnitPlacement
                {
                    ChampionSpecId = unit.ChampionSpecId,
                    Col = unit.BoardCol,
                    Row = unit.BoardRow,
                    StarLevel = unit.StarLevel,
                });
            }

            if (units.Count == 0) return;

            var presetData = ClientPresetData.Get();
            presetData.SavePreset(index, units);

            // 새 프리셋이면 기본 이름 설정
            if (string.IsNullOrEmpty(presetData.Presets[index]?.Name))
                presetData.RenamePreset(index, $"Preset_{index + 1}");

            RefreshSlots();
        }

        // ── 배치: 보드 회수 → 프리셋 매칭 유닛 배치 → 팝업 닫기 ──

        private void OnLoadPreset(int index)
        {
            if (_param == null) return;
            var world = _param.GetWorld();
            if (world == null) return;

            var presetData = ClientPresetData.Get();
            if (!presetData.HasPreset(index)) return;

            var preset = presetData.Presets[index];
            byte playerIndex = _param.PlayerIndex;

            // 1. 보드 유닛 전부 회수
            var withdrawIds = new HashSet<int>();
            for (int i = 0; i < world.BoardSize; i++)
            {
                int entityId = world.BoardSlots[playerIndex][i];
                if (entityId != UnitData.InvalidId)
                    withdrawIds.Add(entityId);
            }
            foreach (var id in withdrawIds)
                _param.ViewBridge?.SendCommand(GameCommand.WithdrawUnit(playerIndex, id));

            // 2. 전체 유닛 수집 (이전 보드 + 벤치)
            var available = new List<int>();
            foreach (var id in withdrawIds)
                available.Add(id);

            var benchSlots = world.BenchSlots[playerIndex];
            for (int i = 0; i < benchSlots.Length; i++)
            {
                int entityId = benchSlots[i];
                if (entityId != UnitData.InvalidId)
                    available.Add(entityId);
            }

            // 3. 프리셋 순회 → ChampionSpecId 매칭 → PlaceUnit
            var used = new HashSet<int>();
            foreach (var placement in preset.Units)
            {
                int matchId = FindMatchingEntity(world, available, used, placement.ChampionSpecId);
                if (matchId == UnitData.InvalidId) continue;

                used.Add(matchId);
                _param.ViewBridge?.SendCommand(
                    GameCommand.PlaceUnit(playerIndex, matchId, placement.Col, placement.Row));
            }

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private int FindMatchingEntity(GameWorld world, List<int> available, HashSet<int> used, int championSpecId)
        {
            foreach (var entityId in available)
            {
                if (used.Contains(entityId)) continue;
                ref var unit = ref world.GetUnit(entityId);
                if (unit.ChampionSpecId == championSpecId)
                    return entityId;
            }
            return UnitData.InvalidId;
        }

        // ── CP 계산 ──

        private int CalculateTotalCP(PresetSlotData preset)
        {
            int totalCP = 0;
            foreach (var unit in preset.Units)
            {
                var spec = SpecDataManager.Instance.GetSpecCharacter(unit.ChampionSpecId);
                if (spec == null) continue;

                int star = unit.StarLevel > 0 ? unit.StarLevel : 1;
                int starMul = star switch { 2 => 180, 3 => 320, _ => 100 };
                int hp = spec.stat_hp * starMul / 100;
                int atk = spec.stat_atk * starMul / 100;
                int def = spec.stat_def;
                int adReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ad_reduce);
                int apReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ap_reduce);
                int atkSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                int critRate = Mathf.Max(0, (int)(spec.crit_rate * 100));
                int critPower = Mathf.Max(0, (int)(spec.crit_power * 100));
                int atkPierce = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                if (critRate <= 0) critRate = 25;
                if (critPower <= 0) critPower = 150;

                totalCP += CombatPowerCalculator.CalculateFromOldSpec(
                    hp, atk, def, adReduce, apReduce, atkSpeed, critRate, critPower, atkPierce);
            }
            return totalCP;
        }

        // ── 시너지 수집 ──

        private List<PresetInGameSlot.SynergyDisplayInfo> CollectSynergies(PresetSlotData preset)
        {
            var countMap = new Dictionary<SynergyType, int>();
            foreach (var unit in preset.Units)
            {
                var spec = SpecDataManager.Instance.GetSpecCharacter(unit.ChampionSpecId);
                if (spec == null) continue;

                if (spec.character_element_type != SynergyType.NONE)
                    countMap[spec.character_element_type] = countMap.GetValueOrDefault(spec.character_element_type) + 1;
                if (spec.character_stella_type != SynergyType.NONE)
                    countMap[spec.character_stella_type] = countMap.GetValueOrDefault(spec.character_stella_type) + 1;
            }
            var result = new List<PresetInGameSlot.SynergyDisplayInfo>();
            foreach (var (type, count) in countMap)
            {
                if (!SpecDataManager.Instance.TryGetSynergyDataByCount(type, count, out _, out _))
                    continue;
                result.Add(new PresetInGameSlot.SynergyDisplayInfo { Type = type, IsActive = true });
            }

            result.Sort((a, b) => countMap[b.Type].CompareTo(countMap[a.Type]));

            return result;
        }

        private List<SynergyTooltipImageGroup.CharacterSlotData> CollectCharacters(PresetSlotData preset)
        {
            var result = new List<SynergyTooltipImageGroup.CharacterSlotData>();
            foreach (var unit in preset.Units)
            {
                var spec = SpecDataManager.Instance.GetSpecCharacter(unit.ChampionSpecId);
                if (spec == null) continue;

                var grade = spec is CharacterInfo charInfo ? charInfo.grade_type : GradeType.COMMON;
                var synergyTypes = new List<SynergyType>();
                if (spec.character_element_type != SynergyType.NONE)
                    synergyTypes.Add(spec.character_element_type);
                if (spec.character_stella_type != SynergyType.NONE)
                    synergyTypes.Add(spec.character_stella_type);

                result.Add(new SynergyTooltipImageGroup.CharacterSlotData
                {
                    PrefabId = spec.prefab_id,
                    Grade = grade,
                    InBattle = true,
                    SynergyTypes = synergyTypes,
                });
            }
            return result;
        }

        // ── 이름 변경 ──

        private void OnRenamePreset(int index, string name)
        {
            ClientPresetData.Get().RenamePreset(index, name);
        }
        

        // ── 삭제: 확인 팝업 → 프리셋 데이터 제거 → 슬롯 갱신 ──

        private async void OnDeletePreset(int index)
        {
            var popupData = new SystemConfirmPopupData(
                "UI_SYSTEM_ALERT", "MSG_PRESET_DELETE", "UI_CONFIRM_BTN", "UI_CANCEL_BTN");
            var popup = await SceneUILayerManager.Instance
                .PushUILayerAsync<SystemConfirmPopup>(popupData);
            var isConfirmed = await popup.WaitForExit();
            if (isConfirmed is not true) return;

            ClientPresetData.Get().DeletePreset(index);
            RefreshSlots();
        }
    }
}
