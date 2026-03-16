using System;
using System.Collections.Generic;
using CookApps.AutoChess;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class PresetInGameSlot : CachedMonoBehaviour
    {
        [Header("Character Save Load Buttons")]
        [SerializeField] private CAButton _deleteButton;
        [SerializeField] private CAButton _editSaveButton;
        [SerializeField] private CAButton _loadButton;

        [Header("Preset Name")]
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private CAButton _nameEditButton;


        [Header("Character Thumbnails")]
        [SerializeField] private SynergyTooltipImageGroup _characterImageGroup;

        [Header("Synergy Icons")]
        [SerializeField] private SynergyUI _synergyIconPrefab;
        [SerializeField] private Transform _synergyIconContainer;

        [Header("CP")] 
        [SerializeField] private TextMeshProUGUI _cpText;

        public struct SynergyDisplayInfo
        {
            public SynergyType Type;
            public bool IsActive;
        }

        public class SlotData
        {
            public int PresetIndex;
            public PresetSlotData Preset;
            public List<SynergyDisplayInfo> Synergies;
            public List<SynergyTooltipImageGroup.CharacterSlotData> Characters;
            public int TotalCP;
            public Action<int> OnSave;
            public Action<int> OnLoad;
            public Action<int> OnDelete;
            public Action<int, string> OnRename;
            public Action<int, int> OnReorder;
        }

        private SlotData _data;
        private readonly List<SynergyUI> _iconPool = new();
        private ReorderableSlotDragHandler _dragHandler;

        public void Bind(SlotData data)
        {
            _data = data;

            _editSaveButton?.onClick.AddListener(OnSaveClicked);
            _loadButton?.onClick.AddListener(OnLoadClicked);
            _deleteButton?.onClick.AddListener(OnDeleteClicked);
            _nameEditButton?.onClick.AddListener(OnNameEditClicked);

            BindName(data);
            BindCharacters(data.Characters);
            BindSynergies(data.Synergies);
            BindCP(data.TotalCP);
            BindDragHandler(data);
        }

        public void Clear()
        {
            if (_dragHandler != null)
            {
                _dragHandler.OnReordered -= OnReorderSlot;
            }

            _editSaveButton?.onClick.RemoveListener(OnSaveClicked);
            _loadButton?.onClick.RemoveListener(OnLoadClicked);
            _deleteButton?.onClick.RemoveListener(OnDeleteClicked);
            _nameEditButton?.onClick.RemoveListener(OnNameEditClicked);

            if (_nameInputField != null)
                _nameInputField.onEndEdit.RemoveListener(OnNameChanged);

            if (_characterImageGroup != null)
                _characterImageGroup.gameObject.SetActive(false);

            if (_synergyIconContainer != null)
                _synergyIconContainer.gameObject.SetActive(false);

            for (int i = 0; i < _iconPool.Count; i++)
                _iconPool[i].gameObject.SetActive(false);
        }

        private void BindCharacters(List<SynergyTooltipImageGroup.CharacterSlotData> characters)
        {
            if (_characterImageGroup == null) return;

            bool hasCharacters = characters is { Count: > 0 };
            _characterImageGroup.gameObject.SetActive(hasCharacters);

            if (!hasCharacters) return;

            _characterImageGroup.SetCharacters(characters);
        }

        private void BindSynergies(List<SynergyDisplayInfo> synergies)
        {
            if (_synergyIconContainer == null) return;

            bool hasSynergies = synergies is { Count: > 0 };
            _synergyIconContainer.gameObject.SetActive(hasSynergies);

            if (!hasSynergies)
            {
                for (int i = 0; i < _iconPool.Count; i++)
                    _iconPool[i].gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < synergies.Count; i++)
            {
                var icon = GetOrCreateIcon(i);
                icon.SetSynergyUI(synergies[i].Type, synergies[i].IsActive);
                icon.gameObject.SetActive(true);
            }
            for (int i = synergies.Count; i < _iconPool.Count; i++)
                _iconPool[i].gameObject.SetActive(false);
        }

        private SynergyUI GetOrCreateIcon(int index)
        {
            if (index < _iconPool.Count) return _iconPool[index];
            var icon = Instantiate(_synergyIconPrefab, _synergyIconContainer);
            _iconPool.Add(icon);
            return icon;
        }

        private void BindCP(int totalCP)
        {
            if (_cpText == null) return;
            _cpText.text = totalCP > 0 ? totalCP.ToString("n0") : "";
        }

        private void BindName(SlotData data)
        {
            if (_nameInputField == null) return;
            _nameInputField.onEndEdit.RemoveListener(OnNameChanged);
            var presetName = data.Preset?.Name;
            _nameInputField.text = string.IsNullOrEmpty(presetName)
                ? $"Preset_{data.PresetIndex + 1}"
                : presetName;
            _nameInputField.interactable = false;
            _nameInputField.onEndEdit.AddListener(OnNameChanged);
        }

        private void OnNameEditClicked()
        {
            if (_nameInputField == null) return;
            _nameInputField.interactable = true;
            _nameInputField.ActivateInputField();
        }

        private void OnNameChanged(string newName)
        {
            if (_nameInputField != null)
                StartCoroutine(DisableInputFieldNextFrame());

            _data.OnRename?.Invoke(_data.PresetIndex, newName);
            ToastManager.Instance.ShowToastByTokenKey("MSG_PRESET_NAME_SAVED");
        }

        private System.Collections.IEnumerator DisableInputFieldNextFrame()
        {
            yield return null;
            if (_nameInputField != null)
                _nameInputField.interactable = false;
        }
        private void BindDragHandler(SlotData data)
        {
            _dragHandler = GetComponent<ReorderableSlotDragHandler>();
            if (_dragHandler == null) return;

            bool hasPreset = data.Preset?.Units is { Count: > 0 };
            _dragHandler.SetDraggable(hasPreset);
            _dragHandler.OnReordered -= OnReorderSlot;
            _dragHandler.OnReordered += OnReorderSlot;
        }

        private void OnReorderSlot(int fromIndex, int toIndex)
        {
            _data?.OnReorder?.Invoke(fromIndex, toIndex);
        }

        private void OnSaveClicked() => _data.OnSave?.Invoke(_data.PresetIndex);
        private void OnLoadClicked() => _data.OnLoad?.Invoke(_data.PresetIndex);
        private void OnDeleteClicked() => _data.OnDelete?.Invoke(_data.PresetIndex);
    }
}
