using System;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using System.Linq;
using R3;

namespace CookApps.AutoBattler
{
    public class UIElpisBuildPopup : UILayer
    {
        [Header("UI Components")]
        [SerializeField] private CAButton closeButton;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private UIElpisBuildCell _cellPrefab;

        private List<UIElpisBuildCell> _cells = new List<UIElpisBuildCell>();
        private ElpisDataBridge _dataBridge;

        private ElpisFacility facilityData;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            _dataBridge = new ElpisDataBridge();

            if (param is ElpisFacility facility)
            {
                facilityData = facility;
            }
            RefreshUI();
        }

        protected override void Awake()
        {
            base.Awake();

            closeButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnCloseClicked())
                .AddTo(this);
        }

        private void RefreshUI()
        {
            PopulateBuildList();
        }

        private void PopulateBuildList()
        {
            // Clear existing cells (Simple destruction for MVP)
            foreach (var cell in _cells)
            {
                Destroy(cell.gameObject);
            }
            _cells.Clear();

            // Load Build Infos
            var buildInfos = SpecDataManager.Instance.ElpisBuildInfo.All;
            var commandCenter = _dataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            int commandCenterLv = (int)commandCenter.Level;

            foreach (var info in buildInfos)
            {
                // Instantiate Cell
                var cell = Instantiate(_cellPrefab, contentRoot);

                // Determine State
                bool isInstalled = HasFacility(info.bulid_id);
                // 설치 조건: 사령부 레벨이 건물 정보의 조건을 만족해야 함 (여기서는 임시로 build_lv를 요구 레벨로 가정하거나 별도 필드 확인)
                // ElpisBuildInfo의 build_lv가 설치된 건물의 레벨을 의미하는지, 요구 레벨인지 SpecDatas.cs를 다시 봐야 함.
                // 보통 build_group_id로 묶이고 level별로 존재.
                // 1레벨 건물 정보를 표시하고, 이미 설치되었으면 업그레이드 or 설치 완료 표시.
                // 여기서는 "건물 설치" 팝업이므로, 1레벨 건물(신규 설치) 목록만 보여주거나,
                // 설치되지 않은 건물들만 보여주는 것이 타당함.

                if (info.build_lv != 1) continue; // 1레벨(설치) 정보만 표시

                // Check Lock Condition (Command Center Level Dependency)
                // ElpisCommandCenterBenefit에서 해금 정보를 가져와야 정확함.
                bool isLocked = !IsUnlockedByCommandCenter(info.build_group_id, commandCenterLv);

                // Check Cost
                bool canAfford = true; // TODO: Check actual resources

                cell.SetData(info, isLocked, isInstalled, canAfford, OnInstallRequested);
                _cells.Add(cell);
            }
        }

        private bool HasFacility(int buildId)
        {
            // buildId가 유니크 ID라면 이것으로 체크.
            // Bridge의 HasFacility는 instanceId를 받지만, 여기선 Spec ID로 체크해야 함.
            // Bridge에 Spec ID(build_id string or unique int)로 체크하는 기능이 없다면,
            // GetAllFacilities()로 순회해야 함.

            var facilities = _dataBridge.GetAllFacilities();
            // ElpisFacility에는 DataId(String) 또는 similar field가 있을 것. 
            // 여기서는 간단히 구현하고 추후 수정.
            // return facilities.Any(f => f.DataId == buildId.ToString()); 
            return false; // 임시
        }

        private bool IsUnlockedByCommandCenter(int buildGroupId, int commandCenterLv)
        {
            // ElpisCommandCenterBenefit 테이블에서 현재 사령부 레벨로 해금되는 build_group_id 확인
            // 혹은 단순히 요구 레벨 로직이 있다면 사용.
            // MVP 명세에 따르면 사령부 레벨에 따라 해금.

            var benefits = SpecDataManager.Instance.ElpisCommandCenterBenefit.All;
            // 현재 레벨 이하의 혜택들 중 해당 건물을 해금하는 혜택이 있는지 확인
            foreach (var benefit in benefits)
            {
                if (benefit.lv <= commandCenterLv && benefit.build_group_id == buildGroupId)
                    return true;
            }
            return false;
        }

        private void OnInstallRequested(ElpisBuildInfo info)
        {
            // Send Install Request
            InstallBuilding(info).Forget();
        }

        private async UniTaskVoid InstallBuilding(ElpisBuildInfo info)
        {
            // if (_targetSlotIndex < 0)
            // {
            //     Debug.LogError("Slot Index is invalid!");
            //     return;
            // }

            if (!Enum.TryParse<ElpisFacilityType>(info.build_id, out var facilityType))
            {
                Debug.LogError($"Invalid Facility Type: {info.build_id}");
                return;
            }

            // NetManager 호출
            var result = await NetManager.Instance.Elpis.BuildFacilityAsync(facilityType, 0, 0);

            if (result != null && result.IsSuccess)
            {
                Debug.Log($"건물 설치 성공: {info.buld_name_token}");
                // 성공 시 팝업 닫기
                OnCloseClicked();

                // 로비 메인 갱신 (Visual Logic)
                var lobby = LobbyMain.GetLobbyMain();
                if (lobby != null && lobby.MainBlock != null)
                {
                    // TODO: 건물 설치
                }
            }
            else
            {
                Debug.LogError($"건물 설치 실패: {info.buld_name_token}");
                // Toast or Error Popup
            }
        }

        private void OnCloseClicked()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
