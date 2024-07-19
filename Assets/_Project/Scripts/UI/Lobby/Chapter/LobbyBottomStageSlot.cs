using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class LobbyBottomStageSlot : CachedMonoBehaviour
    {
        [FormerlySerializedAs("_noramlStateHeight")]
        [Header("[Stage - Common]")]
        [SerializeField] private int _normalStateHeight;        // 일반 스테이지 높이
        [SerializeField] private int _doneStateHeight;          // 완료 스테이지 높이
        [SerializeField] private int _currentStateHeight;       // 현재 스테이지 높이
        [SerializeField] private CAButton _bottomStageSlotButton;   // 스테이지 선택 버튼

        [Header("[State - Normal]")]
        [SerializeField] private GameObject _normalLayerObject;
        [SerializeField] private GameObject _normalStarLayerObject;
        [SerializeField] private GameObject _normalCharacterLayerObject;
        [SerializeField] private GameObject _normalClearCheckObject;
        [SerializeField] private GameObject _normalNextStageCheckObject;
        [SerializeField] private TextMeshProUGUI _normalStageNumberText;
        [SerializeField] private List<GameObject> _normalStarObjectList;

        [Header("[State - Elite/Boss]")]
        [SerializeField] private GameObject _bossLayerObject;
        [SerializeField] private GameObject _bossStarLayerObject;
        [SerializeField] private GameObject _bossCharacterLayerObject;
        [SerializeField] private GameObject _bossClearCheckObject;
        [SerializeField] private GameObject _bossNextStageCheckObject;
        [SerializeField] private TextMeshProUGUI _bossStageNumberText;
        [SerializeField] private TextMeshProUGUI _bossStageTitleText;
        [SerializeField] private List<GameObject> _bossStarObjectList;

        private SpecStage _specStageData;
        private UserStage _userStageData;

        private bool _isClearStage;     // 클리어 한 스테이지 여부 체크용
        private bool _isCurrentStage;   // 현재 스테이지 여부 체크용
        private bool _isValidStage;     // 유효한 스테이지 여부 체크용
        private bool _isLatestPlayableStage;      // 플레이 가능한 다음 스테이지 체크용

        private int _resultCustomHeight;    // 스테이지 상태에 따른 최종 높이 값

        private void Awake()
        {
            _bottomStageSlotButton.onClick.AddListener(OnClickBottomStageSlot);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _bottomStageSlotButton.onClick.RemoveListener(OnClickBottomStageSlot);
        }

        public void SetStageItemSlot(SpecStage data)
        {
            if (data == null) return;

            _specStageData = data;
            _userStageData = UserDataManager.Instance.GetUserStage(_specStageData.stage_id);

            _isClearStage = UserDataManager.Instance.IsClearStage(_specStageData.stage_id);
            _isCurrentStage = UserDataManager.Instance.GetLastPlayStageID() == _specStageData.stage_id;

            int lastestStageID = UserDataManager.Instance.GetLatestClearUserStageID();
            var nextStageData = SpecDataManager.Instance.GetNextStageData(lastestStageID);
            _isLatestPlayableStage = nextStageData != null && (nextStageData.stage_id == _specStageData.stage_id);

            SetStageState();
        }

        public void RefershSlot()
        {
            _isClearStage = UserDataManager.Instance.IsClearStage(_specStageData.stage_id);
            _isCurrentStage = UserDataManager.Instance.GetLastPlayStageID() == _specStageData.stage_id;

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
                    SetBossLayerState();
                    break;
                case StageType.BATTLE_BOSS:
                    SetBossLayerState();
                    break;
                // case StageType.CHEST:
                //     SetNormalLayerState();  // temp..임시처리
                //     break;
            }
        }

        private void SetNormalLayerState()
        {
            _normalStageNumberText.text = _specStageData.stage_number.ToString();

            var originNormalSizeDelta = _normalLayerObject.GetComponent<RectTransform>().sizeDelta;
            _normalLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originNormalSizeDelta.x, _resultCustomHeight);

            _normalLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_NORMAL || _specStageData.stage_type == StageType.CHEST);
            _normalStarLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _normalStarObjectList.Count; i++)
                {
                    _normalStarObjectList[i].SetActive(i < _userStageData.StarCount);
                }
            }

            _normalCharacterLayerObject.SetActive(_isCurrentStage);
            _normalClearCheckObject.SetActive(_isClearStage);
            _normalNextStageCheckObject.SetActive(_isLatestPlayableStage);
        }

        private void SetBossLayerState()
        {
            _bossStageTitleText.text = _specStageData.stage_type == StageType.BATTLE_BOSS ? "BOSS" : "ELITE";
            _bossStageNumberText.text = _specStageData.stage_number.ToString();

            var originBossSizeDelta = _bossLayerObject.GetComponent<RectTransform>().sizeDelta;
            _bossLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originBossSizeDelta.x, _resultCustomHeight);

            _bossLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_BOSS || _specStageData.stage_type == StageType.BATTLE_ELITE);
            _bossStarLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _bossStarObjectList.Count; i++)
                {
                    _bossStarObjectList[i].SetActive(i < _userStageData.StarCount);
                }
            }

            _bossCharacterLayerObject.SetActive(_isCurrentStage);
            _bossClearCheckObject.SetActive(_isClearStage);
            _bossNextStageCheckObject.SetActive(_isLatestPlayableStage);
        }

        private void OnClickBottomStageSlot()
        {
            // 스테이지 개방 여부 확인
            if (UserDataManager.Instance.IsStageOpen(_specStageData.stage_id) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_LOCK_STAGE");
                return;
            }

            // 유저 데이터 갱신
            UserDataManager.Instance.SetLastPlayStageID(_specStageData.stage_id, true);

            // 로비 메인 하단 스테이지 UI 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer("LobbyMain");
            if (lobbyMain != null)
            {
                lobbyMain.GetComponent<LobbyMain>()?.RefreshBottomStageUI();
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
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
