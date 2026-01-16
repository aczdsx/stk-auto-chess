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
    /// <summary>
    /// 로비 하단 스테이지 슬롯 UI 컴포넌트
    /// 
    /// [인스턴스 생성 및 매핑 방식]
    /// 1. BattleReadyMain.SetBottomStageUI()에서 _stageSelectSlotObject 프리팹을 Instantiate
    /// 2. 프리팹에 이미 이 컴포넌트가 붙어있고, 모든 SerializeField 멤버 변수들이 Unity Inspector에서 미리 설정됨
    /// 3. GetComponent<LobbyBottomStageSlot>()로 컴포넌트를 가져온 후 SetStageItemSlot() 호출하여 데이터 설정
    /// 
    /// [멤버 변수 매핑]
    /// - 모든 [SerializeField] 변수들은 Unity Inspector에서 프리팹/씬의 GameObject/Component를 드래그 앤 드롭으로 연결
    /// - 런타임에 코드로 할당하지 않음 (프리팹에 미리 설정되어 있음)
    /// </summary>
    public class LobbyBottomStageSlot : CachedMonoBehaviour
    {
        [Header("[Stage - Common]")]
        [SerializeField] private int _normalStateHeight;        // 일반 스테이지 높이 (Inspector에서 설정)
        [SerializeField] private int _doneStateHeight;          // 완료 스테이지 높이 (Inspector에서 설정)
        [SerializeField] private int _currentStateHeight;       // 현재 스테이지 높이 (Inspector에서 설정)
        [SerializeField] private CAButton _bottomStageSlotButton;   // 스테이지 선택 버튼 (Inspector에서 프리팹의 버튼 GameObject 연결)

        [Header("[State - Normal]")]
        [SerializeField] private GameObject _normalLayerObject;  // 일반 스테이지 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _normalStarLayerObject;  // 별 표시 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _normalCharacterLayerObject;  // 캐릭터 표시 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _normalClearCheckObject;  // 클리어 체크 표시 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _normalNextStageCheckObject;  // 다음 스테이지 체크 표시 GameObject (Inspector에서 연결)
        [SerializeField] private TextMeshProUGUI _normalStageNumberText;  // 스테이지 번호 텍스트 (Inspector에서 연결)
        [SerializeField] private List<GameObject> _normalStarObjectList;  // 별 GameObject 리스트 (Inspector에서 연결)

        [Header("[State - Perfect Clear]")]
        [SerializeField] private GameObject _perfectClearLayerObject;  // 퍼펙트 클리어 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _perfectClearCharacterLayerObject;  // 퍼펙트 클리어 캐릭터 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _perfectClearNextStageCheckObject;  // 퍼펙트 클리어 다음 스테이지 체크 GameObject (Inspector에서 연결)
        [SerializeField] private TextMeshProUGUI _perfectClearStageNumberText;  // 퍼펙트 클리어 스테이지 번호 텍스트 (Inspector에서 연결)

        [Header("[State - Elite/Boss]")]
        [SerializeField] private GameObject _bossLayerObject;  // 보스/엘리트 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _bossStarLayerObject;  // 보스 별 표시 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _bossCharacterLayerObject;  // 보스 캐릭터 표시 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _bossClearCheckObject;  // 보스 클리어 체크 표시 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _bossNextStageCheckObject;  // 보스 다음 스테이지 체크 표시 GameObject (Inspector에서 연결)
        [SerializeField] private TextMeshProUGUI _bossStageNumberText;  // 보스 스테이지 번호 텍스트 (Inspector에서 연결)
        [SerializeField] private TextMeshProUGUI _bossStageTitleText;  // 보스 스테이지 제목 텍스트 (Inspector에서 연결)
        [SerializeField] private List<GameObject> _bossStarObjectList;  // 보스 별 GameObject 리스트 (Inspector에서 연결)

        // ========== 런타임 데이터 (코드에서 설정) ==========
        private StageInfo _specStageData;  // SetStageItemSlot()에서 설정되는 스테이지 스펙 데이터
        private BattleStageProgress _userStageProgress;  // SetStageItemSlot()에서 설정되는 유저 진행도 데이터

        // ========== 상태 플래그 (SetStageItemSlot() 또는 RefershSlot()에서 계산) ==========
        private bool _isClearStage;     // 클리어 한 스테이지 여부 체크용
        private bool _isCurrentStage;   // 현재 스테이지 여부 체크용
        private bool _isValidStage;     // 유효한 스테이지 여부 체크용 (BestStars > 0)
        private bool _isLatestPlayableStage;      // 플레이 가능한 다음 스테이지 체크용

        // ========== 계산된 값 ==========
        private int _resultCustomHeight;    // 스테이지 상태에 따른 최종 높이 값 (SetStageState()에서 계산)

        private void Awake()
        {
            _bottomStageSlotButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickBottomStageSlot()).AddTo(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        /// <summary>
        /// 스테이지 슬롯에 데이터를 설정합니다.
        /// 
        /// [호출 위치]
        /// - BattleReadyMain.SetBottomStageUI()에서 각 스테이지마다 호출됨
        /// 
        /// [설정되는 데이터]
        /// - _specStageData: 스테이지 스펙 정보
        /// - _userStageProgress: 유저의 스테이지 진행도 (클리어 여부, 별 개수 등)
        /// - _isClearStage: 클리어 여부
        /// - _isCurrentStage: 현재 선택된 스테이지 여부
        /// - _isLatestPlayableStage: 다음에 플레이 가능한 스테이지 여부
        /// 
        /// [다음 단계]
        /// - SetStageState() 호출하여 UI 상태 설정
        /// </summary>
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

        /// <summary>
        /// 슬롯 데이터를 갱신합니다.
        /// 
        /// [호출 위치]
        /// - BattleReadyMain.RefreshBottomStageUI()에서 모든 슬롯에 대해 호출됨
        /// - 스테이지 선택 후 UI 갱신 시 사용
        /// </summary>
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
