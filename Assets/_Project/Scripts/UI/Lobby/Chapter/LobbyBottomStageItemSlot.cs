using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class LobbyBottomStageItemSlot : CachedMonoBehaviour
    {
        [FormerlySerializedAs("_noramlStateHeight")]
        [Header("[Stage - Common]")]
        [SerializeField] private int _normalStateHeight;        // 일반 스테이지 높이
        [SerializeField] private int _doneStateHeight;          // 완료 스테이지 높이
        [SerializeField] private int _currentStateHeight;       // 현재 스테이지 높이

        [Header("[State - Normal]")]
        [SerializeField] private GameObject _normalLayerObject;
        [SerializeField] private GameObject _normalStarLayerObject;
        [SerializeField] private GameObject _normalCharacterLayerObject;
        [SerializeField] private TextMeshProUGUI _normalStageNumberText;
        [SerializeField] private List<GameObject> _normalStarObjectList;

        [Header("[State - Boss]")]
        [SerializeField] private GameObject _bossLayerObject;
        [SerializeField] private GameObject _bossStarLayerObject;
        [SerializeField] private GameObject _bossCharacterLayerObject;
        [SerializeField] private TextMeshProUGUI _bossStageNumberText;
        [SerializeField] private List<GameObject> _bossStarObjectList;

        private Stage _specStageData;
        private UserStage _userStageData;

        private bool _isClearStage;     // 클리어 한 스테이지 여부 체크용
        private bool _isCurrentStage;   // 현재 스테이지 여부 체크용
        private bool _isValidStage;     // 유효한 스테이지 여부 체크용

        private int _resultCustomHeight;    // 스테이지 상태에 따른 최종 높이 값

        public void SetStageItemSlot(Stage data)
        {
            if (data == null) return;

            _specStageData = data;
            _userStageData = UserDataManager.Instance.GetUserStage(_specStageData.id);

            _isClearStage = UserDataManager.Instance.IsClearStage(_specStageData.id);
            _isCurrentStage = UserDataManager.Instance.GetCurrentStageId() == _specStageData.id;

            SetStageState();
        }

        public void RefershSlot()
        {
            SetStageState();
        }

        private void SetStageState()
        {
            ClearSlot();

            _resultCustomHeight = _isClearStage ? _doneStateHeight : (_isCurrentStage ? _currentStateHeight : _normalStateHeight);
            _isValidStage = _userStageData != null && _userStageData.StarCount > 0;

            switch (_specStageData.stage_type)
            {
                case StageType.BATTLE_NORMAL:
                    SetNormalLayerState();
                    break;
                case StageType.BATTLE_ELITE:
                    break;
                case StageType.BATTLE_BOSS:
                    SetBossLayerState();
                    break;
                case StageType.CHEST:
                    break;
            }
        }

        private void SetNormalLayerState()
        {
            _normalStageNumberText.text = _specStageData.stage_number.ToString();

            var originNormalSizeDelta = _normalLayerObject.GetComponent<RectTransform>().sizeDelta;
            _normalLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originNormalSizeDelta.x, _resultCustomHeight);

            _normalLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_NORMAL);
            _normalStarLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _normalStarObjectList.Count; i++)
                {
                    _normalStarObjectList[i].SetActive(i < _userStageData.StarCount);
                }
            }

            _normalCharacterLayerObject.SetActive(_isCurrentStage);
        }

        private void SetBossLayerState()
        {
            _bossStageNumberText.text = _specStageData.stage_number.ToString();

            var originBossSizeDelta = _bossLayerObject.GetComponent<RectTransform>().sizeDelta;
            _bossLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originBossSizeDelta.x, _resultCustomHeight);

            _bossLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_BOSS);
            _bossStarLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _bossStarObjectList.Count; i++)
                {
                    _bossStarObjectList[i].SetActive(i < _userStageData.StarCount);
                }
            }

            _bossCharacterLayerObject.SetActive(_isCurrentStage);
        }

        private void ClearSlot()
        {
            _normalLayerObject.SetActive(false);
            _normalStarLayerObject.SetActive(false);

            _bossLayerObject.SetActive(false);
            _bossStarLayerObject.SetActive(false);
        }
    }
}
