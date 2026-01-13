using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class LobbyBottomStageSlot : CachedMonoBehaviour
    {
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

        [Header("[State - Perfect Clear]")]
        [SerializeField] private GameObject _perfectClearLayerObject;
        [SerializeField] private GameObject _perfectClearCharacterLayerObject;
        [SerializeField] private GameObject _perfectClearNextStageCheckObject;
        [SerializeField] private TextMeshProUGUI _perfectClearStageNumberText;

        [Header("[State - Elite/Boss]")]
        [SerializeField] private GameObject _bossLayerObject;
        [SerializeField] private GameObject _bossStarLayerObject;
        [SerializeField] private GameObject _bossCharacterLayerObject;
        [SerializeField] private GameObject _bossClearCheckObject;
        [SerializeField] private GameObject _bossNextStageCheckObject;
        [SerializeField] private TextMeshProUGUI _bossStageNumberText;
        [SerializeField] private TextMeshProUGUI _bossStageTitleText;
        [SerializeField] private List<GameObject> _bossStarObjectList;

        private StageInfo _specStageData;
        private BattleStageProgress _userStageProgress;

        private bool _isClearStage;     // 클리어 한 스테이지 여부 체크용
        private bool _isCurrentStage;   // 현재 스테이지 여부 체크용
        private bool _isValidStage;     // 유효한 스테이지 여부 체크용
        private bool _isLatestPlayableStage;      // 플레이 가능한 다음 스테이지 체크용

        private int _resultCustomHeight;    // 스테이지 상태에 따른 최종 높이 값

        private void Awake()
        {
            _bottomStageSlotButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickBottomStageSlot()).AddTo(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public void SetStageItemSlot(StageInfo data, bool isCurrentStage)
        {
            if (data == null) return;

            _specStageData = data;
            _userStageProgress = ServerDataManager.Instance.Battle.GetStageProgress((uint)_specStageData.stage_id);

            _isClearStage = ServerDataManager.Instance.Battle.IsStageCleared((uint)_specStageData.stage_id);
            _isCurrentStage = isCurrentStage;

            int lastestStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
            var nextStageData = SpecDataManager.Instance.GetNextStageData(lastestStageID);
            _isLatestPlayableStage = nextStageData != null && (nextStageData.stage_id == _specStageData.stage_id);

            SetStageState();
        }

        public void RefershSlot()
        {
            _userStageProgress = ServerDataManager.Instance.Battle.GetStageProgress((uint)_specStageData.stage_id);
            _isClearStage = ServerDataManager.Instance.Battle.IsStageCleared((uint)_specStageData.stage_id);
            _isCurrentStage = LocalDataManager.Instance.GetLastPlayStageId() == _specStageData.stage_id;

            SetStageState();
        }

        private void SetStageState()
        {
            ClearSlot();

            _resultCustomHeight = _isClearStage ? _doneStateHeight : (_isCurrentStage ? _currentStateHeight : _normalStateHeight);
            _isValidStage = _userStageProgress != null && _userStageProgress.BestStars > 0;
            bool isPerfectClear = _isValidStage && _userStageProgress.BestStars >= 3;

            switch (_specStageData.stage_type)
            {
                case StageType.BATTLE_NORMAL:
                    if (isPerfectClear)
                    {
                        SetPerfectClearLayerState();
                    }
                    else
                    {
                        SetNormalLayerState();
                    }
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
                    _normalStarObjectList[i].SetActive(i < _userStageProgress.BestStars);
                }
            }

            _normalCharacterLayerObject.SetActive(_isCurrentStage);
            _normalClearCheckObject.SetActive(_isClearStage);
            _normalNextStageCheckObject.SetActive(_isLatestPlayableStage);
        }

        private void SetPerfectClearLayerState()
        {
            _perfectClearStageNumberText.text = _specStageData.stage_number.ToString();

            var originNormalSizeDelta = _perfectClearLayerObject.GetComponent<RectTransform>().sizeDelta;
            _perfectClearLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originNormalSizeDelta.x, _resultCustomHeight);

            _perfectClearLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_NORMAL || _specStageData.stage_type == StageType.CHEST);

            _perfectClearCharacterLayerObject.SetActive(_isCurrentStage);
            _perfectClearNextStageCheckObject.SetActive(_isLatestPlayableStage);
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
                    _bossStarObjectList[i].SetActive(i < _userStageProgress.BestStars);
                }
            }

            _bossCharacterLayerObject.SetActive(_isCurrentStage);
            _bossClearCheckObject.SetActive(_isClearStage);
            _bossNextStageCheckObject.SetActive(_isLatestPlayableStage);
        }

        private void OnClickBottomStageSlot()
        {
            // 스테이지 개방 여부 확인
            if (ServerDataManager.Instance.Battle.IsStageOpen((uint)_specStageData.stage_id) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_LOCK_STAGE");
                return;
            }

            // 유저 데이터 갱신
            LocalDataManager.Instance.SetLastPlayStageId((uint)_specStageData.stage_id);

            // 로비 메인 하단 스테이지 UI 갱신
            var battleReadyMain = SceneUILayerManager.Instance.GetUILayer("BattleReadyMain");
            if (battleReadyMain != null)
            {
                battleReadyMain.GetComponent<BattleReadyMain>()?.RefreshBottomStageUI();
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
