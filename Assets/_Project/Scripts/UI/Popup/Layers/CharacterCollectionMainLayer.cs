using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class CharacterCollectionMainLayer : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _backButton;

        [Space(10)]
        [SerializeField] private TableView _characterTableView;
        [SerializeField] private CharacterCardSlot _characterCardSlotPrefab;

        private SynergyType _currentMainLayerTabType = SynergyType.NORMAL;

        private List<CharacterInfo> _totalCharacterList;      // 전체 캐릭터 리스트
        private List<CharacterInfo> _filteredCharacterList = new List<CharacterInfo>();
        private TableViewController<CharacterInfo, CharacterCardSlot> _characterController;

        [SerializeField]
        private GameObject _guideObj;

        private void Awake()
        {
            _backButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickBackButton()).AddTo(this);
        }

        public void InitLayer()
        {
            _currentMainLayerTabType = SynergyType.NORMAL;

            SetCharacterCollectionUI();
        }

        public void RefreshLayer()
        {
            SetCharacterCollectionUI();
        }

        /// <summary>
        /// Unity UI 이벤트는 int/float/string/bool/Object만 인자로 넘길 수 있어서
        /// int로 받은 뒤 SynergyType으로 캐스팅합니다. (인스펙터에서 m_IntArgument로 enum 값 지정)
        /// </summary>
        public void OnClickTabToggleButton(int synergyTypeInt)
        {
            _currentMainLayerTabType = (SynergyType)synergyTypeInt;

            FilterCharacterList(_currentMainLayerTabType);
        }

        private void SetCharacterCollectionUI()
        {
            ClearList();

            _totalCharacterList = SpecDataManager.Instance.GetCharacterListByCharacterType(CharacterType.CHARACTER);

            // 정렬 (획득 여부-> id 값 -> 조각 획득 여부)
            _totalCharacterList = _totalCharacterList.OrderByDescending(data => ServerDataManager.Instance.Character.HasCharacter(data.id))
                .ThenByDescending(data => data.id).ToList();

            var guideMission = ServerDataManager.Instance.GuideMission;
            var _specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get((int)guideMission.GuideMissionId);
            bool isGuide = false;
            if (_specGuideMissionData != null)
                isGuide = _specGuideMissionData.guide_mission_type == GuideMissionType.LEVELUP_CHARACTER_TARGET;
            if (_guideObj != null)
                _guideObj.SetActive(isGuide);

            var isTutorial = TutorialManager.Instance.IsTutorial;

            _filteredCharacterList.Clear();
            _filteredCharacterList.AddRange(_totalCharacterList);

            _characterController = _characterTableView.CreateController<CharacterInfo, CharacterCardSlot>()
                .WithData(_filteredCharacterList)
                .WithCellPrefab(_characterCardSlotPrefab.gameObject)
                .WithCellSize(_characterCardSlotPrefab.GetComponent<RectTransform>().rect.size)
                .OnBind((cell, characterData, index) =>
                {
                    cell.SetCharcacterSlot(characterData, _filteredCharacterList);

                    // 가이드 오브젝트 배치
                    if (isGuide && _guideObj != null && _specGuideMissionData.sub_key == characterData.id)
                    {
                        _guideObj.transform.SetParent(cell.transform, false);
                        _guideObj.transform.localPosition = Vector3.zero;
                    }

                    // 튜토리얼 모드일 때 TutorialTarget 동적 등록
                    if (isTutorial)
                    {
                        var tutorialTarget = cell.GetComponent<TutorialTarget>()
                                             ?? cell.gameObject.AddComponent<TutorialTarget>();
                        tutorialTarget.SetTargetId($"CharacterCardSlot_{characterData.id}");
                    }
                })
                .OnCellReleased((index, cell) =>
                {
                    cell.ClearCardSlot();
                })
                .Build();
        }

        private void FilterCharacterList(SynergyType targetType)
        {
            _filteredCharacterList.Clear();

            if (targetType == SynergyType.NORMAL)
                _filteredCharacterList.AddRange(_totalCharacterList);
            else
                _filteredCharacterList.AddRange(
                    _totalCharacterList.Where(c => c.character_element_type == targetType));

            _characterTableView.RefreshAll(resetPos: true);
        }

        private void ClearList()
        {
            _characterController?.Detach();
            _characterController = null;
            _filteredCharacterList.Clear();
        }

        private void OnClickBackButton()
        {
            SceneUILayerManager.Instance.PopUILayer("CharacterCollectionPopup");
        }
    }
}
