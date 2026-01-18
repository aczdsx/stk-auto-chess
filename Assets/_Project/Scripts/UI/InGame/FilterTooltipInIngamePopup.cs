using System;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 인게임 캐릭터 필터링 팝업
    /// 속성(Element), 성군(Stella) 필터를 선택하여 하단 캐릭터 목록 필터링
    /// </summary>
    public class FilterTooltipInIngamePopup : UILayer
    {
        [Header("Buttons")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimButton;

        [Header("Element Filter (속성)")]
        [SerializeField] private CAButton _elementAllButton;
        [SerializeField] private GameObject _elementAllOnObject;
        [SerializeField] private GameObject _elementAllOffObject;
        [SerializeField] private List<CAButton> _elementButtons; // FIRE, WIND, LIGHTNING, EARTH, WATER 순서
        [SerializeField] private List<GameObject> _elementOnObjects; // 선택 시 표시
        [SerializeField] private List<GameObject> _elementOffObjects; // 미선택 시 표시

        [Header("Stella Filter (성군)")]
        [SerializeField] private CAButton _stellaAllButton;
        [SerializeField] private GameObject _stellaAllOnObject;
        [SerializeField] private GameObject _stellaAllOffObject;
        [SerializeField] private List<CAButton> _stellaButtons; // NOBLESSE, TROUBLESHOOTER, SUPERNOVA 순서
        [SerializeField] private List<GameObject> _stellaOnObjects; // 선택 시 표시
        [SerializeField] private List<GameObject> _stellaOffObjects; // 미선택 시 표시

        // 속성 시너지 타입 매핑 (버튼 인덱스 순서)
        private readonly SynergyType[] _elementTypes =
        {
            SynergyType.FIRE,
            SynergyType.WIND,
            SynergyType.LIGHTNING,
            SynergyType.EARTH,
            SynergyType.WATER
        };

        // 성군 시너지 타입 매핑 (버튼 인덱스 순서)
        private readonly SynergyType[] _stellaTypes =
        {
            SynergyType.NOBLESSE,
            SynergyType.TROUBLESHOOTER,
            SynergyType.SUPERNOVA
        };

        private HashSet<SynergyType> _selectedElements = new();
        private HashSet<SynergyType> _selectedStellas = new();
        private Action<HashSet<SynergyType>, HashSet<SynergyType>> _onFilterChanged;

        /// <summary>
        /// 팝업 파라미터
        /// </summary>
        public readonly struct FilterParam
        {
            public readonly HashSet<SynergyType> SelectedElements;
            public readonly HashSet<SynergyType> SelectedStellas;
            public readonly Action<HashSet<SynergyType>, HashSet<SynergyType>> OnFilterChanged;

            public FilterParam(
                HashSet<SynergyType> selectedElements,
                HashSet<SynergyType> selectedStellas,
                Action<HashSet<SynergyType>, HashSet<SynergyType>> onFilterChanged)
            {
                SelectedElements = selectedElements;
                SelectedStellas = selectedStellas;
                OnFilterChanged = onFilterChanged;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // 닫기 버튼
            if (_closeButton != null)
                _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickClose()).AddTo(this);
            if (_dimButton != null)
                _dimButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickClose()).AddTo(this);

            // 속성 ALL 버튼
            if (_elementAllButton != null)
                _elementAllButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickElementAll()).AddTo(this);

            // 속성 개별 버튼
            for (int i = 0; i < _elementButtons.Count; i++)
            {
                int index = i;
                if (_elementButtons[i] != null)
                    _elementButtons[i].OnClickAsObservable().Subscribe((this, index), (_, state) => state.Item1.OnClickElement(state.index)).AddTo(this);
            }

            // 성군 ALL 버튼
            if (_stellaAllButton != null)
                _stellaAllButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickStellaAll()).AddTo(this);

            // 성군 개별 버튼
            for (int i = 0; i < _stellaButtons.Count; i++)
            {
                int index = i;
                if (_stellaButtons[i] != null)
                    _stellaButtons[i].OnClickAsObservable().Subscribe((this, index), (_, state) => state.Item1.OnClickStella(state.index)).AddTo(this);
            }
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            if (param is FilterParam filterParam)
            {
                // 기존 필터 상태 복사
                _selectedElements = new HashSet<SynergyType>(filterParam.SelectedElements);
                _selectedStellas = new HashSet<SynergyType>(filterParam.SelectedStellas);
                _onFilterChanged = filterParam.OnFilterChanged;
            }
            else
            {
                _selectedElements = new HashSet<SynergyType>();
                _selectedStellas = new HashSet<SynergyType>();
                _onFilterChanged = null;
            }

            UpdateUI();
        }

        private void OnClickClose()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickElementAll()
        {
            // 모두 선택된 상태면 → 모두 해제, 아니면 → 모두 선택
            if (_selectedElements.Count == _elementTypes.Length)
            {
                _selectedElements.Clear();
            }
            else
            {
                _selectedElements.Clear();
                foreach (var type in _elementTypes)
                    _selectedElements.Add(type);
            }
            UpdateUI();
            NotifyFilterChanged();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        private void OnClickElement(int index)
        {
            if (index < 0 || index >= _elementTypes.Length) return;

            var type = _elementTypes[index];

            // 토글
            if (_selectedElements.Contains(type))
                _selectedElements.Remove(type);
            else
                _selectedElements.Add(type);

            UpdateUI();
            NotifyFilterChanged();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        private void OnClickStellaAll()
        {
            // 모두 선택된 상태면 → 모두 해제, 아니면 → 모두 선택
            if (_selectedStellas.Count == _stellaTypes.Length)
            {
                _selectedStellas.Clear();
            }
            else
            {
                _selectedStellas.Clear();
                foreach (var type in _stellaTypes)
                    _selectedStellas.Add(type);
            }
            UpdateUI();
            NotifyFilterChanged();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        private void OnClickStella(int index)
        {
            if (index < 0 || index >= _stellaTypes.Length) return;

            var type = _stellaTypes[index];

            // 토글
            if (_selectedStellas.Contains(type))
                _selectedStellas.Remove(type);
            else
                _selectedStellas.Add(type);

            UpdateUI();
            NotifyFilterChanged();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        private void UpdateUI()
        {
            // 속성 ALL 버튼 상태 (모두 선택 시 On)
            bool isAllElementSelected = _selectedElements.Count == _elementTypes.Length;
            if (_elementAllOnObject != null)
                _elementAllOnObject.SetActive(isAllElementSelected);
            if (_elementAllOffObject != null)
                _elementAllOffObject.SetActive(!isAllElementSelected);

            // 속성 개별 선택 표시 업데이트
            for (int i = 0; i < _elementTypes.Length; i++)
            {
                bool isSelected = _selectedElements.Contains(_elementTypes[i]);

                if (i < _elementOnObjects.Count && _elementOnObjects[i] != null)
                    _elementOnObjects[i].SetActive(isSelected);

                if (i < _elementOffObjects.Count && _elementOffObjects[i] != null)
                    _elementOffObjects[i].SetActive(!isSelected);
            }

            // 성군 ALL 버튼 상태 (모두 선택 시 On)
            bool isAllStellaSelected = _selectedStellas.Count == _stellaTypes.Length;
            if (_stellaAllOnObject != null)
                _stellaAllOnObject.SetActive(isAllStellaSelected);
            if (_stellaAllOffObject != null)
                _stellaAllOffObject.SetActive(!isAllStellaSelected);

            // 성군 개별 선택 표시 업데이트
            for (int i = 0; i < _stellaTypes.Length; i++)
            {
                bool isSelected = _selectedStellas.Contains(_stellaTypes[i]);

                if (i < _stellaOnObjects.Count && _stellaOnObjects[i] != null)
                    _stellaOnObjects[i].SetActive(isSelected);

                if (i < _stellaOffObjects.Count && _stellaOffObjects[i] != null)
                    _stellaOffObjects[i].SetActive(!isSelected);
            }
        }

        private void NotifyFilterChanged()
        {
            _onFilterChanged?.Invoke(_selectedElements, _selectedStellas);
        }
    }
}
