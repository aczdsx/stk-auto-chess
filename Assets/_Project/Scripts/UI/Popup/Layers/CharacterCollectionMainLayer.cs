using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum CharacterCollectionMainLayerTabType
    {
        All,
        EARTH = 1,
        WIND = 2,
        WATER = 3,
        FIRE = 4,
        DARK = 5,
        LIGHT = 6,
    }

    public class CharacterCollectionMainLayer : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _backButton;

        [Space(10)]
        [SerializeField] private ScrollRect _characterScrollRect;
        [SerializeField] private GameObject _characterCardSlotObject;

        private CharacterCollectionMainLayerTabType _currentMainLayerTabType = CharacterCollectionMainLayerTabType.All;

        private List<CharacterInfo> _totalCharacterList;      // 전체 캐릭터 리스트
        private List<CharacterCardSlot> _characterCardSlotList = new List<CharacterCardSlot>();

        private CharacterCollectionPopup _parentCollectionPopup;

        [SerializeField]
        private GameObject _guideObj;

        private void Awake()
        {
            _backButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickBackButton()).AddTo(this);
        }

        public void InitLayer(CharacterCollectionPopup _parentPopup)
        {
            _parentCollectionPopup = _parentPopup;

            _currentMainLayerTabType = CharacterCollectionMainLayerTabType.All;

            SetCharacterCollectionUI();
        }

        public void RefreshLayer()
        {
            SetCharacterCollectionUI();
        }

        public void OnClickTabToggleButton(int tabIndex)
        {
            _currentMainLayerTabType = (CharacterCollectionMainLayerTabType)tabIndex;

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
                isGuide = _specGuideMissionData.guide_mission_type == GuideMissionType.LEVELUP_CHARACTER_TARGET || _specGuideMissionData.guide_mission_type == GuideMissionType.SET_LV_CHARACTER_TARGET;
            if (_guideObj != null)
                _guideObj.SetActive(isGuide);

            var isTutorial = TutorialManager.Instance.IsTutorial;

            foreach (var characterData in _totalCharacterList)
            {
                GameObject newCardObject = Instantiate(_characterCardSlotObject, _characterScrollRect.content);
                CharacterCardSlot slot = newCardObject.GetComponent<CharacterCardSlot>();
                slot.SetCharcacterSlot(characterData, _parentCollectionPopup);

                if (isGuide && _guideObj != null)
                {
                    if (_specGuideMissionData.sub_key == characterData.id)
                    {
                        _guideObj.transform.parent = newCardObject.transform;
                        _guideObj.transform.localPosition = Vector3.zero;
                    }
                }

                // 튜토리얼 모드일 때 TutorialTarget 동적 등록
                if (isTutorial)
                {
                    var tutorialTarget = newCardObject.GetComponent<TutorialTarget>()
                                         ?? newCardObject.AddComponent<TutorialTarget>();
                    tutorialTarget.SetTargetId($"CharacterCardSlot_{characterData.id}");
                }

                _characterCardSlotList.Add(slot);
            }

            _characterScrollRect.verticalNormalizedPosition = 1;
        }

        private void FilterCharacterList(CharacterCollectionMainLayerTabType targetType)
        {
            _characterCardSlotList.ForEach(slot =>
            {
                if (targetType == CharacterCollectionMainLayerTabType.All)
                {
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive((int)slot.SpecCharacterData.character_element_type == (int)targetType);
                }
            });

            _characterScrollRect.verticalNormalizedPosition = 1;
        }

        private void ClearList()
        {
            _characterCardSlotList.Clear();

            BMUtil.RemoveChildObjects(_characterScrollRect.content);
        }

        private void OnClickBackButton()
        {
            SceneUILayerManager.Instance.PopUILayer("CharacterCollectionPopup");
        }
    }
}
