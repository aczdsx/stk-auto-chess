using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using R3;
using Tech.Hive.V1;
using TMPro;
using Unity.VisualScripting;
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
        [SerializeField] private int _currentStateHeight;       // 현재 스테이지 높이 (Inspector에서 설정)
        [SerializeField] private float _normalChaPosY = 102.8f;
        [SerializeField] private float _bossChaPosY = 114.2f;
        [SerializeField] private CAButton _bottomStageSlotButton;   // 스테이지 선택 버튼 (Inspector에서 프리팹의 버튼 GameObject 연결)
        [SerializeField] private GameObject _currentItemObj;  // 일반 스테이지 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private GameObject _characterLayerObject;  // 캐릭터 표시 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private TextMeshProUGUI _stageNumberText;  // 스테이지 번호 텍스트 (Inspector에서 연결)
        [SerializeField] private GameObject _starLayerObject;  // 별 표시 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private List<GameObject> _starObjectList;  // 별 GameObject 리스트 (Inspector에서 연결)

        [Header("[State - Normal]")]
        [SerializeField] private GameObject _normalLayerObject;  // 일반 스테이지 레이어 GameObject (Inspector에서 연결)


        [Header("[State - Perfect Clear]")]
        [SerializeField] private GameObject _perfectClearLayerObject;  // 퍼펙트 클리어 레이어 GameObject (Inspector에서 연결)

        [Header("[State - Elite/Boss]")]
        [SerializeField] private GameObject _bossLayerObject;  // 보스/엘리트 레이어 GameObject (Inspector에서 연결)
        [SerializeField] private TextMeshProUGUI _bossStageTitleText;  // 보스 스테이지 제목 텍스트 (Inspector에서 연결)

        // ========== 런타임 데이터 (코드에서 설정) ==========
        private StageInfo _specStageData;  // SetStageItemSlot()에서 설정되는 스테이지 스펙 데이터
        private BattleStageProgress _userStageProgress;  // SetStageItemSlot()에서 설정되는 유저 진행도 데이터

        // ========== 상태 플래그 (SetStageItemSlot() 또는 RefershSlot()에서 계산) ==========
        private bool _isClearStage;     // 클리어 한 스테이지 여부 체크용
        private bool _isCurrentStage;   // 현재 스테이지 여부 체크용
        private bool _isValidStage;     // 유효한 스테이지 여부 체크용 (BestStars > 0)
        private bool _isLatestPlayableStage;      // 플레이 가능한 다음 스테이지 체크용


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

            _isValidStage = _userStageProgress != null && _userStageProgress.BestStars > 0;
            bool isPerfectClear = _isValidStage && _userStageProgress.BestStars >= 3;

            // 공통 처리: 현재 스테이지 표시
            _currentItemObj.SetActive(_isCurrentStage);
            _stageNumberText.text = _specStageData.stage_number.ToString("D2");  // 2자리 형식 (예: 3 -> "03")

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

            var originNormalSizeDelta = _normalLayerObject.GetComponent<RectTransform>().sizeDelta;
            _normalLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originNormalSizeDelta.x, _currentStateHeight);

            _normalLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_NORMAL || _specStageData.stage_type == StageType.CHEST);
            _starLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _starObjectList.Count; i++)
                {
                    _starObjectList[i].SetActive(i < _userStageProgress.BestStars);
                }
            }

            _characterLayerObject.SetActive(_isCurrentStage);
            // _characterLayerObject.GetComponent<RectTransform>().localPosition = new Vector3(0, _normalChaPosY,0.0f);
        }

        private void SetPerfectClearLayerState()
        {

            var originNormalSizeDelta = _perfectClearLayerObject.GetComponent<RectTransform>().sizeDelta;
            _perfectClearLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originNormalSizeDelta.x, _currentStateHeight);

            _perfectClearLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_NORMAL || _specStageData.stage_type == StageType.CHEST);
            _starLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _starObjectList.Count; i++)
                {
                    _starObjectList[i].SetActive(i < _userStageProgress.BestStars);
                }
            }
            // _characterLayerObject.GetComponent<RectTransform>().localPosition = new Vector3(0, _normalChaPosY,0.0f);
            _characterLayerObject.SetActive(_isCurrentStage);
        }

        private void SetBossLayerState()
        {
            _stageNumberText.gameObject.SetActive(false);
            _bossStageTitleText.gameObject.SetActive(true);
            _bossStageTitleText.text = _specStageData.stage_type == StageType.BATTLE_BOSS ? "BOSS" : "ELITE";

            var originBossSizeDelta = _bossLayerObject.GetComponent<RectTransform>().sizeDelta;
            _bossLayerObject.GetComponent<RectTransform>().sizeDelta = new Vector2(originBossSizeDelta.x, _currentStateHeight);

            _bossLayerObject.SetActive(_specStageData.stage_type == StageType.BATTLE_BOSS || _specStageData.stage_type == StageType.BATTLE_ELITE);
            _starLayerObject.SetActive(_isValidStage);
            if (_isValidStage)
            {
                for (int i = 0; i < _starObjectList.Count; i++)
                {
                    _starObjectList[i].SetActive(i < _userStageProgress.BestStars);
                }
            }

            _characterLayerObject.SetActive(_isCurrentStage);
            // _characterLayerObject.GetComponent<RectTransform>().localPosition = new Vector3(0, _bossChaPosY,0.0f);
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
            _perfectClearLayerObject.SetActive(false);
            _bossLayerObject.SetActive(false);

            _starLayerObject.SetActive(false);
        }
    }
}
